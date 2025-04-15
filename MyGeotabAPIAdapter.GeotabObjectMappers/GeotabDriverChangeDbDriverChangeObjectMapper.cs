using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="DriverChange"/> and <see cref="DbDriverChange"/> entities.
    /// </summary>
    public class GeotabDriverChangeDbDriverChangeObjectMapper : IGeotabDriverChangeDbDriverChangeObjectMapper
    {
        /// <inheritdoc/>
        public List<DbDriverChange> CreateEntities(List<DriverChange> entitiesToMapTo)
        {
            DateTime recordCreationTimeUtc = DateTime.UtcNow;
            var dbDriverChanges = new List<DbDriverChange>();
            foreach (var entity in entitiesToMapTo)
            {
                var dbDriverChange = CreateEntity(entity);
                dbDriverChange.RecordCreationTimeUtc = recordCreationTimeUtc;
                dbDriverChanges.Add(dbDriverChange);
            }
            return dbDriverChanges;
        }

        /// <inheritdoc/>
        public DbDriverChange CreateEntity(DriverChange entityToMapTo)
        {
            Device driverChangeDevice = entityToMapTo.Device;
            Driver driverChangeDriver = entityToMapTo.Driver;

            DbDriverChange dbDriverChange = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DateTime = entityToMapTo.DateTime,
                DeviceId = driverChangeDevice.Id.ToString(),
                DriverId = driverChangeDriver.Id.ToString(),
                GeotabId = entityToMapTo.Id.ToString(),
                RecordCreationTimeUtc = DateTime.UtcNow
            };
            if (entityToMapTo.Type != null)
            {
                dbDriverChange.Type = Enum.GetName(typeof(DriverChangeType), entityToMapTo.Type);
            }
            if (entityToMapTo.Version != null)
            {
                dbDriverChange.Version = (long)entityToMapTo.Version;
            }
            return dbDriverChange;
        }
    }
}
