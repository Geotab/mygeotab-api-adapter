using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Charging;
using Geotab.Checkmate.ObjectModel.Engine;
using Geotab.Checkmate.ObjectModel.Exceptions;
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
    /// A class that is used to filter lists of <see cref="Entity"/>s that are related to <see cref="Device"/>s based on the <see cref="AdapterConfiguration.DevicesToTrackList"/>.
    /// </summary>
    internal class GeotabDeviceFilterer : IGeotabDeviceFilterer
    {
        static string CurrentClassName { get => nameof(GeotabDeviceFilterer); }

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IGenericGeotabObjectFiltererBase<Device> genericGeotabObjectFiltererBase;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;

        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <inheritdoc/>
        public string Id { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeotabDeviceFilterer"/> class.
        /// </summary>
        public GeotabDeviceFilterer(IAdapterConfiguration adapterConfiguration, IGenericGeotabObjectFiltererBase<Device> genericGeotabObjectFiltererBase, IMyGeotabAPIHelper myGeotabAPIHelper)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.adapterConfiguration = adapterConfiguration;
            this.genericGeotabObjectFiltererBase = genericGeotabObjectFiltererBase;
            this.myGeotabAPIHelper = myGeotabAPIHelper;

            Id = Guid.NewGuid().ToString();
            logger.Debug($"{nameof(GeotabDeviceFilterer)} [Id: {Id}] created.");
            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public async Task<List<T>> ApplyDeviceFilterAsync<T>(CancellationTokenSource cancellationTokenSource, List<T> entitiesToBeFiltered) where T : Entity
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            if (genericGeotabObjectFiltererBase.IsInitialized == false)
            {
                await genericGeotabObjectFiltererBase.InitializeAsync(cancellationTokenSource, adapterConfiguration.DevicesToTrackList, adapterConfiguration.DeviceCacheIntervalDailyReferenceStartTimeUTC, adapterConfiguration.DeviceCacheUpdateIntervalMinutes, adapterConfiguration.DeviceCacheRefreshIntervalMinutes, myGeotabAPIHelper.GetFeedResultLimitDevice, true);
            }

            var filteredEntities = new List<T>();
            if (genericGeotabObjectFiltererBase.GeotabObjectsToFilterOn.IsEmpty)
            {
                // No specific Devices are being tracked. All entities in the list should be kept.
                filteredEntities.AddRange(entitiesToBeFiltered);
            }
            else
            {
                // Certain Devices are being tracked. Iterate through the list of entities to be filtered and keep only those that represent Devices that are being tracked.
                string entityTypeName = typeof(T).Name;
                foreach (var entityToBeEvaluated in entitiesToBeFiltered)
                {
                    Device entityToBeEvaluatedDevice = NoDevice.Value;
                    string errorMessage = "";
                    switch (entityTypeName)
                    {
                        case nameof(Geotab.Checkmate.ObjectModel.BinaryData):
                            var binaryDataToBeEvaluated = entityToBeEvaluated as Geotab.Checkmate.ObjectModel.BinaryData;
                            entityToBeEvaluatedDevice = binaryDataToBeEvaluated.Device;
                            break;
                        case nameof(ChargeEvent):
                            var chargeEventToBeEvaluated = entityToBeEvaluated as ChargeEvent;
                            entityToBeEvaluatedDevice = chargeEventToBeEvaluated.Device;
                            break;
                        case nameof(DeviceStatusInfo):
                            var deviceStatusInfoToBeEvaluated = entityToBeEvaluated as DeviceStatusInfo;
                            entityToBeEvaluatedDevice = deviceStatusInfoToBeEvaluated.Device;
                            break;
                        case nameof(DriverChange):
                            var driverChangeToBeEvaluated = entityToBeEvaluated as DriverChange;
                            entityToBeEvaluatedDevice = driverChangeToBeEvaluated.Device;
                            break;
                        case nameof(DVIRLog):
                            var dvirLogToBeEvaluated = entityToBeEvaluated as DVIRLog;
                            entityToBeEvaluatedDevice = dvirLogToBeEvaluated.Device;
                            break;
                        case nameof(ExceptionEvent):
                            var exceptionEventToBeEvaluated = entityToBeEvaluated as ExceptionEvent;
                            entityToBeEvaluatedDevice = exceptionEventToBeEvaluated.Device;
                            break;
                        case nameof(FaultData):
                            var faultDataToBeEvaluated = entityToBeEvaluated as FaultData;
                            entityToBeEvaluatedDevice = faultDataToBeEvaluated.Device;
                            break;
                        case nameof(LogRecord):
                            var logRecordToBeEvaluated = entityToBeEvaluated as LogRecord;
                            entityToBeEvaluatedDevice = logRecordToBeEvaluated.Device;
                            break;
                        case nameof(Trip):
                            var tripToBeEvaluated = entityToBeEvaluated as Trip;
                            entityToBeEvaluatedDevice = tripToBeEvaluated.Device;
                            break;
                        case nameof(StatusData):
                            var statusDataToBeEvaluated = entityToBeEvaluated as StatusData;
                            entityToBeEvaluatedDevice = statusDataToBeEvaluated.Device;
                            break;
                        default:
                            errorMessage = $"The entity type '{entityTypeName}' is not supported by the '{methodBase.ReflectedType.Name}' method.";
                            logger.Error(errorMessage);
                            throw new Exception(errorMessage);
                    }

                    if (entityToBeEvaluatedDevice != null && genericGeotabObjectFiltererBase.GeotabObjectsToFilterOn.ContainsKey(entityToBeEvaluatedDevice.Id))
                    {
                        filteredEntities.Add(entityToBeEvaluated);
                    }
                }
            }
            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return filteredEntities;
        }
    }
}
