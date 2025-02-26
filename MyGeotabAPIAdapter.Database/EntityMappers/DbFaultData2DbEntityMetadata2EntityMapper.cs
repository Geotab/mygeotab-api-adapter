using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.MyGeotabAPI;
using System;
using System.Collections.Generic;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="DbFaultData2"/> and <see cref="DbEntityMetadata2"/> entities.
    /// </summary>
    public class DbFaultData2DbEntityMetadata2EntityMapper : IDbFaultData2DbEntityMetadata2EntityMapper
    {
        /// <inheritdoc/>
        public List<DbEntityMetadata2> CreateEntities(List<DbFaultData2> entitiesToMapTo)
        {
            DateTime recordCreationTimeUtc = DateTime.UtcNow;
            var dbEntityMetadata2s = new List<DbEntityMetadata2>();
            foreach (var entity in entitiesToMapTo)
            {
                var dbEntityMetadata2 = CreateEntity(entity);
                dbEntityMetadata2.RecordCreationTimeUtc = recordCreationTimeUtc;
                dbEntityMetadata2s.Add(dbEntityMetadata2);
            }
            return dbEntityMetadata2s;
        }

        /// <inheritdoc/>
        public DbEntityMetadata2 CreateEntity(DbFaultData2 entityToMapTo)
        {
            DbEntityMetadata2 dbEntityMetadata2 = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DateTime = entityToMapTo.DateTime,
                DeviceId = entityToMapTo.DeviceId,
                EntityId = entityToMapTo.id,
                EntityType = (byte)GeotabEntityType.FaultData.Id,
                IsDeleted = null,
                RecordCreationTimeUtc = DateTime.UtcNow
            };
            return dbEntityMetadata2;
        }
    }
}
