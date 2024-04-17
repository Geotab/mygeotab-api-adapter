using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.MyGeotabAPI;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// A data reduction class that is used to reduce lists of <see cref="Entity"/>s by applying the configured <see cref="AdapterConfiguration.MinimumIntervalSamplingIntervalSeconds"/> setting to ensure that a minimum of the specified interval exists between each entity that is kept for later downstream persistence for the subject <see cref="Device"/> + <see cref="Entity"/> type (+ <see cref="Diagnostic"/> if applicable) combination."/>
    /// </summary>
    /// <typeparam name="T">The type of Geotab <see cref="Entity"/> to be processed by this <see cref="MinimumIntervalSampler{T}"/> instance.</typeparam>
    internal class MinimumIntervalSampler<T> : IMinimumIntervalSampler<T> where T : Entity
    {
        static string CurrentClassName { get => nameof(MinimumIntervalSampler<T>); }

        const string WildcardString = "*";

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;

        readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly HashSet<string> minimumIntervalSamplingDiagnosticIds;
        private readonly ConcurrentDictionary<string, DateTime> sampleIntervalTrackingDictionary;
        
        /// <inheritdoc/>
        public string Id { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MinimumIntervalSampler"/> class.
        /// </summary>
        public MinimumIntervalSampler(IAdapterConfiguration adapterConfiguration, IMyGeotabAPIHelper myGeotabAPIHelper)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.adapterConfiguration = adapterConfiguration;
            this.myGeotabAPIHelper = myGeotabAPIHelper;

            minimumIntervalSamplingDiagnosticIds = new HashSet<string>(adapterConfiguration.MinimumIntervalSamplingDiagnosticsList.Split(','));

            sampleIntervalTrackingDictionary = new ConcurrentDictionary<string, DateTime>();

            Id = Guid.NewGuid().ToString();
            logger.Debug($"{CurrentClassName} [Id: {Id}] created.");
            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public async Task<List<T>> ApplyMinimumIntervalAsync(CancellationTokenSource cancellationTokenSource, List<T> entitiesToBeFiltered)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var filteredEntities = new List<T>();
            string entityTypeName = typeof(T).Name;
            string errorMessage = string.Empty;

            // Return the full list of entitiesToBeFiltered if minimum interval sampling is not configured.
            switch (entityTypeName)
            {
                case nameof(LogRecord):
                    if (adapterConfiguration.EnableMinimunIntervalSamplingForLogRecords == false)
                    {
                        return entitiesToBeFiltered;
                    }
                    break;
                case nameof(StatusData):
                    if (adapterConfiguration.EnableMinimunIntervalSamplingForStatusData == false)
                    {
                        return entitiesToBeFiltered;
                    }
                    if (adapterConfiguration.EnableMinimunIntervalSamplingForStatusData == true && (adapterConfiguration.MinimumIntervalSamplingDiagnosticsList == string.Empty || adapterConfiguration.MinimumIntervalSamplingDiagnosticsList == WildcardString))
                    {
                        return entitiesToBeFiltered;
                    }
                    break;
                default:
                    errorMessage = $"The entity type '{entityTypeName}' is not supported by the '{methodBase.ReflectedType.Name}' method.";
                    logger.Error(errorMessage);
                    throw new Exception(errorMessage);
            }

            // Since minimum interval sampling is configured, iterate through the entitiesToBeFiltered and apply minimum interval sampling.
            foreach (var entityToBeEvaluated in entitiesToBeFiltered)
            {
                // Build the key for use with the sampleIntervalTrackingDictionary.
                Device entityToBeEvaluatedDevice = NoDevice.Value;
                Diagnostic entityToBeEvaluatedDiagnostic = NoDiagnostic.Value;
                DateTime entityToBeEvaluatedDateTime = DateTime.MinValue;
                string sampleIntervalTrackingKey = string.Empty;
                errorMessage = string.Empty;
                switch (entityTypeName)
                {
                    case nameof(LogRecord):
                        var logRecordToBeEvaluated = entityToBeEvaluated as LogRecord;
                        entityToBeEvaluatedDevice = logRecordToBeEvaluated.Device;
                        entityToBeEvaluatedDateTime = (DateTime)logRecordToBeEvaluated.DateTime;
                        sampleIntervalTrackingKey = $"[Device:{entityToBeEvaluatedDevice.Id}]";
                        break;
                    case nameof(StatusData):
                        var statusDataToBeEvaluated = entityToBeEvaluated as StatusData;
                        entityToBeEvaluatedDevice = statusDataToBeEvaluated.Device;
                        entityToBeEvaluatedDateTime = (DateTime)statusDataToBeEvaluated.DateTime;
                        entityToBeEvaluatedDiagnostic = statusDataToBeEvaluated.Diagnostic;
                        sampleIntervalTrackingKey = $"[Device:{entityToBeEvaluatedDevice.Id}] [Diagnostic:{entityToBeEvaluatedDiagnostic.Id}]";
                        break;
                    default:
                        errorMessage = $"The entity type '{entityTypeName}' is not supported by the '{methodBase.ReflectedType.Name}' method.";
                        logger.Error(errorMessage);
                        throw new Exception(errorMessage);
                }

                // If processing StatusData, simply include the entityToBeEvaluated if its diagnostic is not configured for minimum interval sampling (i.e. don't bother with filtering logic as it shouldn't be filtered).
                if (entityTypeName == nameof(StatusData) && minimumIntervalSamplingDiagnosticIds.Contains(entityToBeEvaluatedDiagnostic.Id.ToString()) == false)
                {
                    filteredEntities.Add(entityToBeEvaluated);
                    continue;
                }

                // Get the last sampled DateTime for the Device + EntityType (+ Diagnostic if applicable).
                var previouslySampledDateTime = sampleIntervalTrackingDictionary.GetOrAdd(sampleIntervalTrackingKey, DateTime.MinValue);

                // Determine if the entityToBeEvaluated should be kept.
                if (entityToBeEvaluatedDateTime > previouslySampledDateTime.AddSeconds(adapterConfiguration.MinimumIntervalSamplingIntervalSeconds))
                {
                    // The entityToBeEvaluated should be kept. Update the sampleIntervalTrackingDictionary.
                    sampleIntervalTrackingDictionary.AddOrUpdate(sampleIntervalTrackingKey, entityToBeEvaluatedDateTime, (key, oldValue) => entityToBeEvaluatedDateTime);
                    filteredEntities.Add(entityToBeEvaluated);
                }
            }

            logger.Debug($"{CurrentClassName} [Id: {Id}] filtered {entitiesToBeFiltered.Count} {entityTypeName} entities down to {filteredEntities.Count} entities.");
            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return filteredEntities;
        }
    }
}
