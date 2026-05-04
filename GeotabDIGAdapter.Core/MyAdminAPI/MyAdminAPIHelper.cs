using Geotab.Checkmate.ObjectModel;
using Geotab.Internal.MyAdmin.APILib.Geotab.MyAdmin.MyAdminApi.ObjectModel;
using MyAdminApiLib.Geotab.MyAdmin.MyAdminApi.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Exceptions;
using MyGeotabAPIAdapter.Logging;
using NLog;
using Polly.Wrap;

namespace MyGeotabAPIAdapter.MyAdminAPI
{
    /// <summary>
    /// A helper class to assist in working with the MyAdmin API.
    /// </summary>
    public class MyAdminAPIHelper : IMyAdminAPIHelper
    {
        public const int DefaultTimeoutSeconds = 30;
        public const int MinTimeoutSeconds = 30;

        // Polly-related items:
        AsyncPolicyWrap? asyncMyAdminAPICallTimeoutAndRetryPolicyWrap;
        AsyncPolicyWrap? asyncMyAdminAPICallWithReauthPolicyWrap;

        readonly IMyAdminExceptionHelper exceptionHelper;
        readonly IMyAdminAPIRateLimiter rateLimiter;
        readonly Logger logger = LogManager.GetCurrentClassLogger();
        // MyAdmin API client
        string myAdminAPIEndpoint = string.Empty;
        string myAdminAPIUsername = string.Empty;
        string myAdminAPIPassword = string.Empty;
        MyAdminInvoker? myAdminApi;
        Guid? myAdminAPIKey;
        Guid? myAdminSessionId;

        // Lock object for thread-safe re-authentication
        readonly SemaphoreSlim reauthenticationLock = new(1, 1);

        // Keeps track of the current session generation for re-authentication so that only one re-authentication occurs when multiple threads detect session expiry.
        long myAdminSessionGeneration = 0;

        /// <inheritdoc/>
        public bool MyAdminAPIIsAuthenticated { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MyAdminAPIHelper"/> class.
        /// </summary>
        /// <param name="exceptionHelper">The <see cref="IMyAdminExceptionHelper"/> to use for exception handling.</param>
        /// <param name="rateLimiter">The <see cref="IMyAdminAPIRateLimiter"/> to use for rate limiting. If null, a default rate limiter is created.</param>
        public MyAdminAPIHelper(IMyAdminExceptionHelper exceptionHelper, IMyAdminAPIRateLimiter? rateLimiter = null)
        {
            this.exceptionHelper = exceptionHelper;
            this.rateLimiter = rateLimiter ?? new MyAdminAPIRateLimiter();
        }

        /// <inheritdoc/>
        public async Task AuthenticateMyAdminAPIAsync(string myAdminApiEndpoint, string username, string password, int requestTimeoutSeconds = DefaultTimeoutSeconds)
        {
            const string MethodName = "Authenticate";

            logger.Info($"Authenticating MyAdmin API (URL: '{myAdminApiEndpoint}', username: '{username}').");

            // Reset authentication state.
            myAdminAPIKey = null;
            myAdminSessionId = null;
            MyAdminAPIIsAuthenticated = false;

            // Store connection parameters for re-authentication.
            myAdminAPIEndpoint = myAdminApiEndpoint;
            myAdminAPIUsername = username;
            myAdminAPIPassword = password;

            ValidateTimeoutSeconds(requestTimeoutSeconds);
            SetAsyncPolicyWrapForMyAdminAPICalls(requestTimeoutSeconds);

            try
            {
                var pollyContext = MyAdminAPIResilienceHelper.CreateContextWithMethodName(MethodName);

                await asyncMyAdminAPICallTimeoutAndRetryPolicyWrap.ExecuteAsync(async ctx =>
                {
                    await rateLimiter.WaitForPermitAsync(MethodName);
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(requestTimeoutSeconds));

                    // Create the MyAdmin API client and authenticate.
                    myAdminApi = new MyAdminInvoker(myAdminAPIEndpoint);
                    Dictionary<string, object> parameters = new Dictionary<string, object> { { "username", myAdminAPIUsername }, { "password", myAdminAPIPassword } };
                    ApiUser apiUser = await myAdminApi.InvokeAsync<ApiUser>("Authenticate", parameters, cts.Token);

                    if (apiUser != null)
                    {
                        myAdminAPIKey = apiUser.UserId;
                        myAdminSessionId = apiUser.SessionId;
                        MyAdminAPIIsAuthenticated = true;
                        logger.Info($"MyAdmin API authentication successful. User: '{myAdminAPIUsername}'.");
                    }
                    else
                    {
                        throw new MyAdminConnectionException("MyAdmin API authentication returned an invalid or null result.");
                    }
                }, pollyContext);
            }
            catch (Exception exception)
            {
                myAdminAPIKey = null;
                myAdminSessionId = null;
                MyAdminAPIIsAuthenticated = false;

                if (exceptionHelper.ExceptionIsRelatedToMyAdminConnectivityLoss(exception))
                {
                    throw new MyAdminConnectionException("An exception occurred while attempting to authenticate with the MyAdmin API.", exception);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Ensures that the MyAdmin API is authenticated before making API calls.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when not authenticated.</exception>
        void EnsureAuthenticated()
        {
            if (!MyAdminAPIIsAuthenticated || myAdminAPIKey == null || myAdminSessionId == null)
            {
                throw new InvalidOperationException($"MyAdmin API is not authenticated. Authenticate using the {nameof(AuthenticateMyAdminAPIAsync)} method first.");
            }
        }

        /// <inheritdoc/>
        public async Task<GetDeviceDatabaseNamesResult> GetDeviceDatabaseNamesAsync(IList<string> serialNumbers, int requestTimeoutSeconds = DefaultTimeoutSeconds)
        {
            const string MYAMethodName = "GetDeviceDatabaseNamesAsync";

            ArgumentNullException.ThrowIfNull(serialNumbers);

            // Return early if no serial numbers were provided.
            if (serialNumbers.Count == 0)
            {
                return new GetDeviceDatabaseNamesResult
                {
                    ApiDeviceDatabaseOwnerShareds = []
                };
            }

            logger.Debug($"Getting database names for {serialNumbers.Count} device(s).");

            EnsureAuthenticated();
            ValidateTimeoutSeconds(requestTimeoutSeconds);
            SetAsyncPolicyWrapForMyAdminAPICallsWithReauthentication(requestTimeoutSeconds);

            GetDeviceDatabaseNamesResult? result = null;

            try
            {
                var pollyContext = MyAdminAPIResilienceHelper.CreateContextWithMethodName(MYAMethodName);

                await asyncMyAdminAPICallWithReauthPolicyWrap!.ExecuteAsync(async ctx =>
                {
                    await rateLimiter.WaitForPermitAsync(MYAMethodName);
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(requestTimeoutSeconds));

                    // Build parameters for GetDeviceDatabaseNamesAsync method.
                    Dictionary<string, object> parameters = new()
                    {
                        { "apiKey", myAdminAPIKey! },
                        { "sessionId", myAdminSessionId! },
                        { "serialNumbers", serialNumbers }
                    };

                    var apiResult = await myAdminApi!.InvokeAsync<ApiDeviceDatabaseOwnerShared[]>(MYAMethodName, parameters, cts.Token);

                    result = new GetDeviceDatabaseNamesResult
                    {
                        ApiDeviceDatabaseOwnerShareds = apiResult
                    };

                    logger.Debug($"Successfully retrieved database info for {result.ApiDeviceDatabaseOwnerShareds?.Length ?? 0} device(s).");
                }, pollyContext);
            }
            catch (Exception exception)
            {
                if (exceptionHelper.ExceptionIsRelatedToMyAdminConnectivityLoss(exception))
                {
                    throw new MyAdminConnectionException("An exception occurred while attempting to get device database names.", exception);
                }

                // Return a failure result for other errors.
                result = new GetDeviceDatabaseNamesResult
                {
                    ApiDeviceDatabaseOwnerShareds = null,
                    ErrorMessage = exception.Message,
                    ErrorSource = ErrorSource.Middleware
                };

                logger.Error($"MyAdmin API {MYAMethodName} failed: {exception.Message}");
            }

            return result ?? throw new InvalidOperationException($"{MYAMethodName} failed to return a result.");
        }

        /// <inheritdoc/>
        public MyAdminAPIRateLimiterStatistics GetRateLimiterStatistics()
        {
            return rateLimiter.GetStatistics();
        }

        /// <inheritdoc/>
        public MyAdminAPIRateLimiterMethodStatistics GetRateLimiterStatistics(string methodName)
        {
            return rateLimiter.GetStatistics(methodName);
        }

        /// <summary>
        /// Re-authenticates with the MyAdmin API using stored credentials (myAdminAPIEndpoint, myAdminAPIUsername, myAdminAPIPassword). This method is thread-safe and will only allow one re-authentication at a time.
        /// </summary>
        async Task ReauthenticateAsync()
        {
            // Capture the current MyAdmin session generation before waiting for the lock.
            long generationAtEntry = Interlocked.Read(ref myAdminSessionGeneration);

            await reauthenticationLock.WaitAsync();
            try
            {
                // If the MyAdmin session generation has changed since we started waiting, another thread already re-authenticated.
                if (Interlocked.Read(ref myAdminSessionGeneration) != generationAtEntry)
                {
                    logger.Debug("Re-authentication skipped - another thread already re-authenticated.");
                    return;
                }

                // Check if stored credentials are available.
                if (string.IsNullOrEmpty(myAdminAPIEndpoint) || string.IsNullOrEmpty(myAdminAPIUsername) || string.IsNullOrEmpty(myAdminAPIPassword))
                {
                    throw new InvalidOperationException("Cannot re-authenticate: credentials or API endpoint not available. Call AuthenticateMyAdminAPIAsync first.");
                }

                logger.Info("Re-authenticating with MyAdmin API due to session expiry.");
                await AuthenticateMyAdminAPIAsync(myAdminAPIEndpoint, myAdminAPIUsername, myAdminAPIPassword);

                // Increment generation to signal that a new session was established.
                Interlocked.Increment(ref myAdminSessionGeneration);
            }
            finally
            {
                reauthenticationLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<ProvisionDeviceResult> ProvisionDeviceToAccountAsync(DbGdaQProvisionDevice dbGdaQProvisionDevice, int requestTimeoutSeconds = DefaultTimeoutSeconds)
        {
            const string MYAMethodName = "ProvisionDeviceToAccount";

            ArgumentNullException.ThrowIfNull(dbGdaQProvisionDevice);

            logger.Debug($"Provisioning device with ThirdPartyId '{dbGdaQProvisionDevice.ThirdPartyId}', ProductId '{dbGdaQProvisionDevice.ProductId}'.");

            EnsureAuthenticated();
            ValidateTimeoutSeconds(requestTimeoutSeconds);
            SetAsyncPolicyWrapForMyAdminAPICallsWithReauthentication(requestTimeoutSeconds);

            ProvisionDeviceResult result = null;

            try
            {
                var pollyContext = MyAdminAPIResilienceHelper.CreateContextWithMethodName(MYAMethodName);

                await asyncMyAdminAPICallWithReauthPolicyWrap.ExecuteAsync(async ctx =>
                {
                    await rateLimiter.WaitForPermitAsync(MYAMethodName);
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(requestTimeoutSeconds));

                    // Build parameters for ProvisionDeviceToAccount method (mandatory parameters only).
                    Dictionary<string, object> parameters = new()
                    {
                        { "apiKey", myAdminAPIKey },
                        { "sessionId", myAdminSessionId },
                        { "productId", dbGdaQProvisionDevice.ProductId }
                    };

                    // Add optional parameters only if they have values.
                    if (!string.IsNullOrEmpty(dbGdaQProvisionDevice.ErpNo))
                    {
                        parameters.Add("erpNo", dbGdaQProvisionDevice.ErpNo);
                    }

                    if (dbGdaQProvisionDevice.HardwareId.HasValue)
                    {
                        parameters.Add("hardwareId", dbGdaQProvisionDevice.HardwareId.Value);
                    }

                    if (!string.IsNullOrEmpty(dbGdaQProvisionDevice.PromoCode))
                    {
                        parameters.Add("promoCode", dbGdaQProvisionDevice.PromoCode);
                    }

                    if (!string.IsNullOrEmpty(dbGdaQProvisionDevice.SubPlan))
                    {
                        parameters.Add("subPlan", dbGdaQProvisionDevice.SubPlan);
                    }

                    var provisionResult = await myAdminApi.InvokeAsync<ProvisionResult>(MYAMethodName, parameters, cts.Token);

                    result = new ProvisionDeviceResult
                    {
                        IsSuccess = provisionResult.IsSuccess,
                        GeotabSerialNumber = provisionResult?.SerialNo ?? string.Empty,
                        ErrorMessage = provisionResult?.Error ?? string.Empty,
                        ErrorSource = provisionResult.IsSuccess ? ErrorSource.None : ErrorSource.MyAdminAPI
                    };

                    if (result.IsSuccess)
                    {
                        logger.Debug($"Device with ThirdPartyId '{dbGdaQProvisionDevice.ThirdPartyId}' provisioned successfully. SerialNumber: '{result.GeotabSerialNumber}'.");
                    }
                    else
                    {
                        logger.Warn($"Device with ThirdPartyId '{dbGdaQProvisionDevice.ThirdPartyId}' provisioning failed: {result.ErrorMessage}");
                    }
                }, pollyContext);
            }
            catch (Exception exception)
            {
                if (exceptionHelper.ExceptionIsRelatedToMyAdminConnectivityLoss(exception))
                {
                    throw new MyAdminConnectionException($"An exception occurred while attempting to provision device with ThirdPartyId '{dbGdaQProvisionDevice.ThirdPartyId}'.", exception);
                }

                // Return a failure result for other errors.
                result = new ProvisionDeviceResult
                {
                    IsSuccess = false,
                    GeotabSerialNumber = string.Empty,
                    ErrorMessage = exception.Message,
                    ErrorSource = ErrorSource.Middleware
                };

                logger.Error($"MyAdmin API ProvisionDeviceToAccount failed: {exception.Message}");
            }

            return result ?? throw new InvalidOperationException($"{MYAMethodName} failed to return a result for device with ThirdPartyId '{dbGdaQProvisionDevice.ThirdPartyId}'.");
        }

        /// <summary>
        /// Sets the async policy wrap for MyAdmin API calls (without re-authentication).
        /// Used for the initial authentication call.
        /// </summary>
        /// <param name="timeoutSeconds">The initial timeout in seconds.</param>
        void SetAsyncPolicyWrapForMyAdminAPICalls(int timeoutSeconds)
        {
            asyncMyAdminAPICallTimeoutAndRetryPolicyWrap ??= MyAdminAPIResilienceHelper.CreateAsyncPolicyWrapForMyAdminAPICalls<Exception>(
                timeoutSeconds,
                exceptionHelper,
                logger);
        }

        /// <summary>
        /// Sets the async policy wrap for MyAdmin API calls with automatic re-authentication on session expiry.
        /// Used for all API calls after initial authentication.
        /// </summary>
        /// <param name="timeoutSeconds">The initial timeout in seconds.</param>
        void SetAsyncPolicyWrapForMyAdminAPICallsWithReauthentication(int timeoutSeconds)
        {
            asyncMyAdminAPICallWithReauthPolicyWrap ??= MyAdminAPIResilienceHelper.CreateAsyncPolicyWrapForMyAdminAPICallsWithReauthentication<Exception>(
                timeoutSeconds,
                exceptionHelper,
                logger,
                ReauthenticateAsync,
                rateLimiter);
        }

        /// <summary>
        /// Validates that the timeout value meets the minimum requirement.
        /// </summary>
        /// <param name="timeoutSeconds">The timeout value to validate.</param>
        static void ValidateTimeoutSeconds(int timeoutSeconds)
        {
            if (timeoutSeconds < MinTimeoutSeconds)
            {
                throw new ArgumentException($"The supplied timeout value of {timeoutSeconds} seconds is lower than the minimum allowed timeout value of {MinTimeoutSeconds} seconds.");
            }
        }

        /// <summary>
        /// Test method to verify that the session expiry re-authentication policy works correctly.
        /// Throws a SessionExpiredException to trigger the Polly re-authentication policy.
        /// </summary>
        /// <param name="requestTimeoutSeconds">The timeout in seconds for the request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method is intended for testing purposes only. It will:
        /// 1. Throw an exception containing "SessionExpiredException"
        /// 2. Trigger the Polly session expiry policy
        /// 3. Call ReauthenticateAsync
        /// 4. Retry the operation (which will throw again, causing multiple re-auth attempts)
        /// 
        /// To properly test, set a breakpoint in ReauthenticateAsync or watch the logs.
        /// The method will eventually fail after MaxReauthenticationRetries attempts.
        /// </remarks>
        public async Task TestSessionExpiryReauthenticationAsync(int requestTimeoutSeconds = DefaultTimeoutSeconds)
        {
            const string MethodName = "TestSessionExpiry";

            logger.Info("Testing session expiry re-authentication policy...");

            EnsureAuthenticated();
            ValidateTimeoutSeconds(requestTimeoutSeconds);
            SetAsyncPolicyWrapForMyAdminAPICallsWithReauthentication(requestTimeoutSeconds);

            int attemptCount = 0;
            const int maxTestAttempts = 2; // Allow 1 re-auth, then succeed on retry

            try
            {
                var pollyContext = MyAdminAPIResilienceHelper.CreateContextWithMethodName(MethodName);

                await asyncMyAdminAPICallWithReauthPolicyWrap.ExecuteAsync(async ctx =>
                {
                    attemptCount++;
                    logger.Debug($"TestSessionExpiryReauthenticationAsync attempt {attemptCount}");

                    if (attemptCount <= maxTestAttempts - 1)
                    {
                        // Simulate session expiry on first attempt(s)
                        throw new Exception($"Simulated {MyAdminAPIResilienceHelper.MyAdminSessionExpiredExceptionMessage} for testing purposes.");
                    }

                    // On subsequent attempts after re-auth, succeed
                    logger.Info($"TestSessionExpiryReauthenticationAsync succeeded after {attemptCount} attempt(s) (re-authentication was triggered).");
                    await Task.CompletedTask;
                }, pollyContext);
            }
            catch (Exception exception)
            {
                logger.Error($"TestSessionExpiryReauthenticationAsync failed: {exception.Message}");
                throw;
            }
        }
    }
}