using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using Geotab.Checkmate.ObjectModel.Exceptions;
using static MyGeotabAPIAdapter.Database.Common;
using Microsoft.CSharp.RuntimeBinder;
using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            if (dbDevice.Id != device.Id.ToString())
            {
                throw new ArgumentException($"Cannot compare Device '{device.Id.ToString()}' with DbDevice '{dbDevice.Id}' because the IDs do not match.");
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
            if (dbDiagnostic.Id != diagnostic.Id.ToString())
            {
                throw new ArgumentException($"Cannot compare Diagnostic '{diagnostic.Id.ToString()}' with DbDiagnostic '{dbDiagnostic.Id}' because the IDs do not match.");
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
            if (dbDVIRDefect.Id != dvirDefect.Id.ToString())
            {
                throw new ArgumentException($"Cannot compare DVIRDefect '{dvirDefect.Id.ToString()}' with DbDVIRDefect '{dbDVIRDefect.Id}' because the IDs do not match.");
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
            if (dbRuleObject.DbRule.Id != rule.Id.ToString())
            {
                throw new ArgumentException($"Cannot compare Rule '{rule.Id.ToString()}' with DbUser '{dbRuleObject.DbRule.Id}' because the IDs do not match.");
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
            if (dbUser.Id != user.Id.ToString())
            {
                throw new ArgumentException($"Cannot compare User '{user.Id.ToString()}' with DbUser '{dbUser.Id}' because the IDs do not match.");
            }

            DateTime dbUserActiveFromUtc = dbUser.ActiveFrom.GetValueOrDefault().ToUniversalTime();
            DateTime dbUserActiveToUtc = dbUser.ActiveTo.GetValueOrDefault().ToUniversalTime();
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
            if (dbUser.FirstName != user.FirstName || dbUser.IsDriver != user.IsDriver || dbUser.LastName != user.LastName || dbUser.Name != user.Name)
            {
                return true;
            }
            if (dbUser.EmployeeNo != user.EmployeeNo)
            {
                if (dbUser.EmployeeNo == null && user.EmployeeNo == "")
                {
                    return false;
                }
                else
                {
                    return true;
                }
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
            DbCondition dbCondition = new DbCondition
            {
                Id = condition.Id.ToString(),
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
            DbCondition dbCondition = new DbCondition
            {
                Id = condition.Id.ToString(),
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

            DbDevice dbDevice = new DbDevice
            {
                ActiveFrom = device.ActiveFrom,
                ActiveTo = device.ActiveTo,
                DeviceType = device.DeviceType.ToString(),
                Id = device.Id.ToString(),
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

            DbDiagnostic dbDiagnostic = new DbDiagnostic
            {
                DiagnosticCode = diagnostic.Code,
                DiagnosticName = diagnostic.Name,
                DiagnosticSourceId = diagnosticSource.Id.ToString(),
                DiagnosticSourceName = diagnosticSource.Name,
                DiagnosticUnitOfMeasureId = diagnosticUnitOfMeasure.Id.ToString(),
                DiagnosticUnitOfMeasureName = diagnosticUnitOfMeasure.Name,
                Id = diagnostic.Id.ToString()
            };
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
        /// Creates and returns a <see cref="DbDVIRDefect"/> using information from the supplied inputs.
        /// </summary>
        /// <param name="dvirLog">The <see cref="DVIRLog"/> from which to capture information.</param>
        /// <param name="dvirDefect">The <see cref="DVIRDefect"/> from which to capture information.</param>
        /// <param name="defect">The <see cref="Defect"/> from which to capture information.</param>
        /// <param name="defectListPartDefect">The <see cref="DefectListPartDefect"/> from which to capture information.</param>
        /// <returns></returns>
        public static DbDVIRDefect GetDbDVIRDefect(DVIRLog dvirLog, DVIRDefect dvirDefect, Defect defect, DefectListPartDefect defectListPartDefect)
        {
            DbDVIRDefect dbDVIRDefect = new DbDVIRDefect
            {
                Id = dvirDefect.Id.ToString(),
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
            DbDVIRDefectRemark dbDVIRDefectRemark = new DbDVIRDefectRemark
            {
                DVIRDefectId = defectRemark.DVIRDefect.Id.ToString(),
                Id = defectRemark.Id.ToString(),
                Remark = defectRemark.Remark,
                RemarkUserId = defectRemark.User.Id.ToString()
            };
            if (defectRemark.DateTime != null)
            {
                dbDVIRDefectRemark.DateTime = defectRemark.DateTime;
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
            DbDVIRLog dbDVIRLog = new DbDVIRLog
            {
                Id = dvirLog.Id.ToString()
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
            if (dvirLog.RepairRemark != null && dvirLog.RepairRemark.Length > 0)
            {
                dbDVIRLog.RepairRemark = dvirLog.RepairRemark;
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

            DbExceptionEvent dbExceptionEvent = new DbExceptionEvent
            {
                Id = exceptionEvent.Id.ToString(),
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

            DbFaultData dbFaultData = new DbFaultData
            {
                Id = faultData.Id.ToString(),
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
            DbLogRecord dbLogRecord = new DbLogRecord
            {
                Id = logRecord.Id.ToString(),
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
            DbRule dbRule = new DbRule
            {
                Id = rule.Id.ToString(),
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
            DbRule dbRule = new DbRule
            {
                Id = rule.Id.ToString(),
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

            DbStatusData dbStatusData = new DbStatusData
            {
                Id = statusData.Id.ToString(),
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
            DbTrip dbTrip = new DbTrip
            {
                DeviceId = trip.Device.Id.ToString(),
                Id = trip.Id.ToString(),
                DriverId = trip.Driver.Id.ToString(),
                Distance = trip.Distance,
                DrivingDuration = trip.DrivingDuration,
                NextTripStart = trip.NextTripStart,
                Start = trip.Start,
                Stop = trip.Stop,
                StopDuration = trip.StopDuration
            };
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

            DbUser dbUser = new DbUser
            {
                ActiveFrom = user.ActiveFrom,
                ActiveTo = user.ActiveTo,
                EmployeeNo = employeeNo,
                FirstName = user.FirstName,
                Id = user.Id.ToString(),
                IsDriver = user.IsDriver ?? false,
                LastName = user.LastName,
                Name = user.Name
            };
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
    }
}
