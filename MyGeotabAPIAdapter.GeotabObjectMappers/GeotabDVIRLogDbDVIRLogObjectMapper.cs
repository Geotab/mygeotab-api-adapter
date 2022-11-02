using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="DVIRLog"/> and <see cref="DbDVIRLog"/> entities.
    /// </summary>
    public class GeotabDVIRLogDbDVIRLogObjectMapper : IGeotabDVIRLogDbDVIRLogObjectMapper
    {
        /// <inheritdoc/>
        public DbDVIRLog CreateEntity(DVIRLog dvirLog, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active)
        {
            DbDVIRLog dbDVIRLog = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                GeotabId = dvirLog.Id.ToString(),
                RecordCreationTimeUtc = DateTime.UtcNow
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
    }
}
