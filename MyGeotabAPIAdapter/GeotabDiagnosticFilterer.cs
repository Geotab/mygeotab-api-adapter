using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.MyGeotabAPI;
using NLog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// A class that is used to filter lists of <see cref="Entity"/>s that are related to <see cref="Diagnostic"/>s based on the <see cref="AdapterConfiguration.DiagnosticsToTrackList"/> and the <see cref="AdapterConfiguration.ExcludeDiagnosticsToTrack"/> setting value.
    /// </summary>
    internal class GeotabDiagnosticFilterer : IGeotabDiagnosticFilterer
    {
        static string CurrentClassName { get => nameof(GeotabDiagnosticFilterer); }

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IGenericGeotabObjectFiltererBase<Diagnostic> genericGeotabObjectFiltererBase;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;

        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <inheritdoc/>
        public string Id { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeotabDiagnosticFilterer"/> class.
        /// </summary>
        public GeotabDiagnosticFilterer(IAdapterConfiguration adapterConfiguration, IGenericGeotabObjectFiltererBase<Diagnostic> genericGeotabObjectFiltererBase, IMyGeotabAPIHelper myGeotabAPIHelper)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.adapterConfiguration = adapterConfiguration;
            this.genericGeotabObjectFiltererBase = genericGeotabObjectFiltererBase;
            this.myGeotabAPIHelper = myGeotabAPIHelper;

            Id = Guid.NewGuid().ToString();
            logger.Debug($"{nameof(GeotabDiagnosticFilterer)} [Id: {Id}] created.");
            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public async Task<List<T>> ApplyDiagnosticFilterAsync<T>(CancellationTokenSource cancellationTokenSource, List<T> entitiesToBeFiltered) where T : Entity
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            if (genericGeotabObjectFiltererBase.IsInitialized == false)
            {
                await genericGeotabObjectFiltererBase.InitializeAsync(cancellationTokenSource, adapterConfiguration.DiagnosticsToTrackList, adapterConfiguration.DiagnosticCacheIntervalDailyReferenceStartTimeUTC, adapterConfiguration.DiagnosticCacheUpdateIntervalMinutes, adapterConfiguration.DiagnosticCacheRefreshIntervalMinutes, myGeotabAPIHelper.GetFeedResultLimitDefault, true);
            }

            var filteredEntities = new List<T>();
            if (genericGeotabObjectFiltererBase.GeotabObjectsToFilterOn.IsEmpty)
            {
                // No specific Diagnostics are being tracked. All entities in the list should be kept.
                filteredEntities.AddRange(entitiesToBeFiltered);
            }
            else
            {
                // Certain Diagnostics are being tracked. Iterate through the list of entities to be filtered and keep only those that represent Diagnostics that are being tracked.
                string entityTypeName = typeof(T).Name;
                foreach (var entityToBeEvaluated in entitiesToBeFiltered)
                {
                    Diagnostic entityToBeEvaluatedDiagnostic = NoDiagnostic.Value;
                    string errorMessage = "";
                    switch (entityTypeName)
                    {
                        case nameof(FaultData):
                            var faultDataToBeEvaluated = entityToBeEvaluated as FaultData;
                            entityToBeEvaluatedDiagnostic = faultDataToBeEvaluated.Diagnostic;
                            break;
                        case nameof(StatusData):
                            var statusDataToBeEvaluated = entityToBeEvaluated as StatusData;
                            entityToBeEvaluatedDiagnostic = statusDataToBeEvaluated.Diagnostic;
                            break;
                        default:
                            errorMessage = $"The entity type '{entityTypeName}' is not supported by the '{methodBase.ReflectedType.Name}' method.";
                            logger.Error(errorMessage);
                            throw new Exception(errorMessage);
                    }

                    if (entityToBeEvaluatedDiagnostic != null)
                    {
                        if (adapterConfiguration.ExcludeDiagnosticsToTrack == false)
                        {
                            // If ExcludeDiagnosticsToTrack is false, then the entities that are returned should be those that match the DiagnosticsToTrackList.
                            if (genericGeotabObjectFiltererBase.GeotabObjectsToFilterOn.ContainsKey(entityToBeEvaluatedDiagnostic.Id))
                            {
                                filteredEntities.Add(entityToBeEvaluated);
                            }
                        }
                        else
                        {
                            // If ExcludeDiagnosticsToTrack is true, then the entities that are returned should be those that do not match the DiagnosticsToTrackList.
                            if (genericGeotabObjectFiltererBase.GeotabObjectsToFilterOn.ContainsKey(entityToBeEvaluatedDiagnostic.Id) == false)
                            {
                                filteredEntities.Add(entityToBeEvaluated);
                            }
                        }
                    }
                }
            }
            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return filteredEntities;
        }
    }
}
