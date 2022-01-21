using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.EntityPersisters
{
    /// <summary>
    /// A generic class with methods involving the persistence of <typeparamref name="T"/> entities to a corresponding database table.
    /// </summary>
    public class GenericEntityPersister<T> : IGenericEntityPersister<T> where T : class, IDbEntity
    {
        readonly IEntityPersistenceLogger entityPersistenceLogger;
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericEntityPersister{T}"/> class.
        /// </summary>
        /// <param name="entityPersistenceLogger">The <see cref="IEntityPersistenceLogger"/> to use.</param>
        public GenericEntityPersister(IEntityPersistenceLogger entityPersistenceLogger)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.entityPersistenceLogger = entityPersistenceLogger;

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public async Task PersistEntitiesToDatabaseAsync(IConnectionContext context, IEnumerable<T> entitiesToPersist, CancellationTokenSource cancellationTokenSource, Logging.LogLevel logLevel, bool logPersistenceOperationDetails = true)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            try
            {
                if (entitiesToPersist.Any())
                {
                    string databaseTableName = "";
                    long insertedEntityCount = 0;
                    long updatedEntityCount = 0;
                    long deletedEntityCount = 0;
                    DateTime startTimeUTC;
                    DateTime endTimeUTC;
                    TimeSpan elapsedTime;
                    TimeSpan totalTimeForInserts = new();
                    TimeSpan totalTimeForUpdates = new();
                    TimeSpan totalTimeForDeletes = new();

                    var entityRepo = new BaseRepository2<T>(context);

                    if (entitiesToPersist.Any())
                    {
                        databaseTableName = entitiesToPersist.First().DatabaseTableName;
                    }

                    // Persist all of the entities:
                    foreach (var entityToPersist in entitiesToPersist)
                    {
                        if (entityToPersist.DatabaseWriteOperationType == Common.DatabaseWriteOperationType.None)
                        {
                            // Nothing to do with this entity. 
                            continue;
                        }
                        
                        startTimeUTC = DateTime.UtcNow;
                        switch (entityToPersist.DatabaseWriteOperationType)
                        {
                            case Common.DatabaseWriteOperationType.Insert:
                                await entityRepo.InsertAsync(entityToPersist, cancellationTokenSource);
                                endTimeUTC = DateTime.UtcNow;
                                elapsedTime = endTimeUTC.Subtract(startTimeUTC);
                                totalTimeForInserts = totalTimeForInserts.Add(elapsedTime);
                                insertedEntityCount++;
                                break;
                            case Common.DatabaseWriteOperationType.Update:
                                await entityRepo.UpdateAsync(entityToPersist, cancellationTokenSource);
                                endTimeUTC = DateTime.UtcNow;
                                elapsedTime = endTimeUTC.Subtract(startTimeUTC);
                                totalTimeForUpdates = totalTimeForUpdates.Add(elapsedTime);
                                updatedEntityCount ++;
                                break;
                            case Common.DatabaseWriteOperationType.Delete:
                                await entityRepo.DeleteAsync(entityToPersist, cancellationTokenSource);
                                endTimeUTC = DateTime.UtcNow;
                                elapsedTime = endTimeUTC.Subtract(startTimeUTC);
                                totalTimeForDeletes = totalTimeForDeletes.Add(elapsedTime);
                                deletedEntityCount++;
                                break;
                            default:
                                throw new NotSupportedException($"The {entityToPersist.DatabaseWriteOperationType} DatabaseWriteOperationType is not supported by this method.");
                        }
                        entityToPersist.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.None;
                    }

                    // Log details of the persistence operation(s): 
                    if (logPersistenceOperationDetails == true)
                    {
                        if (insertedEntityCount > 0)
                        {
                            entityPersistenceLogger.LogPersistenceOperationDetails(Common.DatabaseWriteOperationType.Insert, insertedEntityCount, context.Database, databaseTableName, totalTimeForInserts, logLevel);
                        }
                        if (updatedEntityCount > 0)
                        {
                            entityPersistenceLogger.LogPersistenceOperationDetails(Common.DatabaseWriteOperationType.Update, updatedEntityCount, context.Database, databaseTableName, totalTimeForUpdates, logLevel);
                        }
                        if (deletedEntityCount > 0)
                        {
                            entityPersistenceLogger.LogPersistenceOperationDetails(Common.DatabaseWriteOperationType.Delete, deletedEntityCount, context.Database, databaseTableName, totalTimeForDeletes, logLevel);
                        }
                    }
                }
            }
            catch (Exception)
            {
                cancellationTokenSource.Cancel();
                throw;
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
