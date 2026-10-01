using MyGeotabAPIAdapter.DIGAPI.Models;
using MyGeotabAPIAdapter.Exceptions;
using MyGeotabAPIAdapter.Logging;
using NLog;
using Polly.Wrap;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MyGeotabAPIAdapter.DIGAPI
{
    /// <summary>
    /// A helper class to assist in working with the DIG (Data Intake Gateway) API.
    /// </summary>
    public class DIGAPIHelper : IDIGAPIHelper
    {
        public const int DefaultTimeoutSeconds = 30;
        public const int MinTimeoutSeconds = 30;

        // Polly-related items:
        AsyncPolicyWrap? asyncDIGAPICallTimeoutAndRetryPolicyWrap;
        AsyncPolicyWrap? asyncDIGAPICallWithReauthPolicyWrap;

        readonly IDIGExceptionHelper exceptionHelper;
        readonly IDIGAPIRateLimiter rateLimiter;
        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly HttpClient httpClient;

        // DIG API connection parameters
        string digAPIEndpoint = string.Empty;
        string digAPIUsername = string.Empty;
        string digAPIPassword = string.Empty;

        // Token management
        DIGToken? bearerToken;
        DIGToken? refreshToken;

        // Lock object for thread-safe re-authentication
        readonly SemaphoreSlim reauthenticationLock = new(1, 1);

        // Keeps track of the current session generation for re-authentication
        long digSessionGeneration = 0;

        /// <summary>
        /// The bearer token is proactively re-authenticated once its remaining lifetime falls to this buffer (in
        /// minutes) ahead of expiry. This is an internal resilience heuristic, not an operator setting — see
        /// <see cref="ShouldProactivelyRefreshBearerToken"/>, which additionally caps the effective buffer at half
        /// the token's lifetime so short-lived tokens are not refreshed on every call.
        /// </summary>
        public const int ProactiveTokenRefreshBufferMinutes = 60;

        static readonly TimeSpan ProactiveTokenRefreshBuffer = TimeSpan.FromMinutes(ProactiveTokenRefreshBufferMinutes);

        /// <summary>
        /// Maximum number of characters of a non-JSON DIG API error body retained by <see cref="ExtractErrorDetail"/>.
        /// A gateway's HTML error page can run to many kilobytes, all of which would otherwise reach both the log and
        /// the queue's failure-reason column.
        /// </summary>
        public const int MaxRawErrorBodyLength = 500;

        /// <summary>
        /// Stand-in used by <see cref="ExtractErrorDetail"/> when an unsuccessful response carries no body at all, so
        /// that the resulting message does not trail off after the status code.
        /// </summary>
        public const string EmptyErrorBodyPlaceholder = "(no response body)";

        /// <inheritdoc/>
        public bool DIGAPIIsAuthenticated { get; private set; }

        /// <inheritdoc/>
        public DIGToken? CurrentBearerToken => bearerToken;

        /// <inheritdoc/>
        public DIGToken? CurrentRefreshToken => refreshToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="DIGAPIHelper"/> class.
        /// </summary>
        /// <param name="exceptionHelper">The <see cref="IDIGExceptionHelper"/> to use for exception handling.</param>
        /// <param name="rateLimiter">The <see cref="IDIGAPIRateLimiter"/> to use for rate limiting. If null, a default rate limiter is created.</param>
        /// <param name="httpClient">The <see cref="HttpClient"/> to use for HTTP requests. If null, a default client is created.</param>
        public DIGAPIHelper(IDIGExceptionHelper exceptionHelper, IDIGAPIRateLimiter? rateLimiter = null, HttpClient? httpClient = null)
        {
            this.exceptionHelper = exceptionHelper;
            this.rateLimiter = rateLimiter ?? new DIGAPIRateLimiter();
            this.httpClient = httpClient ?? new HttpClient();
        }

        /// <inheritdoc/>
        public async Task AuthenticateDIGAPIAsync(string digApiEndpoint, string username, string password, int requestTimeoutSeconds = DefaultTimeoutSeconds)
        {
            const string MethodName = "Authenticate";

            logger.Info($"Authenticating DIG API (URL: '{digApiEndpoint}', username: '{username}').");

            // Reset authentication state.
            bearerToken = null;
            refreshToken = null;
            DIGAPIIsAuthenticated = false;

            // Store connection parameters for re-authentication.
            digAPIEndpoint = digApiEndpoint.TrimEnd('/');
            digAPIUsername = username;
            digAPIPassword = password;

            ValidateTimeoutSeconds(requestTimeoutSeconds);
            SetAsyncPolicyWrapForDIGAPICalls(requestTimeoutSeconds);

            try
            {
                var pollyContext = DIGAPIResilienceHelper.CreateContextWithMethodName(MethodName);

                await asyncDIGAPICallTimeoutAndRetryPolicyWrap!.ExecuteAsync(async ctx =>
                {
                    await rateLimiter.WaitForPermitAsync(MethodName);

                    var requestBody = new
                    {
                        Username = digAPIUsername,
                        Password = digAPIPassword
                    };

                    var content = new StringContent(
                        JsonSerializer.Serialize(requestBody),
                        Encoding.UTF8,
                        "application/json");

                    var response = await httpClient.PostAsync(
                        $"{digAPIEndpoint}/authentication/authenticate",
                        content);

                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new DIGConnectionException($"DIG API authentication failed with status {response.StatusCode}: {NeutraliseStatusTokens(responseContent)}");
                    }

                    var authResponse = JsonSerializer.Deserialize<DIGAPIResponse<DIGAuthenticationResult>>(
                        responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (authResponse?.Data != null && authResponse.Data.Authenticated)
                    {
                        bearerToken = authResponse.Data.BearerToken;
                        refreshToken = authResponse.Data.RefreshToken;
                        DIGAPIIsAuthenticated = true;
                        logger.Info($"DIG API authentication successful. User: '{digAPIUsername}'.");
                    }
                    else
                    {
                        var errorMessage = authResponse?.HasErrors == true
                            ? NeutraliseStatusTokens(string.Join(", ", authResponse.Error))
                            : "Unknown authentication error";
                        throw new DIGConnectionException($"DIG API authentication failed: {errorMessage}");
                    }
                }, pollyContext);
            }
            catch (Exception exception)
            {
                bearerToken = null;
                refreshToken = null;
                DIGAPIIsAuthenticated = false;

                if (exceptionHelper.ExceptionIsRelatedToDIGConnectivityLoss(exception))
                {
                    throw new DIGConnectionException("An exception occurred while attempting to authenticate with the DIG API.", exception);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Ensures that the DIG API is authenticated before making API calls.
        /// </summary>
        void EnsureAuthenticated()
        {
            if (!DIGAPIIsAuthenticated || bearerToken == null || string.IsNullOrEmpty(bearerToken.TokenString))
            {
                throw new InvalidOperationException($"DIG API is not authenticated. Authenticate using the {nameof(AuthenticateDIGAPIAsync)} method first.");
            }
        }

        /// <inheritdoc/>
        public async Task RefreshTokensAsync(int requestTimeoutSeconds = DefaultTimeoutSeconds)
        {
            const string MethodName = "RefreshToken";

            EnsureAuthenticated();

            if (refreshToken == null || string.IsNullOrEmpty(refreshToken.TokenString))
            {
                throw new InvalidOperationException("No refresh token available. Please re-authenticate.");
            }

            logger.Debug("Refreshing DIG API tokens.");

            ValidateTimeoutSeconds(requestTimeoutSeconds);
            SetAsyncPolicyWrapForDIGAPICalls(requestTimeoutSeconds);

            try
            {
                var pollyContext = DIGAPIResilienceHelper.CreateContextWithMethodName(MethodName);

                await asyncDIGAPICallTimeoutAndRetryPolicyWrap!.ExecuteAsync(async ctx =>
                {
                    await rateLimiter.WaitForPermitAsync(MethodName);

                    var requestBody = new
                    {
                        BearerToken = bearerToken!.TokenString,
                        RefreshToken = refreshToken!.TokenString
                    };

                    var content = new StringContent(
                        JsonSerializer.Serialize(requestBody),
                        Encoding.UTF8,
                        "application/json");

                    var response = await httpClient.PostAsync(
                        $"{digAPIEndpoint}/authentication/refresh-token",
                        content);

                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        // Emit the NUMERIC status code (mirroring PostRecordsAsync/GetInvalidRecordsAsync) so a rejected
                        // refresh token (401/403) is NOT swallowed by the generic non-reauth retry policy
                        // (CreateAsyncRetryPolicyForDIGAPICallsNotTimedOut excludes messages containing "401"/"403"). With the
                        // numeric code present, the exception propagates to ReauthenticateAsync's catch, which escalates to a
                        // full re-login (AuthenticateDIGAPIAsync) instead of retrying the dead refresh token forever.
                        throw new DIGConnectionException($"DIG API token refresh failed with status [{(int)response.StatusCode}] ({response.StatusCode}): {NeutraliseStatusTokens(responseContent)}");
                    }

                    var refreshResponse = JsonSerializer.Deserialize<DIGAPIResponse<DIGAuthenticationResult>>(
                        responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (refreshResponse?.Data != null && refreshResponse.Data.BearerToken != null)
                    {
                        bearerToken = refreshResponse.Data.BearerToken;
                        refreshToken = refreshResponse.Data.RefreshToken;
                        logger.Info("DIG API tokens refreshed successfully.");
                    }
                    else
                    {
                        var errorMessage = refreshResponse?.HasErrors == true
                            ? NeutraliseStatusTokens(string.Join(", ", refreshResponse.Error))
                            : "Unknown refresh error";
                        throw new DIGConnectionException($"DIG API token refresh failed: {errorMessage}");
                    }
                }, pollyContext);
            }
            catch (Exception exception)
            {
                if (exceptionHelper.ExceptionIsRelatedToDIGConnectivityLoss(exception))
                {
                    throw new DIGConnectionException("An exception occurred while attempting to refresh DIG API tokens.", exception);
                }
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task RevokeTokensAsync(int requestTimeoutSeconds = DefaultTimeoutSeconds)
        {
            const string MethodName = "RevokeToken";

            EnsureAuthenticated();

            logger.Debug("Revoking DIG API tokens.");

            ValidateTimeoutSeconds(requestTimeoutSeconds);
            SetAsyncPolicyWrapForDIGAPICalls(requestTimeoutSeconds);

            try
            {
                var pollyContext = DIGAPIResilienceHelper.CreateContextWithMethodName(MethodName);

                await asyncDIGAPICallTimeoutAndRetryPolicyWrap!.ExecuteAsync(async ctx =>
                {
                    await rateLimiter.WaitForPermitAsync(MethodName);

                    var requestBody = new
                    {
                        BearerToken = bearerToken!.TokenString,
                        RefreshToken = refreshToken!.TokenString
                    };

                    var content = new StringContent(
                        JsonSerializer.Serialize(requestBody),
                        Encoding.UTF8,
                        "application/json");

                    var response = await httpClient.PostAsync(
                        $"{digAPIEndpoint}/authentication/revoke-token",
                        content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        throw new DIGConnectionException($"DIG API token revocation failed with status {response.StatusCode}: {NeutraliseStatusTokens(responseContent)}");
                    }

                    bearerToken = null;
                    refreshToken = null;
                    DIGAPIIsAuthenticated = false;
                    logger.Info("DIG API tokens revoked successfully.");
                }, pollyContext);
            }
            catch (Exception exception)
            {
                if (exceptionHelper.ExceptionIsRelatedToDIGConnectivityLoss(exception))
                {
                    throw new DIGConnectionException("An exception occurred while attempting to revoke DIG API tokens.", exception);
                }
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<PostRecordsResult> PostRecordsAsync(IList<object> records, int requestTimeoutSeconds = DefaultTimeoutSeconds)
        {
            const string MethodName = "PostRecords";

            ArgumentNullException.ThrowIfNull(records);

            if (records.Count == 0)
            {
                return new PostRecordsResult
                {
                    IsSuccess = true,
                    TrackingId = null
                };
            }

            if (records.Count > 5000)
            {
                throw new ArgumentException("Maximum of 5000 records can be posted at once.", nameof(records));
            }

            logger.Debug($"Posting {records.Count} record(s) to DIG API.");

            EnsureAuthenticated();
            ValidateTimeoutSeconds(requestTimeoutSeconds);
            SetAsyncPolicyWrapForDIGAPICallsWithReauthentication(requestTimeoutSeconds);
            await EnsureBearerTokenFreshAsync();

            PostRecordsResult? result = null;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var pollyContext = DIGAPIResilienceHelper.CreateContextWithMethodName(MethodName);

                await asyncDIGAPICallWithReauthPolicyWrap!.ExecuteAsync(async ctx =>
                {
                    await rateLimiter.WaitForPermitAsync(MethodName);

                    var content = new StringContent(
                        JsonSerializer.Serialize(records),
                        Encoding.UTF8,
                        "application/json");

                    var request = new HttpRequestMessage(HttpMethod.Post, $"{digAPIEndpoint}/records")
                    {
                        Content = content
                    };
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken!.TokenString);

                    var response = await httpClient.SendAsync(request);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var apiResponse = JsonSerializer.Deserialize<DIGAPIResponse<Guid>>(
                            responseContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        result = new PostRecordsResult
                        {
                            IsSuccess = true,
                            TrackingId = apiResponse?.Data
                        };

                        logger.Debug($"Successfully posted {records.Count} record(s). TrackingId: {result.TrackingId}");
                    }
                    else
                    {
                        var statusCode = (int)response.StatusCode;

                        // Parsed defensively: an error body is not necessarily JSON (see ExtractErrorDetail). Letting a
                        // JsonException escape here would pre-empt the throw below, which is the only thing that puts
                        // the numeric status code in front of the resilience predicates.
                        var errorDetail = ExtractErrorDetail(responseContent, out var rawErrorBody);
                        if (rawErrorBody != null)
                        {
                            // The character count is reported HERE and not in errorDetail: errorDetail is
                            // interpolated into the thrown message the resilience predicates read, and a length of
                            // 400 or 429 would steer it. A log line steers nothing.
                            logger.Warn($"DIG API {MethodName} received an unusable error body ({responseContent.Trim().Length} characters) with HTTP {statusCode} ({response.StatusCode}): {rawErrorBody}");
                        }

                        // Non-retryable client errors — return error result immediately without throwing.
                        if (statusCode == 400 || statusCode == 413)
                        {
                            result = new PostRecordsResult
                            {
                                IsSuccess = false,
                                TrackingId = null,
                                ErrorMessage = $"DIG API returned {response.StatusCode}: {errorDetail}",
                                ErrorSource = ErrorSource.DIGAPI,
                                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                            };
                            logger.Error($"DIG API {MethodName} non-retryable error: {result.ErrorMessage}");
                            return;
                        }

                        // All other errors — throw so Polly policies can handle retries, token refresh, or rate limiting.
                        // The numeric status code MUST appear in the message: the token-expiry/rate-limit Polly predicates
                        // match on "401"/"403"/"429", whereas {response.StatusCode} renders the enum name (e.g. "Unauthorized").
                        throw new Exception($"DIG API returned [{statusCode}] ({response.StatusCode}): {errorDetail}");
                    }
                }, pollyContext);
            }
            catch (Exception exception)
            {
                stopwatch.Stop();

                if (exceptionHelper.ExceptionIsRelatedToDIGConnectivityLoss(exception))
                {
                    throw new DIGConnectionException("An exception occurred while attempting to post records to the DIG API.", exception);
                }

                result = new PostRecordsResult
                {
                    IsSuccess = false,
                    TrackingId = null,
                    ErrorMessage = exception.Message,
                    ErrorSource = ErrorSource.Middleware,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                };

                logger.Error($"DIG API {MethodName} failed: {exception.Message}");
            }

            stopwatch.Stop();
            if (result != null)
            {
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }

            return result ?? throw new InvalidOperationException($"{MethodName} failed to return a result.");
        }

        /// <inheritdoc/>
        public DIGAPIRateLimiterStatistics GetRateLimiterStatistics()
        {
            return rateLimiter.GetStatistics();
        }

        /// <inheritdoc/>
        public DIGAPIRateLimiterMethodStatistics GetRateLimiterStatistics(string methodName)
        {
            return rateLimiter.GetStatistics(methodName);
        }

        /// <inheritdoc/>
        public async Task<GetInvalidRecordsResult> GetInvalidRecordsAsync(int? nextResultKey = null, int numberOfResults = 1000, int requestTimeoutSeconds = DefaultTimeoutSeconds)
        {
            const string MethodName = "GetInvalidRecords";

            if (numberOfResults < 1 || numberOfResults > 50000)
            {
                throw new ArgumentException("NumberOfResults must be between 1 and 50000.", nameof(numberOfResults));
            }

            logger.Debug($"Retrieving invalid records from DIG API (NextResultKey: {nextResultKey?.ToString() ?? "null"}, NumberOfResults: {numberOfResults}).");

            EnsureAuthenticated();
            ValidateTimeoutSeconds(requestTimeoutSeconds);
            SetAsyncPolicyWrapForDIGAPICallsWithReauthentication(requestTimeoutSeconds);
            await EnsureBearerTokenFreshAsync();

            GetInvalidRecordsResult? result = null;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var pollyContext = DIGAPIResilienceHelper.CreateContextWithMethodName(MethodName);

                await asyncDIGAPICallWithReauthPolicyWrap!.ExecuteAsync(async ctx =>
                {
                    await rateLimiter.WaitForPermitAsync(MethodName);

                    // Build query string
                    var queryParams = new List<string>
                    {
                        $"NumberOfResults={numberOfResults}"
                    };
                    if (nextResultKey.HasValue)
                    {
                        queryParams.Add($"NextResultKey={nextResultKey.Value}");
                    }
                    var queryString = string.Join("&", queryParams);

                    var request = new HttpRequestMessage(HttpMethod.Get, $"{digAPIEndpoint}/invalidrecords?{queryString}");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken!.TokenString);

                    var response = await httpClient.SendAsync(request);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var apiResponse = JsonSerializer.Deserialize<DIGAPIResponse<DIGInvalidRecordsResponseData>>(
                            responseContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (apiResponse?.Data != null)
                        {
                            result = new GetInvalidRecordsResult
                            {
                                IsSuccess = true,
                                NextResultKey = apiResponse.Data.NextResultKey,
                                CurrentResultKey = apiResponse.Data.CurrentResultKey,
                                TotalNumberOfInvalidRecords = apiResponse.Data.TotalNumberOfInvalidRecords,
                                CurrentNumberOfInvalidRecords = apiResponse.Data.CurrentNumberOfInvalidRecords,
                                InvalidRecords = apiResponse.Data.InvalidRecords ?? new List<DIGInvalidRecord>()
                            };

                            logger.Debug($"Successfully retrieved {result.CurrentNumberOfInvalidRecords} invalid record(s) from DIG API. Total available: {result.TotalNumberOfInvalidRecords}");
                        }
                        else
                        {
                            result = new GetInvalidRecordsResult
                            {
                                IsSuccess = true,
                                NextResultKey = null,
                                CurrentResultKey = nextResultKey,
                                TotalNumberOfInvalidRecords = 0,
                                CurrentNumberOfInvalidRecords = 0,
                                InvalidRecords = new List<DIGInvalidRecord>()
                            };

                            logger.Debug("DIG API returned success but no data in response.");
                        }
                    }
                    else
                    {
                        var statusCode = (int)response.StatusCode;

                        // Parsed defensively: an error body is not necessarily JSON (see ExtractErrorDetail). Letting a
                        // JsonException escape here would pre-empt the throw below, which is the only thing that puts
                        // the numeric status code in front of the resilience predicates.
                        var errorDetail = ExtractErrorDetail(responseContent, out var rawErrorBody);
                        if (rawErrorBody != null)
                        {
                            // The character count is reported HERE and not in errorDetail: errorDetail is
                            // interpolated into the thrown message the resilience predicates read, and a length of
                            // 400 or 429 would steer it. A log line steers nothing.
                            logger.Warn($"DIG API {MethodName} received an unusable error body ({responseContent.Trim().Length} characters) with HTTP {statusCode} ({response.StatusCode}): {rawErrorBody}");
                        }

                        // Non-retryable client errors — return error result immediately without throwing.
                        if (statusCode == 400)
                        {
                            result = new GetInvalidRecordsResult
                            {
                                IsSuccess = false,
                                NextResultKey = null,
                                CurrentResultKey = nextResultKey,
                                TotalNumberOfInvalidRecords = 0,
                                CurrentNumberOfInvalidRecords = 0,
                                InvalidRecords = new List<DIGInvalidRecord>(),
                                ErrorMessage = $"DIG API returned {response.StatusCode}: {errorDetail}",
                                ErrorSource = ErrorSource.DIGAPI,
                                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                            };
                            logger.Error($"DIG API {MethodName} non-retryable error: {result.ErrorMessage}");
                            return;
                        }

                        // All other errors — throw so Polly policies can handle retries, token refresh, or rate limiting.
                        // The numeric status code MUST appear in the message: the token-expiry/rate-limit Polly predicates
                        // match on "401"/"403"/"429", whereas {response.StatusCode} renders the enum name (e.g. "Unauthorized").
                        throw new Exception($"DIG API returned [{statusCode}] ({response.StatusCode}): {errorDetail}");
                    }
                }, pollyContext);
            }
            catch (Exception exception)
            {
                stopwatch.Stop();

                if (exceptionHelper.ExceptionIsRelatedToDIGConnectivityLoss(exception))
                {
                    throw new DIGConnectionException("An exception occurred while attempting to retrieve invalid records from the DIG API.", exception);
                }

                result = new GetInvalidRecordsResult
                {
                    IsSuccess = false,
                    NextResultKey = null,
                    CurrentResultKey = nextResultKey,
                    TotalNumberOfInvalidRecords = 0,
                    CurrentNumberOfInvalidRecords = 0,
                    InvalidRecords = new List<DIGInvalidRecord>(),
                    ErrorMessage = exception.Message,
                    ErrorSource = ErrorSource.Middleware,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                };

                logger.Error($"DIG API {MethodName} failed: {exception.Message}");
            }

            stopwatch.Stop();
            if (result != null)
            {
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }

            return result ?? throw new InvalidOperationException($"{MethodName} failed to return a result.");
        }

        /// <summary>
        /// Determines whether the supplied bearer token should be proactively re-authenticated ahead of expiry.
        /// Refresh is triggered once the token's remaining lifetime falls to <paramref name="buffer"/>, except that
        /// the buffer is capped at half the token's total lifetime (when the token's <see cref="DIGToken.Created"/>
        /// time is known) so that short-lived tokens are not refreshed on every call. Returns <c>false</c> when the
        /// token is null or has no known expiry, in which case the reactive 401/403 retry policy is relied upon.
        /// </summary>
        /// <param name="bearerToken">The current bearer token.</param>
        /// <param name="buffer">How close to expiry the token may be before a proactive refresh is warranted.</param>
        public static bool ShouldProactivelyRefreshBearerToken(DIGToken? bearerToken, TimeSpan buffer)
        {
            if (bearerToken == null || bearerToken.Expires == default)
            {
                return false;
            }

            var effectiveBuffer = buffer;
            if (bearerToken.Created != default && bearerToken.Expires > bearerToken.Created)
            {
                var halfLifetime = TimeSpan.FromTicks((bearerToken.Expires - bearerToken.Created).Ticks / 2);
                if (halfLifetime < effectiveBuffer)
                {
                    effectiveBuffer = halfLifetime;
                }
            }

            return bearerToken.TimeUntilExpiry <= effectiveBuffer;
        }

        /// <summary>
        /// Extracts a human-readable error detail from the body of an unsuccessful DIG API response.
        /// <para>
        /// The body is NOT guaranteed to be JSON: an intervening gateway or load balancer may answer a 5xx with its
        /// own HTML error page. Deserializing such a body throws a <see cref="JsonException"/>, and because the
        /// general retry policy deliberately excludes <see cref="JsonException"/> (a malformed <i>successful</i>
        /// response is not worth retrying) that exception reaches no policy at all — the call is abandoned without a
        /// single retry and the caller discards the payload. Callers therefore MUST route error bodies through this
        /// method rather than deserializing them directly, so that the numeric-status-code exception the resilience
        /// predicates match on is the exception that actually gets thrown.
        /// </para>
        /// </summary>
        /// <param name="responseContent">The raw response body.</param>
        /// <param name="rawErrorBody">
        /// When the body yielded no usable detail - it is not JSON, or it parsed but carried no <c>error</c>
        /// property - receives it trimmed and truncated to <see cref="MaxRawErrorBodyLength"/> characters, for the
        /// caller to log. <c>null</c> otherwise.
        /// </param>
        /// <returns>
        /// The joined <see cref="DIGAPIResponse{T}.Error"/> entries when the body carries any; otherwise a short
        /// descriptor. See the remarks on why the raw body is deliberately excluded from the return value.
        /// </returns>
        /// <remarks>
        /// The raw body is returned via <paramref name="rawErrorBody"/> rather than inlined into the detail
        /// because the detail is interpolated into the exception message that the resilience predicates inspect.
        /// An error page happening to contain a status code — in a request id, a byte count, a timestamp — must not
        /// route a 5xx to the re-authentication or non-retryable path, which is the very class of defect this guard
        /// removes.
        /// <para>
        /// That guarantee no longer rests on this method alone, and the earlier wording here overstated what it
        /// gives. The second return path below joins <see cref="DIGAPIResponse{T}.Error"/> and so returns upstream
        /// text verbatim, by design and unchanged since before this guard existed — so "the detail is free of
        /// unvetted upstream text" was never true of every path. What makes the routing safe is that the predicates
        /// now match a DELIMITED status code ("[401]", not "401") that only the thrower emits, and that the upstream
        /// text on that path has its square brackets rewritten (<see cref="NeutraliseStatusTokens"/>), so no upstream
        /// text can produce one. See the constants in DIGAPIResilienceHelper; the two forms are a pair and changing
        /// one without the other breaks the routing silently.
        /// </para>
        /// <para>
        /// The same reasoning bars any number COMPUTED from the body. A descriptor reading "… response body (400
        /// characters) …" steers policy exactly as a request id would, and a given gateway returns a fixed-shape
        /// error page, so an unlucky page length would misroute every one of that deployment's upstream errors
        /// rather than an occasional one. The descriptors this method can return are therefore fixed strings, and
        /// the character count is logged by the caller alongside the body.
        /// </para>
        /// </remarks>
        public static string ExtractErrorDetail(string responseContent, out string? rawErrorBody)
        {
            rawErrorBody = null;

            // JsonSerializer.Deserialize raises ArgumentNullException - NOT JsonException - for a null input,
            // so the catch below would not hold it and it would escape, pre-empting the throw that puts the
            // numeric status code in front of the resilience predicates. That guarantee is the only reason
            // this method exists. No caller in this solution reaches it, because HttpContent.ReadAsStringAsync
            // returns "" and never null; the guard is here because the method is public and the guarantee
            // should not rest on that staying true. Widening the catch to Exception would close the same hole
            // by also swallowing real faults, which is why it is a guard and not a wider catch. An empty or
            // whitespace body already produced this placeholder by way of a JsonException, and still does.
            if (string.IsNullOrWhiteSpace(responseContent))
            {
                return EmptyErrorBodyPlaceholder;
            }

            try
            {
                var apiResponse = JsonSerializer.Deserialize<DIGAPIResponse<string>>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // A body that CARRIES errors is returned as those errors, with square brackets rewritten - see
                // NeutraliseStatusTokens.
                var detail = string.Join(", ", apiResponse?.Error ?? new List<string>());
                if (!string.IsNullOrWhiteSpace(detail))
                {
                    return NeutraliseStatusTokens(detail);
                }

                // The body parsed but produced nothing to say, by either of two routes. Its "error" property was
                // absent or empty - DIGAPIResponse<T>.Error initialises to an EMPTY LIST, so the join yields "" -
                // or the body was the JSON literal "null", which deserialises to a null response altogether.
                //
                // That second route used to fall back to inlining the WHOLE RAW BODY as the detail, which is the
                // one thing this method must never do: the return value is interpolated into the exception
                // message the resilience predicates read, and they match on bare substrings. A four-character
                // "null" carries no digits and so could not in fact steer anything - but the invariant should
                // hold by construction, not by arithmetic on the single body shape that happens to reach it.
                //
                // An intervening gateway answering a 502 with
                // {"message":"upstream connect error"} lands here, and used to be discarded twice over: the thrown
                // message trailed off after the colon, and because no JsonException was raised the body was never
                // logged either. Route it the same way as a non-JSON body, so the gateway's explanation at least
                // reaches the log.
                return DescribeUnusableErrorBody(responseContent, "unrecognized-shape", out rawErrorBody);
            }
            catch (JsonException)
            {
                return DescribeUnusableErrorBody(responseContent, "non-JSON", out rawErrorBody);
            }
        }

        /// <summary>
        /// Rewrites square brackets in upstream text as parentheses, for text that is about to be interpolated into
        /// an exception message the resilience predicates read.
        /// </summary>
        /// <remarks>
        /// The predicates route on a bracketed status code ("[400]", "[401]") that only the thrower is meant to emit.
        /// Upstream text naming an array element - "records[400]" - contains the token itself, and a 5xx carrying it
        /// was excluded from the general retry policy, matched by no other one, and dead-lettered after one
        /// attempt. Brackets are the one character the tokens and ordinary upstream text share, so rewriting them
        /// makes the tokens thrower-only while the text still reads the same in the log.
        /// </remarks>
        /// <param name="upstreamText">Text taken from a DIG API response.</param>
        /// <returns>The text, with every '[' and ']' replaced by '(' and ')'.</returns>
        public static string NeutraliseStatusTokens(string upstreamText)
        {
            return upstreamText.Replace('[', '(').Replace(']', ')');
        }

        /// <summary>
        /// Renders an error body that yielded no usable detail and hands the body itself back for the caller to
        /// log. Shared by the two ways that happens - a body that is not JSON at all, and one that parses but
        /// carries no <c>error</c> property - so that both reach the log by the same route.
        /// </summary>
        /// <param name="responseContent">The raw response body.</param>
        /// <param name="bodyKind">Short label naming why the body was unusable; appears in the returned descriptor.</param>
        /// <param name="rawErrorBody">
        /// Receives the body trimmed and truncated to <see cref="MaxRawErrorBodyLength"/> characters, or
        /// <c>null</c> when there is no body to log.
        /// </param>
        /// <returns>
        /// A short descriptor, deliberately free of unvetted upstream text AND of any number derived from it. See
        /// the remarks on <see cref="ExtractErrorDetail"/>: the descriptor is interpolated into the exception
        /// message the resilience predicates inspect, and a body's own character count is as capable of steering
        /// policy selection as its text. The count belongs in the log line beside the body, and the callers put it
        /// there.
        /// </returns>
        static string DescribeUnusableErrorBody(string responseContent, string bodyKind, out string? rawErrorBody)
        {
            rawErrorBody = null;

            if (string.IsNullOrWhiteSpace(responseContent))
            {
                return EmptyErrorBodyPlaceholder;
            }

            var trimmed = responseContent.Trim();
            rawErrorBody = trimmed.Length <= MaxRawErrorBodyLength
                ? trimmed
                : $"{trimmed[..MaxRawErrorBodyLength]}... (truncated; {trimmed.Length} characters total)";

            // Every value this method can return is a FIXED string carrying no digits - this one, the two
            // bodyKind labels, and EmptyErrorBodyPlaceholder. That is the invariant, and it is what keeps
            // routing dependent on the real status code alone. Do not interpolate a length here.
            return $"{bodyKind} response body; see log for content";
        }

        /// <summary>
        /// Proactively re-authenticates ahead of bearer-token expiry when the token is within
        /// <see cref="ProactiveTokenRefreshBuffer"/> of expiring. Best-effort: any failure is logged and swallowed so
        /// the reactive 401/403 re-authentication retry policy can still recover on the subsequent API call.
        /// </summary>
        async Task EnsureBearerTokenFreshAsync()
        {
            var token = bearerToken;
            if (!ShouldProactivelyRefreshBearerToken(token, ProactiveTokenRefreshBuffer))
            {
                return;
            }

            logger.Info($"DIG bearer token expires in {token!.TimeUntilExpiry.TotalSeconds:F0}s; re-authenticating ahead of expiry.");
            try
            {
                await ReauthenticateAsync();
            }
            catch (Exception exception)
            {
                logger.Warn($"Proactive DIG token refresh failed; the reactive 401/403 re-authentication retry policy remains as a backstop. Error: {exception.Message}");
            }
        }

        /// <summary>
        /// Re-authenticates with the DIG API using stored credentials.
        /// </summary>
        async Task ReauthenticateAsync()
        {
            long generationAtEntry = Interlocked.Read(ref digSessionGeneration);

            await reauthenticationLock.WaitAsync();
            try
            {
                if (Interlocked.Read(ref digSessionGeneration) != generationAtEntry)
                {
                    logger.Debug("Re-authentication skipped - another thread already re-authenticated.");
                    return;
                }

                if (string.IsNullOrEmpty(digAPIEndpoint) || string.IsNullOrEmpty(digAPIUsername) || string.IsNullOrEmpty(digAPIPassword))
                {
                    throw new InvalidOperationException("Cannot re-authenticate: credentials or API endpoint not available. Call AuthenticateDIGAPIAsync first.");
                }

                // Try to use refresh token first if available and not expired
                if (refreshToken != null && !refreshToken.IsExpired)
                {
                    logger.Info("Refreshing DIG API tokens due to bearer token expiry.");
                    try
                    {
                        await RefreshTokensAsync();
                        Interlocked.Increment(ref digSessionGeneration);
                        return;
                    }
                    catch (Exception ex)
                    {
                        logger.Warn($"Token refresh failed, falling back to full re-authentication: {ex.Message}");
                    }
                }

                logger.Info("Re-authenticating with DIG API.");
                await AuthenticateDIGAPIAsync(digAPIEndpoint, digAPIUsername, digAPIPassword);
                Interlocked.Increment(ref digSessionGeneration);
            }
            finally
            {
                reauthenticationLock.Release();
            }
        }

        /// <summary>
        /// Sets the async policy wrap for DIG API calls (without re-authentication).
        /// </summary>
        void SetAsyncPolicyWrapForDIGAPICalls(int timeoutSeconds)
        {
            asyncDIGAPICallTimeoutAndRetryPolicyWrap ??= DIGAPIResilienceHelper.CreateAsyncPolicyWrapForDIGAPICalls<Exception>(
                timeoutSeconds,
                (DIGExceptionHelper)exceptionHelper,
                logger);
        }

        /// <summary>
        /// Sets the async policy wrap for DIG API calls with automatic re-authentication on token expiry.
        /// </summary>
        void SetAsyncPolicyWrapForDIGAPICallsWithReauthentication(int timeoutSeconds)
        {
            asyncDIGAPICallWithReauthPolicyWrap ??= DIGAPIResilienceHelper.CreateAsyncPolicyWrapForDIGAPICallsWithReauthentication<Exception>(
                timeoutSeconds,
                (DIGExceptionHelper)exceptionHelper,
                logger,
                ReauthenticateAsync,
                rateLimiter);
        }

        /// <summary>
        /// Validates that the timeout value meets the minimum requirement.
        /// </summary>
        static void ValidateTimeoutSeconds(int timeoutSeconds)
        {
            if (timeoutSeconds < MinTimeoutSeconds)
            {
                throw new ArgumentException($"The supplied timeout value of {timeoutSeconds} seconds is lower than the minimum allowed timeout value of {MinTimeoutSeconds} seconds.");
            }
        }
    }
}