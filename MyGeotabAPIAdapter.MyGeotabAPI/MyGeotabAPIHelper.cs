using Geotab.Checkmate;
using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Exceptions;
using MyGeotabAPIAdapter.Logging;
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

        // For GetFeed result limts see <see href="https://geotab.github.io/sdk/software/api/reference/#M:Geotab.Checkmate.Database.DataStore.GetFeed1">GetFeed(...)</see>.
        public const int GetFeedResultLimitDefault = 50000;
        public const int GetFeedResultLimitDevice = 5000;
        public const int GetFeedResultLimitMediaFile = 10000;
        public const int GetFeedResultLimitRoute = 10000;
        public const int GetFeedResultLimitRule = 10000;
        public const int GetFeedResultLimitUser = 5000;
        public const int GetFeedResultLimitZone = 10000;

        readonly IExceptionHelper exceptionHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="MyGeotabAPIHelper"/> class.
        /// </summary>
        public MyGeotabAPIHelper(IExceptionHelper exceptionHelper)
        {
            this.exceptionHelper = exceptionHelper;
        }

        /// <inheritdoc/>
        public async Task<IList<T>> GetAsync<T>(API myGeotabApi, Search search = null, int timeoutSeconds = DefaultTimeoutSeconds)
        {
            // Obtain the type parameter type (for logging purposes).
            Type typeParameterType = typeof(T);
            try
            {
                CancellationTokenSource cancellationTokenSource = new();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                Task<IList<T>> dataListTask = Task.Run(() => myGeotabApi.CallAsync<IList<T>>("Get", typeof(T), new { search }), cancellationTokenSource.Token);

                return await dataListTask;
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
        }

        /// <inheritdoc/>
        public async Task<FeedResult<T>> GetFeedAsync<T>(API myGeotabApi, DateTime? fromDate = null, int resultsLimit = GetFeedResultLimitDefault, int timeoutSeconds = DefaultTimeoutSeconds) where T : Entity
        {
            // Obtain the type parameter type (for logging purposes).
            Type typeParameterType = typeof(T);

            try
            {
                CancellationTokenSource cancellationTokenSource = new();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                Task<FeedResult<T>> feedResultTask = Task.Run(() => myGeotabApi.CallAsync<FeedResult<T>>("GetFeed", typeof(T), new
                {
                    resultsLimit,
                    search = new
                    {
                        fromDate
                    }
                }), cancellationTokenSource.Token);

                return await feedResultTask;
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
        }

        /// <inheritdoc/>
        public async Task<FeedResult<T>> GetFeedAsync<T>(API myGeotabApi, long fromVersion, int resultsLimit = GetFeedResultLimitDefault, int timeoutSeconds = DefaultTimeoutSeconds) where T : Entity
        {
            // Obtain the type parameter type (for logging purposes).
            Type typeParameterType = typeof(T);

            try
            {
                CancellationTokenSource cancellationTokenSource = new();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                Task<FeedResult<T>> feedResultTask = Task.Run(() => myGeotabApi.CallAsync<FeedResult<T>>("GetFeed", typeof(T), new
                {
                    fromVersion,
                    resultsLimit
                }), cancellationTokenSource.Token);

                return await feedResultTask;
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
        }

        /// <inheritdoc/>
        public async Task<object> SetAsync<T>(API myGeotabApi, T entity, int timeoutSeconds = DefaultTimeoutSeconds)
        {
            // Obtain the type parameter type (for logging purposes).
            Type typeParameterType = typeof(T);

            try
            {
                CancellationTokenSource cancellationTokenSource = new();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                Task<object> setTask = Task.Run(() => myGeotabApi.CallAsync<object>("Set", typeof(T), new { entity }), cancellationTokenSource.Token);

                return await setTask;
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
        }
    }
}
