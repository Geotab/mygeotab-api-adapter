using Geotab.Checkmate;
using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Exceptions;
using MyGeotabAPIAdapter.Exceptions;
using MyGeotabAPIAdapter.Logging;
using NLog;
using Polly;
using Polly.Wrap;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.MyGeotabAPI
{
    /// <summary>
    /// A helper class to assist in working with the MyGeotab API (.NET API wrapper - <see cref="Geotab.Checkmate.ObjectModel"/>).
    /// </summary>
    public class MyGeotabAPIHelper : IMyGeotabAPIHelper
    {
        public const int DefaultTimeoutSeconds = 30;
        public const int MinTimeoutSeconds = 30;

        // For GetFeed result limts see <see href="https://geotab.github.io/sdk/software/api/reference/#M:Geotab.Checkmate.Database.DataStore.GetFeed1">GetFeed(...)</see>.
        public const int GetFeedResultLimitDefaultConst = 50000;
        public const int GetFeedResultLimitDeviceConst = 5000;
        public const int GetFeedResultLimitMediaFileConst = 10000;
        public const int GetFeedResultLimitRouteConst = 10000;
        public const int GetFeedResultLimitRuleConst = 10000;
        public const int GetFeedResultLimitUserConst = 5000;
        public const int GetFeedResultLimitZoneConst = 10000;

        // Polly-related items:
        AsyncPolicyWrap asyncMyGeotabAPICallTimeoutAndRetryPolicyWrap;

        readonly IExceptionHelper exceptionHelper;
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <inheritdoc/>
        public int GetFeedResultLimitDefault { get => GetFeedResultLimitDefaultConst; }

        /// <inheritdoc/>
        public int GetFeedResultLimitDevice { get => GetFeedResultLimitDeviceConst; }

        /// <inheritdoc/>
        public int GetFeedResultLimitMediaFile { get => GetFeedResultLimitMediaFileConst; }

        /// <inheritdoc/>
        public int GetFeedResultLimitRoute { get => GetFeedResultLimitRouteConst; }

        /// <inheritdoc/>
        public int GetFeedResultLimitRule { get => GetFeedResultLimitRuleConst; }

        /// <inheritdoc/>
        public int GetFeedResultLimitUser { get => GetFeedResultLimitUserConst; }

        /// <inheritdoc/>
        public int GetFeedResultLimitZone { get => GetFeedResultLimitZoneConst; }

        /// <inheritdoc/>
        public API MyGeotabAPI { get; private set; }

        /// <inheritdoc/>
        public bool MyGeotabAPIIsAuthenticated
        {
            get
            {
                if (MyGeotabAPI != null && MyGeotabAPI.SessionId != null)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MyGeotabAPIHelper"/> class.
        /// </summary>
        public MyGeotabAPIHelper(IExceptionHelper exceptionHelper)
        {
            this.exceptionHelper = exceptionHelper;
        }

        /// <inheritdoc/>
        public async Task AuthenticateMyGeotabApiAsync(string userName, string password, string database, string server, int requestTimeoutSeconds = DefaultTimeoutSeconds)
        {
            logger.Info($"Authenticating MyGeotab API (server:'{server}', database:'{database}', user:'{userName}').");

            ValidateTimeoutSeconds(requestTimeoutSeconds);
            var requestTimeoutMilliseconds = 0;
            if (requestTimeoutSeconds > 0)
            { 
                requestTimeoutMilliseconds = requestTimeoutSeconds * 1000;
            }
            SetAsyncPolicyWrapForMyGeotabAPICalls(requestTimeoutSeconds);

            MyGeotabAPI = new API(userName, password, null, database, server, requestTimeoutMilliseconds);

            try
            {
                await asyncMyGeotabAPICallTimeoutAndRetryPolicyWrap.ExecuteAsync(async pollyContext =>
                {
                    await MyGeotabAPI.AuthenticateAsync();
                }, new Context());
            }
            catch (Exception exception)
            {
                // If the exception is related to connectivity, wrap it in a MyGeotabConnectionException. Otherwise, just pass it along.
                if (exceptionHelper.ExceptionIsRelatedToMyGeotabConnectivityLoss(exception))
                {
                    throw new MyGeotabConnectionException("An exception occurred while attempting to authenticate the MyGeotab API.", exception);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<IList<T>> GetAsync<T>(Search search = null, int timeoutSeconds = DefaultTimeoutSeconds) where T : class, IEntity
        {
            ValidateTimeoutSeconds(timeoutSeconds);
            SetAsyncPolicyWrapForMyGeotabAPICalls(timeoutSeconds);

            // Obtain the type parameter type (for logging purposes).
            Type typeParameterType = typeof(T);

            IList<T> result = null;
            try
            {
                await asyncMyGeotabAPICallTimeoutAndRetryPolicyWrap.ExecuteAsync(async pollyContext =>
                {
                    using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                    {
                        result = await MyGeotabAPI.CallAsync<IList<T>>("Get", typeof(T), new { search }, cancellationTokenSource.Token);
                    }
                }, new Context());
            }
            catch (OperationCanceledException exception)
            {
                throw new MyGeotabConnectionException($"MyGeotab API GetAsync call for type '{typeParameterType.Name}' did not return within the allowed time of {timeoutSeconds} seconds. This may be due to a loss of connectivity with the MyGeotab server.", exception);
            }
            catch (Exception exception)
            {
                // If the exception is related to connectivity, wrap it in a MyGeotabConnectionException. Otherwise, just pass it along.
                if (exceptionHelper.ExceptionIsRelatedToMyGeotabConnectivityLoss(exception))
                {
                    throw new MyGeotabConnectionException("An exception occurred while attempting to get data from the Geotab API via CallAsync (Get).", exception);
                }
                else
                {
                    throw;
                }
            }

            if (result != null)
            { 
                return result;
            }
            throw new Exception($"GetAsync<T> method failed to return a result for entity type '{typeof(T).Name}'.");
        }

        /// <inheritdoc/>
        public async Task<FeedResult<T>> GetFeedAsync<T>(DateTime? fromDate = null, int resultsLimit = GetFeedResultLimitDefaultConst, int timeoutSeconds = DefaultTimeoutSeconds) where T : class, IEntity
        {
            ValidateTimeoutSeconds(timeoutSeconds);
            SetAsyncPolicyWrapForMyGeotabAPICalls(timeoutSeconds);

            // Obtain the type parameter type (for logging purposes).
            Type typeParameterType = typeof(T);

            FeedResult<T> result = null;
            try
            {
                if (typeParameterType.Name == nameof(DutyStatusLog))
                {
                    // Use a DutyStatusLogSearch with IncludeModifications set to true to include modification history of the DutyStatusLog results.
                    await asyncMyGeotabAPICallTimeoutAndRetryPolicyWrap.ExecuteAsync(async pollyContext =>
                    {
                        using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                        {
                            result = await MyGeotabAPI.CallAsync<FeedResult<T>>("GetFeed", typeof(T), new
                            {
                                search = new DutyStatusLogSearch
                                {
                                    FromDate = fromDate,
                                    IncludeModifications = true
                                },
                                resultsLimit
                            }, cancellationTokenSource.Token);
                        }
                    }, new Context());
                }
                else if (typeParameterType.Name == nameof(ExceptionEvent))
                {
                    // Use an ExceptionEventSearch to enrure that invalidated ExceptionEvents are included in the data feed (since they are not by default).
                    await asyncMyGeotabAPICallTimeoutAndRetryPolicyWrap.ExecuteAsync(async pollyContext =>
                    {
                        using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                        {
                            result = await MyGeotabAPI.CallAsync<FeedResult<T>>("GetFeed", typeof(T), new
                            {
                                search = new ExceptionEventSearch
                                {
                                    FromDate = fromDate,
                                    IncludeInvalidated = true
                                },
                                resultsLimit
                            }, cancellationTokenSource.Token);
                        }
                    }, new Context());
                }
                else if (typeParameterType.Name == nameof(Trip))
                {
                    // Use a TripSearch to enrure that deleted Trips will be included in the data feed (since they are not by default).
                    await asyncMyGeotabAPICallTimeoutAndRetryPolicyWrap.ExecuteAsync(async pollyContext =>
                    {
                        using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                        {
                            result = await MyGeotabAPI.CallAsync<FeedResult<T>>("GetFeed", typeof(T), new
                            {
                                search = new TripSearch
                                {
                                    FromDate = fromDate,
                                    IncludeDeleted = true
                                },
                                resultsLimit
                            }, cancellationTokenSource.Token);
                        }
                    }, new Context());
                }
                else
                {
                    // Use the default search that includes FromDate only.
                    await asyncMyGeotabAPICallTimeoutAndRetryPolicyWrap.ExecuteAsync(async pollyContext =>
                    {
                        using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                        {
                            result = await MyGeotabAPI.CallAsync<FeedResult<T>>("GetFeed", typeof(T), new
                            {
                                resultsLimit,
                                search = new
                                {
                                    fromDate
                                }
                            }, cancellationTokenSource.Token);
                        }
                    }, new Context());
                }
            }
            catch (OperationCanceledException exception)
            {
                throw new MyGeotabConnectionException($"MyGeotab API GetFeedAsync call for type '{typeParameterType.Name}' did not return within the allowed time of {timeoutSeconds} seconds. This may be due to a loss of connectivity with the MyGeotab server.", exception);
            }
            catch (Exception exception)
            {
                // If the exception is related to connectivity, wrap it in a MyGeotabConnectionException. Otherwise, just pass it along.
                if (exceptionHelper.ExceptionIsRelatedToMyGeotabConnectivityLoss(exception))
                {
                    throw new MyGeotabConnectionException("An exception occurred while attempting to get data from the Geotab API via CallAsync (GetFeed).", exception);
                }
                else
                {
                    throw;
                }
            }

            if (result != null)
            {
                return result;
            }
            throw new Exception($"GetFeedAsync<T>(fromDate...) method failed to return a result for entity type '{typeof(T).Name}'.");
        }

        /// <inheritdoc/>
        public async Task<FeedResult<T>> GetFeedAsync<T>(long? fromVersion, int resultsLimit = GetFeedResultLimitDefaultConst, int timeoutSeconds = DefaultTimeoutSeconds) where T : class, IEntity
        {
            ValidateTimeoutSeconds(timeoutSeconds);
            SetAsyncPolicyWrapForMyGeotabAPICalls(timeoutSeconds);

            // Obtain the type parameter type (for logging purposes).
            Type typeParameterType = typeof(T);

            FeedResult<T> result = null;
            try
            {
                if (typeParameterType.Name == nameof(DutyStatusLog))
                {
                    // Use a DutyStatusLogSearch with IncludeModifications set to true to include modification history of the DutyStatusLog results.
                    await asyncMyGeotabAPICallTimeoutAndRetryPolicyWrap.ExecuteAsync(async pollyContext =>
                    {
                        using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                        {
                            result = await MyGeotabAPI.CallAsync<FeedResult<T>>("GetFeed", typeof(T), new
                            {
                                search = new DutyStatusLogSearch
                                {
                                    IncludeModifications = true
                                },
                                fromVersion,
                                resultsLimit
                            }, cancellationTokenSource.Token);
                        }
                    }, new Context());
                }
                else if (typeParameterType.Name == nameof(ExceptionEvent))
                {
                    // Use an ExceptionEventSearch to enrure that invalidated ExceptionEvents are included in the data feed (since they are not by default).
                    await asyncMyGeotabAPICallTimeoutAndRetryPolicyWrap.ExecuteAsync(async pollyContext =>
                    {
                        using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                        {
                            result = await MyGeotabAPI.CallAsync<FeedResult<T>>("GetFeed", typeof(T), new
                            {
                                search = new ExceptionEventSearch
                                {
                                    IncludeInvalidated = true
                                },
                                fromVersion,
                                resultsLimit
                            }, cancellationTokenSource.Token);
                        }
                    }, new Context());
                }
                else if (typeParameterType.Name == nameof(Trip))
                {
                    // Use an TripSearch to enrure that deleted Trips are included in the data feed (since they are not by default).
                    await asyncMyGeotabAPICallTimeoutAndRetryPolicyWrap.ExecuteAsync(async pollyContext =>
                    {
                        using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                        {
                            result = await MyGeotabAPI.CallAsync<FeedResult<T>>("GetFeed", typeof(T), new
                            {
                                search = new TripSearch
                                {
                                    IncludeDeleted = true
                                },
                                fromVersion,
                                resultsLimit
                            }, cancellationTokenSource.Token);
                        }
                    }, new Context());
                }
                else
                {
                    // Use the default search that includes FromVersion only.
                    await asyncMyGeotabAPICallTimeoutAndRetryPolicyWrap.ExecuteAsync(async pollyContext =>
                    {
                        using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                        {
                            result = await MyGeotabAPI.CallAsync<FeedResult<T>>("GetFeed", typeof(T), new
                            {
                                fromVersion,
                                resultsLimit
                            }, cancellationTokenSource.Token);
                        }
                    }, new Context());
                }
            }
            catch (OperationCanceledException exception)
            {
                throw new MyGeotabConnectionException($"MyGeotab API GetFeedAsync call for type '{typeParameterType.Name}' did not return within the allowed time of {timeoutSeconds} seconds. This may be due to a loss of connectivity with the MyGeotab server.", exception);
            }
            catch (Exception exception)
            {
                // If the exception is related to connectivity, wrap it in a MyGeotabConnectionException. Otherwise, just pass it along.
                if (exceptionHelper.ExceptionIsRelatedToMyGeotabConnectivityLoss(exception))
                {
                    throw new MyGeotabConnectionException("An exception occurred while attempting to get data from the Geotab API via CallAsync (GetFeed).", exception);
                }
                else
                {
                    throw;
                }
            }

            if (result != null)
            {
                return result;
            }
            throw new Exception($"GetFeedAsync<T>(fromVersion...) method failed to return a result for entity type '{typeof(T).Name}'.");
        }

        /// <inheritdoc/>
        public async Task<VersionInformation> GetVersionInformationAsync(int timeoutSeconds = DefaultTimeoutSeconds)
        {
            ValidateTimeoutSeconds(timeoutSeconds);
            SetAsyncPolicyWrapForMyGeotabAPICalls(timeoutSeconds);

            VersionInformation result = null;
            try
            {
                await asyncMyGeotabAPICallTimeoutAndRetryPolicyWrap.ExecuteAsync(async pollyContext =>
                {
                    using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                    {
                        result = await MyGeotabAPI.CallAsync<VersionInformation>("GetVersionInformation", null, cancellationTokenSource.Token);
                    }
                }, new Context());
            }
            catch (OperationCanceledException exception)
            {
                throw new MyGeotabConnectionException($"MyGeotab API GetVersionInformationAsync call did not return within the allowed time of {timeoutSeconds} seconds. This may be due to a loss of connectivity with the MyGeotab server.", exception);
            }
            catch (Exception exception)
            {
                // If the exception is related to connectivity, wrap it in a MyGeotabConnectionException. Otherwise, just pass it along.
                if (exceptionHelper.ExceptionIsRelatedToMyGeotabConnectivityLoss(exception))
                {
                    throw new MyGeotabConnectionException("An exception occurred while attempting to get data from the Geotab API via CallAsync (GetVersionInformation).", exception);
                }
                else
                {
                    throw;
                }
            }

            if (result != null)
            {
                return result;
            }
            throw new Exception($"GetVersionInformationAsync method failed to return a result.");
        }

        /// <inheritdoc/>
        public async Task<object> SetAsync<T>(T entity, int timeoutSeconds = DefaultTimeoutSeconds) where T : class, IEntity
        {
            ValidateTimeoutSeconds(timeoutSeconds);
            SetAsyncPolicyWrapForMyGeotabAPICalls(timeoutSeconds);

            // Obtain the type parameter type (for logging purposes).
            Type typeParameterType = typeof(T);

            object result = null;
            try
            {
                await asyncMyGeotabAPICallTimeoutAndRetryPolicyWrap.ExecuteAsync(async pollyContext =>
                {
                    using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                    {
                        result = await MyGeotabAPI.CallAsync<object>("Set", typeof(T), new { entity }, cancellationTokenSource.Token);
                    }
                }, new Context());
            }
            catch (OperationCanceledException exception)
            {
                throw new MyGeotabConnectionException($"MyGeotab API GetAsync call for type '{typeParameterType.Name}' did not return within the allowed time of {timeoutSeconds} seconds. This may be due to a loss of connectivity with the MyGeotab server.", exception);
            }
            catch (Exception exception)
            {
                // If the exception is related to connectivity, wrap it in a MyGeotabConnectionException. Otherwise, just pass it along.
                if (exceptionHelper.ExceptionIsRelatedToMyGeotabConnectivityLoss(exception))
                {
                    throw new MyGeotabConnectionException("An exception occurred while attempting to get data from the Geotab API via CallAsync (Set).", exception);
                }
                else
                {
                    throw;
                }
            }

            if (result != null)
            {
                return result;
            }
            throw new Exception($"SetAsync<T> method failed to return a result for entity type '{typeof(T).Name}'.");
        }

        /// <summary>
        /// Sets the <see cref="asyncMyGeotabAPICallTimeoutAndRetryPolicyWrap"/> if it hasn't already been set.
        /// </summary>
        /// <param name="timeoutSeconds">The initial timeout, in seconds, to be applied to the first MyGeotab API call retry attempt.</param>
        void SetAsyncPolicyWrapForMyGeotabAPICalls(int timeoutSeconds)
        {
            if (asyncMyGeotabAPICallTimeoutAndRetryPolicyWrap == null)
            {
                // Setup a MyGeotab API call timeout and retry policy wrap.
                asyncMyGeotabAPICallTimeoutAndRetryPolicyWrap = MyGeotabAPIResilienceHelper.CreateAsyncPolicyWrapForMyGeotabAPICalls<Exception>(timeoutSeconds, (ExceptionHelper)exceptionHelper, logger);
            }
        }

        /// <summary>
        /// Ensures that <paramref name="timeoutSeconds"/> is no less than <see cref="MinTimeoutSeconds"/> and throws an <see cref="ArgumentException"/> if otherwise.
        /// </summary>
        /// <param name="timeoutSeconds">The value to be validated.</param>
        static void ValidateTimeoutSeconds(int timeoutSeconds)
        {
            if (timeoutSeconds < MinTimeoutSeconds)
            {
                throw new ArgumentException($"The supplied timeout value of {timeoutSeconds} seconds is lower than the minimum allowed timeout value of {MinTimeoutSeconds} seconds.");
            }
        }
    }
}
