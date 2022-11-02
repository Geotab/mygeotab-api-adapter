using Dapper;
using FastMember;
using Microsoft.Data.SqlClient;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using NLog;
using Polly;
using Polly.Wrap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Dapper.SqlMapper;

namespace MyGeotabAPIAdapter.Database.EntityPersisters
{
    /// <summary>
    /// A generic class with methods involving the persistence of <typeparamref name="T"/> entities to a corresponding database table.
    /// </summary>
    public class GenericEntityPersister<T> : IGenericEntityPersister<T> where T : class, IDbEntity
    {
        // Obtain the type parameter type.
        readonly Type typeParameterType = typeof(T);

        const int MinimumEntityCountForBulkOperations = 2;
        const string EntityIdFieldName = "id";
        const int SqlBulkCopyBatchSize = 10000;

        // Polly-related items:
        const int MaxRetries = 10;
        AsyncPolicyWrap bulkDeleteAsyncDatabaseCommandTimeoutAndRetryPolicyWrap = null;
        AsyncPolicyWrap bulkInsertAsyncDatabaseCommandTimeoutAndRetryPolicyWrap = null;
        AsyncPolicyWrap bulkUpdateAsyncDatabaseCommandTimeoutAndRetryPolicyWrap = null;

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

        /// <summary>
        /// Bulk deletes entities from s SQL Server database table.
        /// </summary>
        /// <param name="sqlContext">The <see cref="IDatabaseUnitOfWorkContext"/> to use.</param>
        /// <param name="entitiesToDelete">The list of entities to be bulk deleted.</param>
        /// <param name="destinationTableName">The name of the database table from which the <paramref name="entitiesToDelete"/> are to be bulk deleted.</param>
        /// <returns></returns>
        async Task BulkDeleteSqlServerAsync(IDatabaseUnitOfWorkContext sqlContext, IEnumerable<T> entitiesToDelete, string destinationTableName)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var tempTableName = $"#_MyGeotabAPIAdapter_BulkDelete_{destinationTableName}";
            var clusteredIndexName = $"IX_{tempTableName}";
            var sqlConnection = sqlContext.GetSqlConnection();
            var sqlTransaction = sqlContext.GetSqlTransaction();
            string sql = "";
            var deletedRecordCount = 0;

            // Setup command timeout and retry policies.
            if (bulkDeleteAsyncDatabaseCommandTimeoutAndRetryPolicyWrap == null)
            {
                bulkDeleteAsyncDatabaseCommandTimeoutAndRetryPolicyWrap = DatabaseResilienceHelper.CreateAsyncPolicyWrapForDatabaseCommandTimeoutAndRetry<Exception>(sqlContext.TimeoutSecondsForDatabaseTasks, MaxRetries, logger);
            }

            // Create a temporary table.
            await bulkDeleteAsyncDatabaseCommandTimeoutAndRetryPolicyWrap.ExecuteAsync(async pollyContext =>
            {
                var timeoutTimeSpan = (TimeSpan)pollyContext[DatabaseResilienceHelper.PollyContextKeyRetryAttemptTimeoutTimeSpan];
                var timeoutSeconds = (int)timeoutTimeSpan.TotalSeconds;

                sql = $"SELECT [{EntityIdFieldName}] INTO [{tempTableName}] FROM [{destinationTableName}] WHERE (1 = 0);";
                _ = await sqlConnection.ExecuteAsync(sql, null, sqlTransaction, timeoutSeconds);
            }, new Context());

            // Write records to be deleted into the temporary table using SqlBulkCopy.
            await bulkDeleteAsyncDatabaseCommandTimeoutAndRetryPolicyWrap.ExecuteAsync(async pollyContext =>
            {
                var timeoutTimeSpan = (TimeSpan)pollyContext[DatabaseResilienceHelper.PollyContextKeyRetryAttemptTimeoutTimeSpan];
                var timeoutSeconds = (int)timeoutTimeSpan.TotalSeconds;

                using var sqlBulkCopy = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.KeepIdentity, sqlTransaction);
                sqlBulkCopy.DestinationTableName = tempTableName;
                sqlBulkCopy.BatchSize = SqlBulkCopyBatchSize;
                sqlBulkCopy.BulkCopyTimeout = timeoutSeconds;
                sqlBulkCopy.ColumnMappings.Add(EntityIdFieldName, EntityIdFieldName);
                var insertableProperties = new string[] { EntityIdFieldName };
                using var reader = ObjectReader.Create(entitiesToDelete, insertableProperties);
                await sqlBulkCopy.WriteToServerAsync(reader);
            }, new Context());

            // Create a clustered index on the temporary table.
            await bulkDeleteAsyncDatabaseCommandTimeoutAndRetryPolicyWrap.ExecuteAsync(async pollyContext =>
            {
                var timeoutTimeSpan = (TimeSpan)pollyContext[DatabaseResilienceHelper.PollyContextKeyRetryAttemptTimeoutTimeSpan];
                var timeoutSeconds = (int)timeoutTimeSpan.TotalSeconds;

                sql = $"CREATE CLUSTERED INDEX [{clusteredIndexName}] ON [{tempTableName}] ( [{EntityIdFieldName}] ASC );";
                _ = await sqlConnection.ExecuteAsync(sql, null, sqlTransaction, timeoutSeconds);
            }, new Context());

            // Perform the actual deletion of records from the destination table.
            await bulkDeleteAsyncDatabaseCommandTimeoutAndRetryPolicyWrap.ExecuteAsync(async pollyContext =>
            {
                var timeoutTimeSpan = (TimeSpan)pollyContext[DatabaseResilienceHelper.PollyContextKeyRetryAttemptTimeoutTimeSpan];
                var timeoutSeconds = (int)timeoutTimeSpan.TotalSeconds;

                sql = $"DELETE D FROM [{destinationTableName}] D INNER JOIN [{tempTableName}] T ON (T.[{EntityIdFieldName}] = D.[{EntityIdFieldName}]);";
                deletedRecordCount = await sqlConnection.ExecuteAsync(sql, null, sqlTransaction, timeoutSeconds);
            }, new Context());

            // Drop the temporary table.
            await bulkDeleteAsyncDatabaseCommandTimeoutAndRetryPolicyWrap.ExecuteAsync(async pollyContext =>
            {
                var timeoutTimeSpan = (TimeSpan)pollyContext[DatabaseResilienceHelper.PollyContextKeyRetryAttemptTimeoutTimeSpan];
                var timeoutSeconds = (int)timeoutTimeSpan.TotalSeconds;

                sql = $"DROP TABLE [{tempTableName}];";
                _ = await sqlConnection.ExecuteAsync(sql, null, sqlTransaction, timeoutSeconds);
            }, new Context());

            // Verify that all records were deleted or throw an exception.
            if (deletedRecordCount != entitiesToDelete.Count())
            {
                throw new Exception($"Only {deletedRecordCount} of the {entitiesToDelete.Count()} target records were bulk-deleted from the {destinationTableName} table. Database transaction will be rolled-back.");
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Bulk inserts entities into a SQL Server database table using <see cref="SqlBulkCopy"/>.
        /// </summary>
        /// <param name="sqlContext">The <see cref="IDatabaseUnitOfWorkContext"/> to use.</param>
        /// <param name="entitiesToInsert">The list of entities to be bulk inserted.</param>
        /// <param name="destinationTableName">The name of the database table into which the <paramref name="entitiesToInsert"/> are to be bulk inserted.</param>
        /// <returns></returns>
        async Task BulkInsertSqlServerAsync(IDatabaseUnitOfWorkContext sqlContext, IEnumerable<T> entitiesToInsert, string destinationTableName)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // Setup a database command timeout and retry policy wrap.
            if (bulkInsertAsyncDatabaseCommandTimeoutAndRetryPolicyWrap == null)
            {
                bulkInsertAsyncDatabaseCommandTimeoutAndRetryPolicyWrap = DatabaseResilienceHelper.CreateAsyncPolicyWrapForDatabaseCommandTimeoutAndRetry<Exception>(sqlContext.TimeoutSecondsForDatabaseTasks, MaxRetries, logger);
            }
            await bulkInsertAsyncDatabaseCommandTimeoutAndRetryPolicyWrap.ExecuteAsync(async pollyContext =>
            {
                var timeoutTimeSpan = (TimeSpan)pollyContext[DatabaseResilienceHelper.PollyContextKeyRetryAttemptTimeoutTimeSpan];
                var timeoutSeconds = (int)timeoutTimeSpan.TotalSeconds;

                var sqlConnection = sqlContext.GetSqlConnection();
                var sqlTransaction = sqlContext.GetSqlTransaction();
                using var sqlBulkCopy = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.Default, sqlTransaction);
                sqlBulkCopy.DestinationTableName = destinationTableName;
                sqlBulkCopy.BatchSize = SqlBulkCopyBatchSize;
                sqlBulkCopy.BulkCopyTimeout = timeoutSeconds;
                var insertableProperties = PropertyHelper.GetInsertablePropertyNames(typeof(T));
                foreach (var insertableProperty in insertableProperties)
                {
                    sqlBulkCopy.ColumnMappings.Add(insertableProperty, insertableProperty);
                }
                using var reader = ObjectReader.Create(entitiesToInsert, insertableProperties);
                await sqlBulkCopy.WriteToServerAsync(reader);
            }, new Context());

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Bulk updates entities in s SQL Server database table.
        /// </summary>
        /// <param name="sqlContext">The <see cref="IDatabaseUnitOfWorkContext"/> to use.</param>
        /// <param name="entitiesToUpdate">The list of entities to be bulk updated.</param>
        /// <param name="destinationTableName">The name of the database table to be updated with the <paramref name="entitiesToUpdate"/>.</param>
        /// <returns></returns>
        async Task BulkUpdateSqlServerAsync(IDatabaseUnitOfWorkContext sqlContext, IEnumerable<T> entitiesToUpdate, string destinationTableName)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var tempTableName = $"#_MyGeotabAPIAdapter_BulkUpdate_{destinationTableName}";
            var clusteredIndexName = $"IX_{tempTableName}";
            var sqlConnection = sqlContext.GetSqlConnection();
            var sqlTransaction = sqlContext.GetSqlTransaction();
            var sql = "";
            var returnVal = 0;
            var updatedRecordCount = 0;

            // Setup command timeout and retry policies.
            if (bulkUpdateAsyncDatabaseCommandTimeoutAndRetryPolicyWrap == null)
            {
                bulkUpdateAsyncDatabaseCommandTimeoutAndRetryPolicyWrap = DatabaseResilienceHelper.CreateAsyncPolicyWrapForDatabaseCommandTimeoutAndRetry<Exception>(sqlContext.TimeoutSecondsForDatabaseTasks, MaxRetries, logger);
            }

            // Build SQL statement required for bulk insert of the entitiesToUpdate into a temporary table.
            var allProperties = PropertyHelper.GetAllWriteablePropertyNames(typeof(T));
            var insertableProperties = PropertyHelper.GetInsertablePropertyNames(typeof(T));
            var keyAndUpdatableProperties = PropertyHelper.GetKeyAndUpdatablePropertyNames(typeof(T));
            var sqlBuilder = new StringBuilder();
            sqlBuilder.Append($"SELECT [{EntityIdFieldName}]");
            foreach (var property in insertableProperties)
            {
                // Exclude id column name since it was already added.
                if (property != EntityIdFieldName)
                {
                    sqlBuilder.Append($", [{property}]");
                }
            }
            sqlBuilder.Append($" INTO [{tempTableName}] FROM [{destinationTableName}] WHERE (1 = 0);");
            var sqlForTempTableInsert = sqlBuilder.ToString();

            // Build SQL statement required for update of the entitiesToUpdate in the destination table using the entities that will be bulk inserted into the temporary table.
            sqlBuilder = new StringBuilder();
            var firstPropertyAddedToSql = false;
            sqlBuilder.Append($"UPDATE D SET ");
            foreach (var insertableProperty in insertableProperties)
            {
                if (firstPropertyAddedToSql)
                {
                    sqlBuilder.Append($", D.[{insertableProperty}] = T.[{insertableProperty}]");
                }
                else
                {
                    sqlBuilder.Append($"D.[{insertableProperty}] = T.[{insertableProperty}]");
                    firstPropertyAddedToSql |= true;
                }
            }
            sqlBuilder.Append($" FROM [{destinationTableName}] D INNER JOIN [{tempTableName}] T ON (T.[{EntityIdFieldName}] = D.[{EntityIdFieldName}]);");
            var sqlForDestinationTableUpdate = sqlBuilder.ToString();

            // Create a temporary table.
            await bulkUpdateAsyncDatabaseCommandTimeoutAndRetryPolicyWrap.ExecuteAsync(async pollyContext =>
            {
                var timeoutTimeSpan = (TimeSpan)pollyContext[DatabaseResilienceHelper.PollyContextKeyRetryAttemptTimeoutTimeSpan];
                var timeoutSeconds = (int)timeoutTimeSpan.TotalSeconds;

                _ = await sqlConnection.ExecuteAsync(sqlForTempTableInsert, null, sqlTransaction, timeoutSeconds);
            }, new Context());

            // Write records to be updated into the temporary table using SqlBulkCopy.
            await bulkUpdateAsyncDatabaseCommandTimeoutAndRetryPolicyWrap.ExecuteAsync(async pollyContext =>
            {
                var timeoutTimeSpan = (TimeSpan)pollyContext[DatabaseResilienceHelper.PollyContextKeyRetryAttemptTimeoutTimeSpan];
                var timeoutSeconds = (int)timeoutTimeSpan.TotalSeconds;

                using var sqlBulkCopy = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.KeepIdentity, sqlTransaction);
                sqlBulkCopy.DestinationTableName = tempTableName;
                sqlBulkCopy.BatchSize = SqlBulkCopyBatchSize;
                sqlBulkCopy.BulkCopyTimeout = timeoutSeconds;
                foreach (var property in keyAndUpdatableProperties)
                {
                    sqlBulkCopy.ColumnMappings.Add(property, property);
                }
                using var reader = ObjectReader.Create(entitiesToUpdate, keyAndUpdatableProperties);
                await sqlBulkCopy.WriteToServerAsync(reader);

            }, new Context());

            // Create a clustered index on the temporary table.
            await bulkUpdateAsyncDatabaseCommandTimeoutAndRetryPolicyWrap.ExecuteAsync(async pollyContext =>
            {
                var timeoutTimeSpan = (TimeSpan)pollyContext[DatabaseResilienceHelper.PollyContextKeyRetryAttemptTimeoutTimeSpan];
                var timeoutSeconds = (int)timeoutTimeSpan.TotalSeconds;

                sql = $"CREATE CLUSTERED INDEX [{clusteredIndexName}] ON [{tempTableName}] ( [{EntityIdFieldName}] ASC );";
                returnVal = await sqlConnection.ExecuteAsync(sql, null, sqlTransaction, timeoutSeconds);

            }, new Context());

            // Perform the actual update of records in the destination table.
            await bulkUpdateAsyncDatabaseCommandTimeoutAndRetryPolicyWrap.ExecuteAsync(async pollyContext =>
            {
                var timeoutTimeSpan = (TimeSpan)pollyContext[DatabaseResilienceHelper.PollyContextKeyRetryAttemptTimeoutTimeSpan];
                var timeoutSeconds = (int)timeoutTimeSpan.TotalSeconds;

                updatedRecordCount = await sqlConnection.ExecuteAsync(sqlForDestinationTableUpdate, null, sqlTransaction, timeoutSeconds);
            }, new Context());

            // Drop the temporary table.
            await bulkUpdateAsyncDatabaseCommandTimeoutAndRetryPolicyWrap.ExecuteAsync(async pollyContext =>
            {
                var timeoutTimeSpan = (TimeSpan)pollyContext[DatabaseResilienceHelper.PollyContextKeyRetryAttemptTimeoutTimeSpan];
                var timeoutSeconds = (int)timeoutTimeSpan.TotalSeconds;

                sql = $"DROP TABLE [{tempTableName}];";
                returnVal = await sqlConnection.ExecuteAsync(sql, null, sqlTransaction, timeoutSeconds);
            }, new Context());

            // Verify that all records were updated or throw an exception.
            if (updatedRecordCount != entitiesToUpdate.Count())
            {
                throw new Exception($"Only {updatedRecordCount} of the {entitiesToUpdate.Count()} target records were bulk-updated in the {destinationTableName} table. Database transaction will be rolled-back.");
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public async Task PersistEntitiesToDatabaseAsync(IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext, IEnumerable<T> entitiesToPersist, CancellationTokenSource cancellationTokenSource, Logging.LogLevel logLevel, bool logPersistenceOperationDetails = true)
        {
            await PersistEntitiesToDatabaseAsync(adapterContext, entitiesToPersist, cancellationTokenSource, logLevel, logPersistenceOperationDetails, adapterContext);
        }

        /// <inheritdoc/>
        public async Task PersistEntitiesToDatabaseAsync(IGenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext> optimizerContext, IEnumerable<T> entitiesToPersist, CancellationTokenSource cancellationTokenSource, Logging.LogLevel logLevel, bool logPersistenceOperationDetails = true)
        {
            await PersistEntitiesToDatabaseAsync(optimizerContext, entitiesToPersist, cancellationTokenSource, logLevel, logPersistenceOperationDetails, null, optimizerContext);
        }

        /// <summary>
        /// Persists the <paramref name="entitiesToPersist"/> to database. Note: Either the <paramref name="adapterContext"/> or <paramref name="optimizerContext"/> must be supplied (depending on which database is being connected to).
        /// </summary>
        /// <param name="context">The <see cref="IConnectionContext"/> to use.</param>
        /// <param name="entitiesToPersist">The list of entities to be persisted to database.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="logLevel">The <see cref="LogLevel"/> to apply to the log message.</param>
        /// <param name="logPersistenceOperationDetails">Indicates whether to log the details of the persistence operation (i.e. number of records inserted/updated, etc.)</param>
        /// <param name="adapterContext">The <see cref="AdapterDatabaseUnitOfWorkContext"/> to use.</param>
        /// <param name="optimizerContext">The <see cref="OptimizerDatabaseUnitOfWorkContext"/> to use.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Thrown if null values are supplied for both <paramref name="adapterContext"/> and <paramref name="optimizerContext"/></exception>
        async Task PersistEntitiesToDatabaseAsync(IConnectionContext context, IEnumerable<T> entitiesToPersist, CancellationTokenSource cancellationTokenSource, Logging.LogLevel logLevel, bool logPersistenceOperationDetails = true, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext = null, IGenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext> optimizerContext = null)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // One of the IGenericDatabaseUnitOfWorkContext implementations must be provided.
            if (adapterContext == null && optimizerContext == null)
            {
                throw new ArgumentException($"Null values were provided to both the '{nameof(adapterContext)}' and '{nameof(optimizerContext)}' properties. A value must be provided for one of these properties.");
            }

            try
            {
                if (entitiesToPersist.Any())
                {
                    string databaseTableName = "";
                    long bulkInsertedEntityCount = 0;
                    long bulkUpdatedEntityCount = 0;
                    long bulkDeletedEntityCount = 0;
                    long insertedEntityCount = 0;
                    long updatedEntityCount = 0;
                    long deletedEntityCount = 0;
                    DateTime startTimeUTC;
                    DateTime endTimeUTC;
                    TimeSpan elapsedTime;
                    TimeSpan totalTimeForBulkInserts = new();
                    TimeSpan totalTimeForBulkUpdates = new();
                    TimeSpan totalTimeForBulkDeletes = new();
                    TimeSpan totalTimeForInserts = new();
                    TimeSpan totalTimeForUpdates = new();
                    TimeSpan totalTimeForDeletes = new();

                    // Get the name of the database table.
                    if (entitiesToPersist.Any())
                    {
                        databaseTableName = entitiesToPersist.First().DatabaseTableName;
                    }

                    // Determine the database type.
                    var providerType = ConnectionInfo.DataAccessProviderType.NullValue;
                    if (adapterContext != null)
                    {
                        providerType = adapterContext.ProviderType;
                    }
                    else
                    {
                        providerType = optimizerContext.ProviderType;
                    }

                    // If SQL Server is the database type and the minimum number of entities for bulk operations is met, re-route any inserts to the bulk insert method and process everything else (i.e. updates and deletes_ normally. For all other database types or if the entity count threshold is not met, process everything normally. Note: DbDiagnosticT and DbDiagnosticIdT run into an issue with bulk update. This can be investigated and remedied later, but for now just use regular CRUD operations for these. OProcessorTracking, OServiceTracking and DutyStatusAvailability must also use regular CRUD operations since they have no actual GeotabId column and bulk update logic uses GeotabId.
                    if (providerType == ConnectionInfo.DataAccessProviderType.SQLServer && entitiesToPersist.Count() >= MinimumEntityCountForBulkOperations && typeParameterType != typeof(DbDiagnosticT) && typeParameterType != typeof(DbDiagnosticIdT) && typeParameterType != typeof(DbOProcessorTracking) && typeParameterType != typeof(DbOServiceTracking) && typeParameterType != typeof(DbDutyStatusAvailability))
                    {
                        var entitiesToBulkDelete = new List<T>();
                        var entitiesToBulkInsert = new List<T>();
                        var entitiesToBulkUpdate = new List<T>();
                        foreach (var entityToPersist in entitiesToPersist)
                        {
                            switch (entityToPersist.DatabaseWriteOperationType)
                            {
                                case Common.DatabaseWriteOperationType.None:
                                    // Nothing to do with this entity.
                                    break;
                                case Common.DatabaseWriteOperationType.Insert:
                                    entitiesToBulkInsert.Add(entityToPersist);
                                    break;
                                case Common.DatabaseWriteOperationType.Update:
                                    entitiesToBulkUpdate.Add(entityToPersist);
                                    break;
                                case Common.DatabaseWriteOperationType.Delete:
                                    entitiesToBulkDelete.Add(entityToPersist);
                                    break;
                                default:
                                    throw new NotSupportedException($"The {entityToPersist.DatabaseWriteOperationType} DatabaseWriteOperationType is not supported by this method.");
                            }
                        }

                        // Bulk Insert.
                        bulkInsertedEntityCount = entitiesToBulkInsert.Count;
                        if (entitiesToBulkInsert.Count > 0)
                        {
                            startTimeUTC = DateTime.UtcNow;
                            if (adapterContext != null)
                            {
                                await BulkInsertSqlServerAsync(adapterContext, entitiesToBulkInsert, databaseTableName);
                            }
                            else
                            {
                                await BulkInsertSqlServerAsync(optimizerContext, entitiesToBulkInsert, databaseTableName);
                            }
                            endTimeUTC = DateTime.UtcNow;
                            totalTimeForBulkInserts = endTimeUTC.Subtract(startTimeUTC);
                        }

                        // Bulk Update.
                        bulkUpdatedEntityCount = entitiesToBulkUpdate.Count;
                        if (entitiesToBulkUpdate.Count > 0)
                        {
                            startTimeUTC = DateTime.UtcNow;
                            if (adapterContext != null)
                            {
                                await BulkUpdateSqlServerAsync(adapterContext, entitiesToBulkUpdate, databaseTableName);
                            }
                            else
                            {
                                await BulkUpdateSqlServerAsync(optimizerContext, entitiesToBulkUpdate, databaseTableName);
                            }
                            endTimeUTC = DateTime.UtcNow;
                            totalTimeForBulkUpdates = endTimeUTC.Subtract(startTimeUTC);
                        }

                        // Bulk Delete.
                        bulkDeletedEntityCount = entitiesToBulkDelete.Count;
                        if (entitiesToBulkDelete.Count > 0)
                        {
                            startTimeUTC = DateTime.UtcNow;
                            if (adapterContext != null)
                            {
                                await BulkDeleteSqlServerAsync(adapterContext, entitiesToBulkDelete, databaseTableName);
                            }
                            else
                            {
                                await BulkDeleteSqlServerAsync(optimizerContext, entitiesToBulkDelete, databaseTableName);
                            }
                            endTimeUTC = DateTime.UtcNow;
                            totalTimeForBulkDeletes = endTimeUTC.Subtract(startTimeUTC);
                        }
                    }
                    else
                    {
                        // Normal processing of entities if not using SQL Server.
                        var entityRepo = new BaseRepository<T>(context);

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
                                    updatedEntityCount++;
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
                    }

                    // Log details of the persistence operation(s): 
                    if (logPersistenceOperationDetails == true)
                    {
                        if (bulkInsertedEntityCount > 0)
                        {
                            entityPersistenceLogger.LogPersistenceOperationDetails(Common.DatabaseWriteOperationType.BulkInsert, bulkInsertedEntityCount, context.Database, databaseTableName, totalTimeForBulkInserts, logLevel);
                        }
                        if (bulkUpdatedEntityCount > 0)
                        {
                            entityPersistenceLogger.LogPersistenceOperationDetails(Common.DatabaseWriteOperationType.BulkUpdate, bulkUpdatedEntityCount, context.Database, databaseTableName, totalTimeForBulkUpdates, logLevel);
                        }
                        if (bulkDeletedEntityCount > 0)
                        {
                            entityPersistenceLogger.LogPersistenceOperationDetails(Common.DatabaseWriteOperationType.BulkDelete, bulkDeletedEntityCount, context.Database, databaseTableName, totalTimeForBulkDeletes, logLevel);
                        }
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
