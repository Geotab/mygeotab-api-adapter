using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using Geotab.Checkmate.ObjectModel.Exceptions;
using static MyGeotabAPIAdapter.Database.Common;
using Microsoft.CSharp.RuntimeBinder;
using MyGeotabAPIAdapter.Database.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Maps between various <see cref="Geotab.Checkmate.ObjectModel.Entity"/> objects and <see cref="MyGeotabAPIAdapter.Database.Models"/> objects. 
    /// </summary>
    static class ObjectMapper
    {
        /// <summary>
        /// Indicates whether the <see cref="DbDevice"/> differs from the <see cref="Device"/>, thereby requiring the <see cref="DbDevice"/> to be updated in the database. 
        /// </summary>
        /// <param name="dbDevice">The <see cref="DbDevice"/> to be evaluated.</param>
        /// <param name="device">The <see cref="Device"/> to compare against.</param>
        /// <returns></returns>
        public static bool DbDeviceRequiresUpdate(DbDevice dbDevice, Device device)
        {
            if (dbDevice.GeotabId != device.Id.ToString())
            {
                throw new ArgumentException($"Cannot compare Device '{device.Id}' with DbDevice '{dbDevice.GeotabId}' because the IDs do not match.");
            }

            DateTime dbDeviceActiveFromUtc = dbDevice.ActiveFrom.GetValueOrDefault().ToUniversalTime();
            DateTime dbDeviceActiveToUtc = dbDevice.ActiveTo.GetValueOrDefault().ToUniversalTime();
            if (dbDevice.ActiveFrom != device.ActiveFrom && dbDeviceActiveFromUtc != device.ActiveFrom)
            {
                return true;
            }
            if (dbDevice.ActiveTo != device.ActiveTo && dbDeviceActiveToUtc != device.ActiveTo)
            {
                return true;
            }

            string deviceDeviceType = device.DeviceType.ToString();
            if (dbDevice.DeviceType != deviceDeviceType || dbDevice.Name != device.Name || dbDevice.SerialNumber != device.SerialNumber)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Indicates whether the <see cref="DbDiagnostic"/> differs from the <see cref="Diagnostic"/>, thereby requiring the <see cref="DbDiagnostic"/> to be updated in the database. 
        /// </summary>
        /// <param name="dbDiagnostic">The <see cref="DbDiagnostic"/> to be evaluated.</param>
        /// <param name="diagnostic">The <see cref="Diagnostic"/> to compare against.</param>
        /// <returns></returns>
        public static bool DbDiagnosticRequiresUpdate(DbDiagnostic dbDiagnostic, Diagnostic diagnostic)
        {
            if (dbDiagnostic.GeotabId != diagnostic.Id.ToString())
            {
                throw new ArgumentException($"Cannot compare Diagnostic '{diagnostic.Id}' with DbDiagnostic '{dbDiagnostic.GeotabId}' because the IDs do not match.");
            }

            Source diagnosticSource = diagnostic.Source;
            UnitOfMeasure diagnosticUnitOfMeasure = diagnostic.UnitOfMeasure;
            if (dbDiagnostic.DiagnosticCode != diagnostic.Code || dbDiagnostic.DiagnosticName != diagnostic.Name || dbDiagnostic.DiagnosticSourceId != diagnosticSource.Id.ToString() || dbDiagnostic.DiagnosticSourceName != diagnosticSource.Name || dbDiagnostic.DiagnosticUnitOfMeasureId != diagnosticUnitOfMeasure.Id.ToString() || dbDiagnostic.DiagnosticUnitOfMeasureName != diagnosticUnitOfMeasure.Name)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Indicates whether the <see cref="DbDVIRDefect"/> differs from the <see cref="DVIRDefect"/>, thereby requiring the <see cref="DbDVIRDefect"/> to be updated in the database. 
        /// </summary>
        /// <param name="dbDVIRDefect">The <see cref="DbDVIRDefect"/> to be evaluated.</param>
        /// <param name="dvirDefect">The <see cref="DVIRDefect"/> to compare against.</param>
        /// <returns></returns>
        public static bool DbDVIRDefectRequiresUpdate(DbDVIRDefect dbDVIRDefect, DVIRDefect dvirDefect)
        {
            if (dbDVIRDefect.GeotabId != dvirDefect.Id.ToString())
            {
                throw new ArgumentException($"Cannot compare DVIRDefect '{dvirDefect.Id}' with DbDVIRDefect '{dbDVIRDefect.GeotabId}' because the IDs do not match.");
            }

            DateTime dbDVIRDefectRepairDateTimeUtc = dbDVIRDefect.RepairDateTime.GetValueOrDefault().ToUniversalTime();
            if (dbDVIRDefect.RepairDateTime != dvirDefect.RepairDateTime && dbDVIRDefectRepairDateTimeUtc != dvirDefect.RepairDateTime)
            {
                return true;
            }
            User dvirDefectRepairUser = dvirDefect.RepairUser;
            if (dvirDefectRepairUser != null && dbDVIRDefect.RepairUserId != dvirDefectRepairUser.Id.ToString())
            {
                return true;
            }
            if (dvirDefect.RepairStatus != null && dbDVIRDefect.RepairStatus != dvirDefect.RepairStatus.ToString())
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Indicates whether the <see cref="DbRule"/> of the <see cref="DbRuleObject"/> differs from the <see cref="Rule"/>, 
        /// thereby requiring the <see cref="DbRule"/> and related <see cref="DbCondition"/> objects to be updated in the database. 
        /// </summary>
        /// <param name="dbRuleObject"></param>
        /// <param name="rule"></param>
        /// <returns>True if update is required otherwise false</returns>
        public static bool DbRuleObjectRequiresUpdate(DbRuleObject dbRuleObject, Rule rule)
        {
            if (dbRuleObject.DbRule.GeotabId != rule.Id.ToString())
            {
                throw new ArgumentException($"Cannot compare Rule '{rule.Id}' with DbUser '{dbRuleObject.DbRule.GeotabId}' because the IDs do not match.");
            }

            if (dbRuleObject.DbRule.Version != rule.Version)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Indicates whether the <see cref="DbUser"/> differs from the <see cref="User"/>, thereby requiring the <see cref="DbUser"/> to be updated in the database. 
        /// </summary>
        /// <param name="dbUser">The <see cref="DbUser"/> to be evaluated.</param>
        /// <param name="user">The <see cref="User"/> to compare against.</param>
        /// <returns></returns>
        public static bool DbUserRequiresUpdate(DbUser dbUser, User user)
        {
            if (dbUser.GeotabId != user.Id.ToString())
            {
                throw new ArgumentException($"Cannot compare User '{user.Id}' with DbUser '{dbUser.GeotabId}' because the IDs do not match.");
            }

            DateTime dbUserActiveFromUtc = dbUser.ActiveFrom.GetValueOrDefault().ToUniversalTime();
            DateTime dbUserActiveToUtc = dbUser.ActiveTo.GetValueOrDefault().ToUniversalTime();
            DateTime dbUserLastAccessDateUtc = dbUser.LastAccessDate.GetValueOrDefault().ToUniversalTime();
            if (dbUser.ActiveFrom != user.ActiveFrom && dbUserActiveFromUtc != user.ActiveFrom)
            {
                return true;
            }
            if (dbUser.ActiveTo != user.ActiveTo && dbUserActiveToUtc != user.ActiveTo)
            {
                return true;
            }
            if (user.IsDriver == null)
            {
                user.IsDriver = false;
            }
            if (dbUser.FirstName != user.FirstName|| dbUser.IsDriver != user.IsDriver || dbUser.LastName != user.LastName || dbUser.Name != user.Name)
            {
                return true;
            }
            if ((dbUser.EmployeeNo != user.EmployeeNo) && (dbUser.EmployeeNo != null && user.EmployeeNo != ""))
            {
                return true;
            }
            if ((user.HosRuleSet == null && dbUser.HosRuleSet != null) || (user.HosRuleSet != null && dbUser.HosRuleSet != user.HosRuleSet.Value.ToString()))
            {
                return true;
            }
            if ((dbUser.LastAccessDate != user.LastAccessDate) && dbUserLastAccessDateUtc != user.LastAccessDate)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Indicates whether the <see cref="DbZone"/> differs from the <see cref="Zone"/>, thereby requiring the <see cref="DbZone"/> to be updated in the database. 
        /// </summary>
        /// <param name="dbZone">The <see cref="DbZone"/> to be evaluated.</param>
        /// <param name="zone">The <see cref="Zone"/> to compare against.</param>
        /// <returns></returns>
        public static bool DbZoneRequiresUpdate(DbZone dbZone, Zone zone)
        {
            if (dbZone.GeotabId != zone.Id.ToString())
            {
                throw new ArgumentException($"Cannot compare Zone '{zone.Id}' with DbZone '{dbZone.GeotabId}' because the IDs do not match.");
            }

            DateTime dbZoneActiveFromUtc = dbZone.ActiveFrom.GetValueOrDefault().ToUniversalTime();
            DateTime dbZoneActiveToUtc = dbZone.ActiveTo.GetValueOrDefault().ToUniversalTime();
            if (dbZone.ActiveFrom != zone.ActiveFrom && dbZoneActiveFromUtc != zone.ActiveFrom)
            {
                return true;
            }
            if (dbZone.ActiveTo != zone.ActiveTo && dbZoneActiveToUtc != zone.ActiveTo)
            {
                return true;
            }
            if (zone.Displayed == null)
            {
                zone.Displayed = false;
            }
            if (zone.MustIdentifyStops == null)
            {
                zone.MustIdentifyStops = false;
            }
            if (dbZone.CentroidLatitude != zone.CentroidLatitude || dbZone.CentroidLongitude != zone.CentroidLongitude || dbZone.Displayed != zone.Displayed || dbZone.MustIdentifyStops != zone.MustIdentifyStops || dbZone.Version != zone.Version)
            {
                return true;
            }
            if ((dbZone.Comment != zone.Comment) && (dbZone.Comment != null && zone.Comment != ""))
            {
                return true;
            }
            if ((dbZone.ExternalReference != zone.ExternalReference) && (dbZone.ExternalReference != null && zone.ExternalReference != ""))
            {
                return true;
            }
            if ((dbZone.Name != zone.Name) && (dbZone.Name != null && zone.Name != ""))
            {
                return true;
            }
            string zonePoints = JsonConvert.SerializeObject(zone.Points);
            if (dbZone.Points != zonePoints)
            {
                return true;
            }
            string zoneZoneTypeIds = GetZoneTypeIdsJSON(zone.ZoneTypes);
            if (dbZone.ZoneTypeIds != zoneZoneTypeIds)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Indicates whether the <see cref="DbZoneType"/> differs from the <see cref="ZoneType"/>, thereby requiring the <see cref="DbZoneType"/> to be updated in the database. 
        /// </summary>
        /// <param name="dbZoneType">The <see cref="DbZoneType"/> to be evaluated.</param>
        /// <param name="zoneType">The <see cref="ZoneType"/> to compare against.</param>
        /// <returns></returns>
        public static bool DbZoneTypeRequiresUpdate(DbZoneType dbZoneType, ZoneType zoneType)
        {
            if (dbZoneType.GeotabId != zoneType.Id.ToString())
            {
                throw new ArgumentException($"Cannot compare ZoneType '{zoneType.Id}' with DbZoneType '{dbZoneType.GeotabId}' because the IDs do not match.");
            }

            if ((dbZoneType.Comment != zoneType.Comment) && (dbZoneType.Comment != null && zoneType.Comment != ""))
            {
                return true;
            }
            if ((dbZoneType.Name != zoneType.Name) && (dbZoneType.Name != null && zoneType.Name != ""))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Converts the supplied <see cref="Condition"/> into a <see cref="DbCondition"/>.
        /// </summary>
        /// <param name="condition">The <see cref="Condition"/> to be converted.</param>
        /// <param name="parentId">The <see cref="Condition.Id"/> of the parent condition (for child conditions).</param>
        /// <returns></returns>
        public static DbCondition GetDbCondition(Condition condition, string parentId)
        {
            DbCondition dbCondition = new()
            {
                GeotabId = condition.Id.ToString(),
                ParentId = parentId
            };
            if (condition.Rule != null)
            {
                dbCondition.RuleId = condition.Rule.Id.ToString();
            }
            if (condition.ConditionType != null)
            {
                dbCondition.ConditionType = condition.ConditionType.ToString();
            }
            if (condition.Device != null)
            {
                dbCondition.DeviceId = condition.Device.Id.ToString();
            }
            if (condition.Diagnostic != null)
            {
                dbCondition.DiagnosticId = condition.Diagnostic.Id.ToString();
            }
            if (condition.Driver != null)
            {
                dbCondition.DriverId = condition.Driver.Id.ToString();
            }
            dbCondition.Value = condition.Value;
            if (condition.WorkTime != null)
            {
                dbCondition.WorkTimeId = condition.WorkTime.Id.ToString();
            }
            if (condition.Zone != null)
            {
                dbCondition.ZoneId = condition.Zone.Id.ToString();
            }
            return dbCondition;
        }

        /// <summary>
        /// Converts the supplied <see cref="Condition"/> into a <see cref="DbCondition"/>.
        /// </summary>
        /// <param name="condition">The <see cref="Condition"/> to be converted.</param>
        /// <param name="entityStatus">The <see cref="Database.Common.DatabaseRecordStatus"/> to be applied to the <see cref="DbCondition"/>.</param>
        /// <param name="recordLastChanged">The timestamp to be applied to the <see cref="DbCondition"/>.</param>
        /// <param name="operationType">The <see cref="Database.Common.DatabaseWriteOperationType"/> to be applied to the <see cref="DbCondition"/>.</param>
        /// <returns></returns>
        public static DbCondition GetDbCondition(Condition condition, string parentId, int entityStatus,
            DateTime recordLastChanged, DatabaseWriteOperationType operationType)
        {
            DbCondition dbCondition = new()
            {
                GeotabId = condition.Id.ToString(),
                ParentId = parentId
            };
            if (condition.Rule != null)
            {
                dbCondition.RuleId = condition.Rule.Id.ToString();
            }
            if (condition.ConditionType != null)
            {
                dbCondition.ConditionType = condition.ConditionType.ToString();
            }
            if (condition.Device != null)
            {
                dbCondition.DeviceId = condition.Device.Id.ToString();
            }
            if (condition.Diagnostic != null)
            {
                dbCondition.DiagnosticId = condition.Diagnostic.Id.ToString();
            }
            if (condition.Driver != null)
            {
                dbCondition.DriverId = condition.Driver.Id.ToString();
            }
            dbCondition.Value = condition.Value;
            if (condition.WorkTime != null)
            {
                dbCondition.WorkTimeId = condition.WorkTime.Id.ToString();
            }
            if (condition.Zone != null)
            {
                dbCondition.ZoneId = condition.Zone.Id.ToString();
            }
            dbCondition.EntityStatus = entityStatus;
            dbCondition.RecordLastChangedUtc = recordLastChanged;
            dbCondition.DatabaseWriteOperationType = operationType;
            return dbCondition;
        }

        /// <summary>
        /// Converts the supplied list of <see cref="Condition"/> objects into a list of <see cref="DbCondition"/> objects.
        /// </summary>
        /// <param name="conditions">The list of <see cref="Condition"/> objects to be converted.</param>
        /// <returns></returns>
        public static List<DbCondition> GetDbConditions(IList<Condition> conditions)
        {
            var dbConditions = new List<DbCondition>();
            foreach (var condition in conditions)
            {
                DbCondition dbCondition = GetDbCondition(condition, "");
                dbConditions.Add(dbCondition);
            }
            return dbConditions;
        }

        /// <summary>
        /// Converts the supplied list of <see cref="Condition"/> objects into a list of <see cref="DbCondition"/> objects.
        /// </summary>
        /// <param name="conditions">The list of <see cref="Condition"/> objects to be converted.</param>
        /// <param name="entityStatus">The <see cref="Database.Common.DatabaseRecordStatus"/> to be applied to the <see cref="DbCondition"/> objects.</param>
        /// <param name="recordLastChanged">The timestamp to be applied to the <see cref="DbCondition"/> objects.</param>
        /// <param name="operationType">The <see cref="Database.Common.DatabaseWriteOperationType"/> to be applied to the <see cref="DbCondition"/> objects.</param>
        /// <returns></returns>
        public static List<DbCondition> GetDbConditions(IList<Condition> conditions, int entityStatus,
            DateTime recordLastChanged, DatabaseWriteOperationType operationType)
        {
            var dbConditions = new List<DbCondition>();
            foreach (var condition in conditions)
            {
                DbCondition dbCondition = GetDbCondition(condition, "", entityStatus, recordLastChanged, operationType);
                dbConditions.Add(dbCondition);
            }
            return dbConditions;
        }

        /// <summary>
        /// Converts the supplied <see cref="Device"/> into a <see cref="DbDevice"/>.
        /// </summary>
        /// <param name="device">The <see cref="Device"/> to be converted.</param>
        /// <returns></returns>
        public static DbDevice GetDbDevice(Device device)
        {
            string deviceLicensePlate = "";
            string deviceLicenseState = "";
            string deviceVIN = "";
            dynamic convertedDevice = Convert.ChangeType(device, device.GetType());

            try
            {
                deviceLicensePlate = convertedDevice.LicensePlate;
            }
            catch (RuntimeBinderException)
            {
                // Property does not exist for the subject Device type.
            }

            try
            {
                deviceLicenseState = convertedDevice.LicenseState;
            }
            catch (RuntimeBinderException)
            {
                // Property does not exist for the subject Device type.
            }

            try
            {
                deviceVIN = convertedDevice.VehicleIdentificationNumber;
            }
            catch (RuntimeBinderException)
            {
                // Property does not exist for the subject Device type.
            }

            if (deviceLicensePlate.Length == 0)
            {
                deviceLicensePlate = null;
            }
            if (deviceLicenseState.Length == 0)
            {
                deviceLicenseState = null;
            }
            if (deviceVIN.Length == 0)
            {
                deviceVIN = null;
            }

            DbDevice dbDevice = new()
            {
                ActiveFrom = device.ActiveFrom,
                ActiveTo = device.ActiveTo,
                DeviceType = device.DeviceType.ToString(),
                GeotabId = device.Id.ToString(),
                LicensePlate = deviceLicensePlate,
                LicenseState = deviceLicenseState,
                Name = device.Name,
                ProductId = device.ProductId,
                SerialNumber = device.SerialNumber,
                VIN = deviceVIN
            };
            return dbDevice;
        }

        /// <summary>
        /// Converts the supplied <see cref="Diagnostic"/> into a <see cref="DbDiagnostic"/>.
        /// </summary>
        /// <param name="diagnostic">The <see cref="Diagnostic"/> to be converted.</param>
        /// <returns></returns>
        public static DbDiagnostic GetDbDiagnostic(Diagnostic diagnostic)
        {
            Source diagnosticSource = diagnostic.Source;
            UnitOfMeasure diagnosticUnitOfMeasure = diagnostic.UnitOfMeasure;
            Controller diagnosticController = diagnostic.Controller;

            DbDiagnostic dbDiagnostic = new()
            {
                DiagnosticCode = diagnostic.Code,
                DiagnosticName = diagnostic.Name,
                DiagnosticSourceId = diagnosticSource.Id.ToString(),
                DiagnosticSourceName = diagnosticSource.Name,
                DiagnosticUnitOfMeasureId = diagnosticUnitOfMeasure.Id.ToString(),
                DiagnosticUnitOfMeasureName = diagnosticUnitOfMeasure.Name,
                GeotabId = diagnostic.Id.ToString()
            };
            if (diagnosticController != null)
            {
                dbDiagnostic.ControllerId = diagnosticController.Id.ToString();

                // Derive the OBD-II Diagnostic Trouble Code (DTC), if applicable.
                if (dbDiagnostic.DiagnosticSourceId == KnownId.SourceObdId.ToString() || dbDiagnostic.DiagnosticSourceId == KnownId.SourceObdSaId.ToString())
                {
                    int diagnosticCode;
                    if (diagnostic.Code != null)
                    {
                        diagnosticCode = (int)diagnostic.Code;
                        string dtcPrefix = "";
                        switch (diagnosticController.Id.ToString())
                        {
                            case nameof(KnownId.ControllerObdPowertrainId):
                                dtcPrefix = Globals.OBD2DTCPrefixPowertrain;
                                break;
                            case nameof(KnownId.ControllerObdWwhPowertrainId):
                                dtcPrefix = Globals.OBD2DTCPrefixPowertrain;
                                break;
                            case nameof(KnownId.ControllerObdBodyId):
                                dtcPrefix = Globals.OBD2DTCPrefixBody;
                                break;
                            case nameof(KnownId.ControllerObdWwhBodyId):
                                dtcPrefix = Globals.OBD2DTCPrefixBody;
                                break;
                            case nameof(KnownId.ControllerObdChassisId):
                                dtcPrefix = Globals.OBD2DTCPrefixChassis;
                                break;
                            case nameof(KnownId.ControllerObdWwhChassisId):
                                dtcPrefix = Globals.OBD2DTCPrefixChassis;
                                break;
                            case nameof(KnownId.ControllerObdNetworkingId):
                                dtcPrefix = Globals.OBD2DTCPrefixNetworking;
                                break;
                            case nameof(KnownId.ControllerObdWwhNetworkingId):
                                dtcPrefix = Globals.OBD2DTCPrefixNetworking;
                                break;
                            default:
                                break;
                        }
                        if (dtcPrefix.Length > 0)
                        {
                            string dtc = Convert.ToString(diagnosticCode, 16).PadLeft(4, '0');
                            dbDiagnostic.OBD2DTC = $"{dtcPrefix}{dtc.ToUpper()}";
                        }
                    }
                }
            }
            return dbDiagnostic;
        }

        /// <summary>
        /// Converts the supplied list of <see cref="Device"/> objects into a list of <see cref="DbDevice"/> objects.
        /// </summary>
        /// <param name="devices">The list of <see cref="Device"/> objects to be converted.</param>
        /// <returns></returns>
        public static List<DbDevice> GetDbDevices(List<Device> devices)
        {
            var dbDevices = new List<DbDevice>();
            foreach (var device in devices)
            {
                DbDevice dbDevice = GetDbDevice(device);
                dbDevices.Add(dbDevice);
            }
            return dbDevices;
        }

        /// <summary>
        /// Converts the supplied <see cref="DriverChange"/> into a <see cref="DbDriverChange"/>.
        /// </summary>
        /// <param name="">The <see cref="DriverChange"/> to be converted.</param>
        /// <returns></returns>
        public static DbDriverChange GetDbDriverChange(DriverChange driverChange)
        {
            Device driverChangeDevice = driverChange.Device;
            Driver driverChangeDriver = driverChange.Driver;

            DbDriverChange dbDriverChange = new()
            {
                GeotabId = driverChange.Id.ToString(),
                DateTime = driverChange.DateTime,
                DeviceId = driverChangeDevice.Id.ToString(),
                DriverId = driverChangeDriver.Id.ToString()
            };
            if (driverChange.Type != null)
            {
                dbDriverChange.Type = Enum.GetName(typeof(DriverChangeType), driverChange.Type);
            }
            if (driverChange.Version != null)
            {
                dbDriverChange.Version = (long)driverChange.Version;
            }
            return dbDriverChange;
        }

        /// <summary>
        /// Converts the supplied list of <see cref="DriverChange"/> objects into a list of <see cref="DbDriverChange"/> objects.
        /// </summary>
        /// <param name="driverChanges">The list of <see cref="DriverChange"/> objects to be converted.</param>
        /// <returns></returns>
        public static List<DbDriverChange> GetDbDriverChanges(List<DriverChange> driverChanges)
        {
            DateTime recordCreationTimeUtc = DateTime.UtcNow;
            var dbDriverChanges = new List<DbDriverChange>();
            foreach (var driverChange in driverChanges)
            {
                DbDriverChange dbDriverChange = GetDbDriverChange(driverChange);
                dbDriverChange.RecordCreationTimeUtc = recordCreationTimeUtc;
                dbDriverChanges.Add(dbDriverChange);
            }
            return dbDriverChanges;
        }

        /// <summary>
        /// Converts the supplied <see cref="DutyStatusAvailability"/> object into a <see cref="DbDutyStatusAvailability"/> object.
        /// </summary>
        /// <param name="dutyStatusAvailability">The <see cref="DutyStatusAvailability"/> object to be converted.</param>
        /// <returns></returns>
        public static DbDutyStatusAvailability GetDbDutyStatusAvailability(DutyStatusAvailability dutyStatusAvailability)
        {
            DbDutyStatusAvailability dbDutyStatusAvailability = new()
            {
                Cycle = dutyStatusAvailability.Cycle,
                CycleRest = dutyStatusAvailability.CycleRest,
                DriverId = dutyStatusAvailability.Driver.Id.ToString(),
                Driving = dutyStatusAvailability.Driving,
                Duty = dutyStatusAvailability.Duty,
                DutySinceCycleRest = dutyStatusAvailability.DutySinceCycleRest,
                Is16HourExemptionAvailable = dutyStatusAvailability.Is16HourExemptionAvailable,
                IsAdverseDrivingExemptionAvailable = dutyStatusAvailability.IsAdverseDrivingExemptionAvailable,
                IsOffDutyDeferralExemptionAvailable = dutyStatusAvailability.IsOffDutyDeferralExemptionAvailable,
                Rest = dutyStatusAvailability.Rest,
                Workday = dutyStatusAvailability.Workday
            };

            string cycleAvailabilities = JsonConvert.SerializeObject(dutyStatusAvailability.CycleAvailabilities);
            dbDutyStatusAvailability.CycleAvailabilities = cycleAvailabilities;

            string recap = JsonConvert.SerializeObject(dutyStatusAvailability.Recap);
            dbDutyStatusAvailability.Recap = recap;

            return dbDutyStatusAvailability;
        }

        /// <summary>
        /// Creates and returns a <see cref="DbDVIRDefect"/> using information from the supplied inputs.
        /// </summary>
        /// <param name="dvirLog">The <see cref="DVIRLog"/> from which to capture information.</param>
        /// <param name="dvirDefect">The <see cref="DVIRDefect"/> from which to capture information.</param>
        /// <param name="defect">The <see cref="Defect"/> from which to capture information.</param>
        /// <param name="defectListPartDefect">The <see cref="DefectListPartDefect"/> from which to capture information.</param>
        /// <returns></returns>
        public static DbDVIRDefect GetDbDVIRDefect(DVIRLog dvirLog, DVIRDefect dvirDefect, Defect defect, DefectListPartDefect defectListPartDefect)
        {
            DbDVIRDefect dbDVIRDefect = new()
            {
                GeotabId = dvirDefect.Id.ToString(),
                DVIRLogId = dvirLog.Id.ToString(),
                DefectId = defect.Id.ToString(),
                DefectListAssetType = defectListPartDefect.DefectListAssetType,
                DefectListId = defectListPartDefect.DefectListID,
                DefectListName = defectListPartDefect.DefectListName,
                PartId = defectListPartDefect.PartID,
                PartName = defectListPartDefect.PartName,
                DefectName = defectListPartDefect.DefectName,
                DefectSeverity = defectListPartDefect.DefectSeverity
            };
            if (dvirDefect.RepairDateTime != null)
            {
                dbDVIRDefect.RepairDateTime = dvirDefect.RepairDateTime;
            }
            if (dvirDefect.RepairStatus != null)
            {
                dbDVIRDefect.RepairStatus = dvirDefect.RepairStatus.ToString();
            }
            User repairUser = dvirDefect.RepairUser;
            if (repairUser != null)
            {
                dbDVIRDefect.RepairUserId = repairUser.Id.ToString();
            }

            return dbDVIRDefect;
        }

        /// <summary>
        /// Converts the supplied <see cref="DefectRemark"/> into a <see cref="DbDVIRDefectRemark"/>.
        /// </summary>
        /// <param name="">The <see cref="DefectRemark"/> to be converted.</param>
        /// <returns></returns>
        public static DbDVIRDefectRemark GetDbDVIRDefectRemark(DefectRemark defectRemark)
        {
            DbDVIRDefectRemark dbDVIRDefectRemark = new()
            {
                DVIRDefectId = defectRemark.DVIRDefect.Id.ToString(),
                GeotabId = defectRemark.Id.ToString(),
                Remark = defectRemark.Remark
            };
            if (defectRemark.DateTime != null)
            {
                dbDVIRDefectRemark.DateTime = defectRemark.DateTime;
            }
            if (defectRemark.User != null)
            {
                dbDVIRDefectRemark.RemarkUserId = defectRemark.User.Id.ToString();
            }

            return dbDVIRDefectRemark;
        }

        /// <summary>
        /// Converts the supplied <see cref="DVIRLog"/> into a <see cref="DbDVIRLog"/>.
        /// </summary>
        /// <param name="">The <see cref="DVIRLog"/> to be converted.</param>
        /// <returns></returns>
        public static DbDVIRLog GetDbDVIRLog(DVIRLog dvirLog)
        {
            DbDVIRLog dbDVIRLog = new()
            {
                GeotabId = dvirLog.Id.ToString()
            };

            if (dvirLog.CertifiedBy != null)
            {
                dbDVIRLog.CertifiedByUserId = dvirLog.CertifiedBy.Id.ToString();
            }
            dbDVIRLog.CertifiedDate = dvirLog.CertifyDate.HasValue ? dvirLog.CertifyDate : null;
            if (dvirLog.CertifyRemark != null && dvirLog.CertifyRemark.Length > 0)
            {
                dbDVIRLog.CertifyRemark = dvirLog.CertifyRemark;
            }
            if (dvirLog.DateTime != null)
            {
                dbDVIRLog.DateTime = dvirLog.DateTime;
            }
            if (dvirLog.Device != null)
            {
                dbDVIRLog.DeviceId = dvirLog.Device.Id.ToString();
            }
            if (dvirLog.Driver != null)
            {
                dbDVIRLog.DriverId = dvirLog.Driver.Id.ToString();
            }
            if (dvirLog.DriverRemark != null && dvirLog.DriverRemark.Length > 0)
            {
                dbDVIRLog.DriverRemark = dvirLog.DriverRemark;
            }
            if (dvirLog.IsSafeToOperate != null)
            {
                dbDVIRLog.IsSafeToOperate = dvirLog.IsSafeToOperate;
            }
            if (dvirLog.Location != null)
            {
                dbDVIRLog.LocationLongitude = dvirLog.Location.Location.X;
                dbDVIRLog.LocationLatitude = dvirLog.Location.Location.Y;
            }
            if (dvirLog.LogType != null)
            {
                dbDVIRLog.LogType = dvirLog.LogType.ToString();
            }
            if (dvirLog.RepairDate != null)
            {
                dbDVIRLog.RepairDate = dvirLog.RepairDate;
            }
            if (dvirLog.RepairedBy != null)
            {
                dbDVIRLog.RepairedByUserId = dvirLog.RepairedBy.Id.ToString();
            }
            if (dvirLog.Trailer != null)
            {
                dbDVIRLog.TrailerId = dvirLog.Trailer.Id.ToString();
                dbDVIRLog.TrailerName = dvirLog.Trailer.Name;
            }
            if (dvirLog.Version != null)
            {
                dbDVIRLog.Version = dvirLog.Version;
            }

            return dbDVIRLog;
        }

        /// <summary>
        /// Converts the supplied <see cref="ExceptionEvent"/> into a <see cref="DbExceptionEvent"/>.
        /// </summary>
        /// <param name="">The <see cref="ExceptionEvent"/> to be converted.</param>
        /// <returns></returns>
        public static DbExceptionEvent GetDbExceptionEvent(ExceptionEvent exceptionEvent)
        {
            Device device = exceptionEvent.Device;
            Driver driver = exceptionEvent.Driver;
            Rule rule = exceptionEvent.Rule;

            DbExceptionEvent dbExceptionEvent = new()
            {
                GeotabId = exceptionEvent.Id.ToString(),
                ActiveFrom = exceptionEvent.ActiveFrom,
                ActiveTo = exceptionEvent.ActiveTo,
                DeviceId = device.Id.ToString(),
                Distance = exceptionEvent.Distance,
                DriverId = driver.Id.ToString(),
                Duration = exceptionEvent.Duration,
                RuleId = rule.Id.ToString(),
                Version = exceptionEvent.Version
            };
            return dbExceptionEvent;
        }

        /// <summary>
        /// Converts the supplied list of <see cref="ExceptionEvent"/> objects into a list of <see cref="DbExceptionEvent"/> objects.
        /// </summary>
        /// <param name="conditions">The list of <see cref="ExceptionEvent"/> objects to be converted.</param>
        /// <returns></returns>
        public static List<DbExceptionEvent> GetDbExceptionEvents(IList<ExceptionEvent> exceptionEvents)
        {
            DateTime recordCreationTimeUtc = DateTime.UtcNow;
            var dbExceptionEvents = new List<DbExceptionEvent>();
            foreach (var exceptionEvent in exceptionEvents)
            {
                DbExceptionEvent dbExceptionEvent = GetDbExceptionEvent(exceptionEvent);
                dbExceptionEvent.RecordCreationTimeUtc = recordCreationTimeUtc;
                dbExceptionEvents.Add(dbExceptionEvent);
            }
            return dbExceptionEvents;
        }

        /// <summary>
        /// Converts the supplied <see cref="DbDVIRDefectUpdate"/> into a <see cref="DbFailedDVIRDefectUpdate"/>.
        /// </summary>
        /// <param name="dbDVIRDefectUpdate">The <see cref="DbDVIRDefectUpdate"/> to be converted.</param>
        /// <param name="failureMessage">A message indicating the reason why the DVIRDefect update failed.</param>
        /// <returns></returns>
        public static DbFailedDVIRDefectUpdate GetDbFailedDVIRDefectUpdate(DbDVIRDefectUpdate dbDVIRDefectUpdate, string failureMessage)
        {
            DbFailedDVIRDefectUpdate dbFailedDVIRDefectUpdate = new()
            {
                DVIRDefectId = dbDVIRDefectUpdate.DVIRDefectId,
                DVIRDefectUpdateId = dbDVIRDefectUpdate.id,
                DVIRLogId = dbDVIRDefectUpdate.DVIRLogId,
                FailureMessage = failureMessage,
                RemarkDateTime = dbDVIRDefectUpdate.RemarkDateTime,
                Remark = dbDVIRDefectUpdate.Remark,
                RemarkUserId = dbDVIRDefectUpdate.RemarkUserId,
                RepairDateTime = dbDVIRDefectUpdate.RepairDateTime,
                RepairStatus = dbDVIRDefectUpdate.RepairStatus,
                RepairUserId = dbDVIRDefectUpdate.RepairUserId
            };
            return dbFailedDVIRDefectUpdate;
        }

        /// <summary>
        /// Converts the supplied <see cref="FaultData"/> into a <see cref="DbFaultData"/>.
        /// </summary>
        /// <param name="">The <see cref="FaultData"/> to be converted.</param>
        /// <returns></returns>
        public static DbFaultData GetDbFaultData(FaultData faultData)
        {
            Controller faultDataController = faultData.Controller;
            Device faultDataDevice = faultData.Device;
            Diagnostic faultDataDiagnostic = null;
            if (faultData.Diagnostic != null)
            {
                faultDataDiagnostic = faultData.Diagnostic;
            }
            User faultDataDismissUser = faultData.DismissUser;
            FailureMode faultDataFailureMode = faultData.FailureMode;
            var faultDataFaultState = faultData.FaultState;

            DbFaultData dbFaultData = new()
            {
                GeotabId = faultData.Id.ToString(),
                AmberWarningLamp = faultData.AmberWarningLamp,
                ControllerId = faultDataController.Id.ToString(),
                ControllerName = faultDataController.Name,
                Count = faultData.Count,
                DateTime = faultData.DateTime,
                DeviceId = faultDataDevice.Id.ToString(),
                DiagnosticId = "",
                DismissDateTime = faultData.DismissDateTime,
                DismissUserId = faultDataDismissUser.Id.ToString(),
                FailureModeId = faultDataFailureMode.Id.ToString(),
                FailureModeName = faultDataFailureMode.Name,
                FaultLampState = faultData.FaultLampState.HasValue ? faultData.FaultLampState.ToString() : null,
                MalfunctionLamp = faultData.MalfunctionLamp,
                ProtectWarningLamp = faultData.ProtectWarningLamp,
                RedStopLamp = faultData.RedStopLamp,
                Severity = faultData.Severity.HasValue ? faultData.Severity.ToString() : null,
                SourceAddress = faultData.SourceAddress
            };

            if (faultDataDiagnostic != null)
            {
                dbFaultData.DiagnosticId = faultDataDiagnostic.Id.ToString();
            }
            if (faultData.ClassCode != null)
            {
                dbFaultData.ClassCode = faultData.ClassCode.ToString();
            }
            if (faultDataFailureMode.Code != null)
            {
                dbFaultData.FailureModeCode = faultDataFailureMode.Code;
            }
            if (faultDataFaultState != null)
            {
                dbFaultData.FaultState = faultDataFaultState.ToString();
            }

            return dbFaultData;
        }

        /// <summary>
        /// Converts the supplied list of <see cref="FaultData"/> objects into a list of <see cref="DbFaultData"/> objects.
        /// </summary>
        /// <param name="faultDatas">The list of <see cref="FaultData"/> objects to be converted.</param>
        /// <returns></returns>
        public static List<DbFaultData> GetDbFaultDatas(List<FaultData> faultDatas)
        {
            DateTime recordCreationTimeUtc = DateTime.UtcNow;
            var dbFaultDatas = new List<DbFaultData>();
            foreach (var faultData in faultDatas)
            {
                DbFaultData dbFaultData = GetDbFaultData(faultData);
                dbFaultData.RecordCreationTimeUtc = recordCreationTimeUtc;
                dbFaultDatas.Add(dbFaultData);
            }
            return dbFaultDatas;
        }

        /// <summary>
        /// Converts the supplied <see cref="LogRecord"/> into a <see cref="DbLogRecord"/>.
        /// </summary>
        /// <param name="">The <see cref="LogRecord"/> to be converted.</param>
        /// <returns></returns>
        public static DbLogRecord GetDbLogRecord(LogRecord logRecord)
        {
            DbLogRecord dbLogRecord = new()
            {
                GeotabId = logRecord.Id.ToString(),
                DateTime = logRecord.DateTime.GetValueOrDefault(),
                DeviceId = logRecord.Device.Id.ToString(),
                Latitude = logRecord.Latitude,
                Longitude = logRecord.Longitude,
                Speed = logRecord.Speed
            };
            return dbLogRecord;
        }

        /// <summary>
        /// Converts the supplied list of <see cref="LogRecord"/> objects into a list of <see cref="DbLogRecord"/> objects.
        /// </summary>
        /// <param name="logRecords">The list of <see cref="LogRecord"/> objects to be converted.</param>
        /// <returns></returns>
        public static List<DbLogRecord> GetDbLogRecords(List<LogRecord> logRecords)
        {
            DateTime recordCreationTimeUtc = DateTime.UtcNow;
            var dbLogRecords = new List<DbLogRecord>();
            foreach (var logRecord in logRecords)
            {
                DbLogRecord dbLogRecord = GetDbLogRecord(logRecord);
                dbLogRecord.RecordCreationTimeUtc = recordCreationTimeUtc;
                dbLogRecords.Add(dbLogRecord);
            }
            return dbLogRecords;
        }

        /// <summary>
        /// Converts the supplied <see cref="Rule"/> into a <see cref="DbRule"/>.
        /// </summary>
        /// <param name="rule">The <see cref="Rule"/> to be converted.</param>
        /// <returns></returns>
        public static DbRule GetDbRule(Rule rule)
        {
            DbRule dbRule = new()
            {
                GeotabId = rule.Id.ToString(),
                Name = rule.Name.ToString(),
                BaseType = rule.BaseType.ToString(),
                ActiveFrom = rule.ActiveFrom,
                ActiveTo = rule.ActiveTo,
                Comment = rule.Comment,
                Version = rule.Version
            };
            return dbRule;
        }

        /// <summary>
        /// Converts the supplied <see cref="Rule"/> into a <see cref="DbRule"/>.
        /// </summary>
        /// <param name="rule">The <see cref="Rule"/> to be converted.</param>
        /// <param name="entityStatus">The <see cref="Database.Common.DatabaseRecordStatus"/> to be applied to the <see cref="DbRule"/>.</param>
        /// <param name="recordLastChanged">The timestamp to be applied to the <see cref="DbRule"/>.</param>
        /// <param name="operationType">The <see cref="Database.Common.DatabaseWriteOperationType"/> to be applied to the <see cref="DbRule"/>.</param>
        /// <returns></returns>
        public static DbRule GetDbRule(Rule rule, int entityStatus,
            DateTime recordLastChanged, DatabaseWriteOperationType operationType)
        {
            DbRule dbRule = new()
            {
                GeotabId = rule.Id.ToString(),
                Name = rule.Name.ToString(),
                BaseType = rule.BaseType.ToString(),
                ActiveFrom = rule.ActiveFrom,
                ActiveTo = rule.ActiveTo,
                Comment = rule.Comment,
                Version = rule.Version,
                EntityStatus = entityStatus,
                RecordLastChangedUtc = recordLastChanged,
                DatabaseWriteOperationType = operationType
            };
            return dbRule;
        }

        /// <summary>
        /// Converts the supplied list of <see cref="Rule"/> objects into a list of <see cref="DbRule"/> objects.
        /// </summary>
        /// <param name="rules">The list of <see cref="Rule"/> objects to be converted.</param>
        /// <returns></returns>
        public static List<DbRule> GetDbRules(IList<Rule> rules)
        {
            var dbRules = new List<DbRule>();
            foreach (var rule in rules)
            {
                DbRule dbRule = GetDbRule(rule);
                dbRules.Add(dbRule);
            }
            return dbRules;
        }

        /// <summary>
        /// Converts the supplied list of <see cref="Rule"/> objects into a list of <see cref="DbRule"/> objects.
        /// </summary>
        /// <param name="rules">The list of <see cref="Rule"/> objects to be converted.</param>
        /// <param name="entityStatus">The <see cref="Database.Common.DatabaseRecordStatus"/> to be applied to the <see cref="DbRule"/> objects.</param>
        /// <param name="recordLastChanged">The timestamp to be applied to the <see cref="DbRule"/> objects.</param>
        /// <param name="operationType">The <see cref="Database.Common.DatabaseWriteOperationType"/> to be applied to the <see cref="DbRule"/> objects.</param>
        /// <returns></returns>
        public static List<DbRule> GetDbRules(IList<Rule> rules, int entityStatus,
            DateTime recordLastChanged, DatabaseWriteOperationType operationType)
        {
            var dbRules = new List<DbRule>();
            foreach (var rule in rules)
            {
                DbRule dbRule = GetDbRule(rule, entityStatus, recordLastChanged, operationType);
                dbRules.Add(dbRule);
            }
            return dbRules;
        }

        /// <summary>
        /// Converts the supplied <see cref="StatusData"/> into a <see cref="DbStatusData"/>.
        /// </summary>
        /// <param name="">The <see cref="StatusData"/> to be converted.</param>
        /// <returns></returns>
        public static DbStatusData GetDbStatusData(StatusData statusData)
        {
            Device statusDataDevice = statusData.Device;
            Diagnostic statusDataDiagnostic = statusData.Diagnostic;

            DbStatusData dbStatusData = new()
            {
                GeotabId = statusData.Id.ToString(),
                Data = statusData.Data,
                DateTime = statusData.DateTime,
                DeviceId = statusDataDevice.Id.ToString(),
                DiagnosticId = statusDataDiagnostic.Id.ToString(),
            };
            return dbStatusData;
        }

        /// <summary>
        /// Converts the supplied list of <see cref="StatusData"/> objects into a list of <see cref="DbStatusData"/> objects.
        /// </summary>
        /// <param name="statusDatas">The list of <see cref="StatusData"/> objects to be converted.</param>
        /// <returns></returns>
        public static List<DbStatusData> GetDbStatusDatas(List<StatusData> statusDatas)
        {
            DateTime recordCreationTimeUtc = DateTime.UtcNow;
            var dbStatusDatas = new List<DbStatusData>();
            foreach (var statusData in statusDatas)
            {
                DbStatusData dbStatusData = GetDbStatusData(statusData);
                dbStatusData.RecordCreationTimeUtc = recordCreationTimeUtc;
                dbStatusDatas.Add(dbStatusData);
            }
            return dbStatusDatas;
        }

        /// <summary>
        /// Converts the supplied <see cref="Trip"/> into a <see cref="DbTrip"/>.
        /// </summary>
        /// <param name="trip">The <see cref="Trip"/> to be converted.</param>
        /// <returns></returns>
        public static DbTrip GetDbTrip(Trip trip)
        {
            DbTrip dbTrip = new()
            {
                AfterHoursDistance = trip.AfterHoursDistance,
                AfterHoursDrivingDuration = trip.AfterHoursDrivingDuration,
                AfterHoursStopDuration = trip.AfterHoursStopDuration,
                AverageSpeed = trip.AverageSpeed,
                DeviceId = trip.Device.Id.ToString(),
                Distance = trip.Distance,
                DriverId = trip.Driver.Id.ToString(),
                DrivingDuration = trip.DrivingDuration,
                GeotabId = trip.Id.ToString(),
                IdlingDuration = trip.IdlingDuration,
                MaximumSpeed = trip.MaximumSpeed,
                NextTripStart = trip.NextTripStart,
                SpeedRange1 = trip.SpeedRange1,
                SpeedRange1Duration = trip.SpeedRange1Duration,
                SpeedRange2 = trip.SpeedRange2,
                SpeedRange2Duration = trip.SpeedRange2Duration,
                SpeedRange3 = trip.SpeedRange3,
                SpeedRange3Duration = trip.SpeedRange3Duration,
                Start = trip.Start,
                Stop = trip.Stop,
                StopDuration = trip.StopDuration,
                WorkDistance = trip.WorkDistance,
                WorkDrivingDuration = trip.WorkDrivingDuration,
                WorkStopDuration = trip.WorkStopDuration
            };
            if (trip.AfterHoursEnd != null)
            {
                dbTrip.AfterHoursEnd = trip.AfterHoursEnd;
            }
            if (trip.AfterHoursStart != null)
            {
                dbTrip.AfterHoursStart = trip.AfterHoursStart;
            }
            if (trip.StopPoint != null)
            {
                dbTrip.StopPointX = trip.StopPoint.X;
                dbTrip.StopPointY = trip.StopPoint.Y;
            }
            return dbTrip;
        }

        /// <summary>
        /// Converts the supplied list of <see cref="Trip"/> objects into a list of <see cref="DbTrip"/> objects.
        /// </summary>
        /// <param name="trips">The list of <see cref="Trip"/> objects to be converted.</param>
        /// <returns></returns>
        public static List<DbTrip> GetDbTrips(List<Trip> trips)
        {
            DateTime recordCreationTimeUtc = DateTime.UtcNow;
            var dbTrips = new List<DbTrip>();
            foreach (var trip in trips)
            {
                DbTrip dbTrip = GetDbTrip(trip);
                dbTrip.RecordCreationTimeUtc = recordCreationTimeUtc;
                dbTrips.Add(dbTrip);
            }
            return dbTrips;
        }

        /// <summary>
        /// Converts the supplied <see cref="User"/> into a <see cref="DbUser"/>.
        /// </summary>
        /// <param name="user">The <see cref="User"/> to be converted.</param>
        /// <returns></returns>
        public static DbUser GetDbUser(User user)
        {
            string employeeNo = user.EmployeeNo;
            if (employeeNo != null && employeeNo.Length == 0)
            {
                employeeNo = null;
            }

            DbUser dbUser = new()
            {
                ActiveFrom = user.ActiveFrom,
                ActiveTo = user.ActiveTo,
                EmployeeNo = employeeNo,
                FirstName = user.FirstName,
                GeotabId = user.Id.ToString(),
                IsDriver = user.IsDriver ?? false,
                LastAccessDate = user.LastAccessDate,
                LastName = user.LastName,
                Name = user.Name
            };
            if (user.HosRuleSet != null)
            {
                dbUser.HosRuleSet = user.HosRuleSet.Value.ToString();
            }
            return dbUser;
        }

        /// <summary>
        /// Converts the supplied list of <see cref="User"/> objects into a list of <see cref="DbUser"/> objects.
        /// </summary>
        /// <param name="users">The list of <see cref="User"/> objects to be converted.</param>
        /// <returns></returns>
        public static List<DbUser> GetDbUsers(List<User> users)
        {
            var dbUsers = new List<DbUser>();
            foreach (var user in users)
            {
                DbUser dbUser = GetDbUser(user);
                dbUsers.Add(dbUser);
            }
            return dbUsers;
        }

        /// <summary>
        /// Converts the supplied <see cref="Zone"/> into a <see cref="DbZone"/>.
        /// </summary>
        /// <param name="zone">The <see cref="Zone"/> to be converted.</param>
        /// <returns></returns>
        public static DbZone GetDbZone(Zone zone)
        {
            DbZone dbZone = new()
            {
                GeotabId = zone.Id.ToString(),
                Displayed = zone.Displayed ?? false,
                MustIdentifyStops = zone.MustIdentifyStops ?? false,
                Name = zone.Name,
                Version = zone.Version ?? null
            };
            if (zone.ActiveFrom != null)
            {
                dbZone.ActiveFrom = zone.ActiveFrom;
            }
            if (zone.ActiveTo != null)
            {
                dbZone.ActiveTo = zone.ActiveTo;
            }
            if (zone.CentroidLatitude != null)
            {
                dbZone.CentroidLatitude = zone.CentroidLatitude;
            }
            if (zone.CentroidLongitude != null)
            {
                dbZone.CentroidLongitude = zone.CentroidLongitude;
            }
            if (zone.Comment != null && zone.Comment.Length > 0)
            {
                dbZone.Comment = zone.Comment;
            }
            if (zone.ExternalReference != null && zone.ExternalReference.Length > 0)
            {
                dbZone.ExternalReference = zone.ExternalReference;
            }
            string zonePoints = JsonConvert.SerializeObject(zone.Points);
            dbZone.Points = zonePoints;
            dbZone.ZoneTypeIds = GetZoneTypeIdsJSON(zone.ZoneTypes);
            return dbZone;
        }

        /// <summary>
        /// Converts the supplied <see cref="ZoneType"/> into a <see cref="DbZoneType"/>.
        /// </summary>
        /// <param name="zoneType">The <see cref="ZoneType"/> to be converted.</param>
        /// <returns></returns>
        public static DbZoneType GetDbZoneType(ZoneType zoneType)
        {
            DbZoneType dbZoneType = new()
            {
                GeotabId = zoneType.Id.ToString(),
                Name = zoneType.Name,
            };
            if (zoneType.Comment != null && zoneType.Comment.Length > 0)
            {
                dbZoneType.Comment = zoneType.Comment;
            }
            return dbZoneType;
        }

        /// <summary>
        /// Converts the supplied list of <see cref="Zone"/> objects into a list of <see cref="DbZone"/> objects.
        /// </summary>
        /// <param name="zones">The list of <see cref="Zone"/> objects to be converted.</param>
        /// <returns></returns>
        public static List<DbZone> GetDbZones(List<Zone> zones)
        {
            var dbZones = new List<DbZone>();
            foreach (var zone in zones)
            {
                DbZone dbZone = GetDbZone(zone);
                dbZones.Add(dbZone);
            }
            return dbZones;
        }

        /// <summary>
        /// Builds a JSON array containing the Ids of the <paramref name="zoneTypes"/>.
        /// </summary>
        /// <param name="zoneTypes">The list of <see cref="ZoneType"/> objects whose Ids are to be included in the output JSON array.</param>
        /// <returns></returns>
        public static string GetZoneTypeIdsJSON(IList<ZoneType> zoneTypes)
        {
            bool zoneTypeIdsArrayHasItems = false;
            var zoneTypeIds = new StringBuilder();
            zoneTypeIds.Append('[');
            foreach (var zoneType in zoneTypes)
            {
                if (zoneTypeIdsArrayHasItems == true)
                {
                    zoneTypeIds.Append(',');
                }
                zoneTypeIds.Append($"{{\"Id\":\"{zoneType.Id}\"}}");
                zoneTypeIdsArrayHasItems = true;
            }
            zoneTypeIds.Append(']');
            return zoneTypeIds.ToString();
        }
    }
}
