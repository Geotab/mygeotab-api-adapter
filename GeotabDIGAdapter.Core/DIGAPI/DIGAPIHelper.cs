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
                        throw new DIGConnectionException($"DIG API authentication failed with status {response.StatusCode}: {responseContent}");
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
                            ? string.Join(", ", authResponse.Error)
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
                        throw new DIGConnectionException($"DIG API token refresh failed with status {response.StatusCode}: {responseContent}");
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
                            ? string.Join(", ", refreshResponse.Error)
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
                        throw new DIGConnectionException($"DIG API token revocation failed with status {response.StatusCode}: {responseContent}");
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
                        var apiResponse = JsonSerializer.Deserialize<DIGAPIResponse<string>>(
                            responseContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        var errorDetail = string.Join(", ", apiResponse?.Error ?? new List<string> { responseContent });

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
                        throw new Exception($"DIG API returned {response.StatusCode}: {errorDetail}");
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
                        var apiResponse = JsonSerializer.Deserialize<DIGAPIResponse<string>>(
                            responseContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        var errorDetail = string.Join(", ", apiResponse?.Error ?? new List<string> { responseContent });

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
                        throw new Exception($"DIG API returned {response.StatusCode}: {errorDetail}");
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