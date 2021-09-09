using Geotab.Checkmate.ObjectModel.Exceptions;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Logic;
using MyGeotabAPIAdapter.Database.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static MyGeotabAPIAdapter.Database.Common;

namespace MyGeotabAPIAdapter
{

    /// <summary>
    /// Provides methods to assist in working with <see cref="Rule"/> and <see cref="Condition"/>objects and their <see cref="DbRule"/> and <see cref="DbCondition"/>counterparts in the adapter database. 
    /// </summary>
    public static class RuleHelper
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Gets the Rules and Conditions from the database and constructs the DbRuleObjects which are then returned as a list.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        public static async Task<IList<DbRuleObject>> GetDatabaseRuleObjectsAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            ConnectionInfo connectionInfo = new(Globals.ConfigurationManager.DatabaseConnectionString, Globals.ConfigurationManager.DatabaseProviderType);

            string sql = "SELECT * from \"vwRuleObject\" ORDER BY \"GeotabId\"";
            IEnumerable<DbVwRuleObject> dbVwRuleObjects;
            dbVwRuleObjects = await DbGeneralService.ExecDynamicTypeQueryAsync<DbVwRuleObject>(connectionInfo, sql, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
            IList<DbRuleObject> dbRuleObjectList = new List<DbRuleObject>();
            string ruleId = "";
            int countOfDbVwRuleObjects = dbVwRuleObjects.Count();
            int countOfProcessedDbVwRuleObjects = 0;
            DbRule dbRule = new();
            DbCondition dbCondition;
            IList<DbCondition> dbConditionList = new List<DbCondition>();
            // Loop through all database rows of vwRuleObject
            foreach (DbVwRuleObject dbVwRuleObject in dbVwRuleObjects)
            {
                string currentRuleId = dbVwRuleObject.GeotabId;
                logger.Debug($"{methodBase.ReflectedType.Name}.{methodBase.Name} - Current RuleId: {currentRuleId}");
                // If rule Id changes go in here
                if (currentRuleId != ruleId)
                {
                    // Add the previous data iteration to the dbRuleObjectList
                    if (ruleId.Length > 0)
                    {
                        // Build the DbRuleObject
                        DbRuleObject dbRuleObject = new();
                        dbRuleObject.BuildFromDatabaseObjects(dbRule, dbConditionList);
                        dbRuleObjectList.Add(dbRuleObject);
                    }
                    // Add the latest rule
                    ruleId = currentRuleId;
                    logger.Debug($"{methodBase.ReflectedType.Name}.{methodBase.Name} - Add rule with Id: {ruleId}");
                    dbRule = new DbRule
                    {
                        id = dbVwRuleObject.RuleAdapterId,
                        GeotabId = dbVwRuleObject.GeotabId,
                        Name = dbVwRuleObject.Name,
                        BaseType = dbVwRuleObject.BaseType,
                        ActiveFrom = dbVwRuleObject.ActiveFrom,
                        ActiveTo = dbVwRuleObject.ActiveTo,
                        Version = dbVwRuleObject.Version,
                        EntityStatus = dbVwRuleObject.EntityStatus,
                        RecordLastChangedUtc = dbVwRuleObject.RecordLastChangedUtc
                    };

                    // Instantiate the next condition list to add the conditions.
                    dbConditionList = new List<DbCondition>();
                }

                // Create the current DbCondition object
                string conditionId = dbVwRuleObject.Cond_Id;
                logger.Debug($"{methodBase.ReflectedType.Name}.{methodBase.Name} - Add condition with Id: {conditionId}");
                dbCondition = new DbCondition
                {
                    id = dbVwRuleObject.ConditionAdapterId,
                    GeotabId = conditionId,
                    ParentId = dbVwRuleObject.Cond_ParentId,
                    RuleId = dbVwRuleObject.Cond_RuleId,
                    ConditionType = dbVwRuleObject.Cond_ConditionType,
                    DeviceId = dbVwRuleObject.Cond_DeviceId,
                    DiagnosticId = dbVwRuleObject.Cond_DiagnosticId,
                    DriverId = dbVwRuleObject.Cond_DriverId,
                    Value = (double?)dbVwRuleObject.Cond_Value,
                    WorkTimeId = dbVwRuleObject.Cond_WorkTimeId,
                    ZoneId = dbVwRuleObject.Cond_ZoneId,
                    EntityStatus = (int)dbVwRuleObject.Cond_EntityStatus,
                    RecordLastChangedUtc = dbVwRuleObject.Cond_RecordLastChangedUtc
                };

                // Add the current DbCondition object to the condition list.
                dbConditionList.Add(dbCondition);
                // Update count for interest.
                countOfProcessedDbVwRuleObjects++;

                // If this is the last DbVwRuleObject being pocessed, a corresponding DbRuleObject must be built and added to the list.
                if (countOfProcessedDbVwRuleObjects == countOfDbVwRuleObjects)
                {
                    DbRuleObject dbRuleObject = new();
                    dbRuleObject.BuildFromDatabaseObjects(dbRule, dbConditionList);
                    dbRuleObjectList.Add(dbRuleObject);
                }
            }
            logger.Info($"{methodBase.ReflectedType.Name}.{methodBase.Name} - DbVwRuleObjects processed: {countOfProcessedDbVwRuleObjects}");
            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return dbRuleObjectList;
        }

        /// <summary>
        /// Inserts the list of <see cref="DbRuleObject"/> objects into the database.
        /// </summary>
        /// <param name="dbRuleObjectList">List of <see cref="DbRuleObject"/> objects to insert.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns>The count of successful inserts</returns>
        public async static Task<int> InsertDbRuleObjectListAsync(List<DbRuleObject> dbRuleObjectList, CancellationTokenSource cancellationTokenSource)
        {
            int insertCount = 0;
            foreach (DbRuleObject dbRuleObject in dbRuleObjectList)
            {
                if(await InsertDbRuleObjectAsync(dbRuleObject, cancellationTokenSource))
                {
                    insertCount++;
                }
            }
            return insertCount;
        }

        /// <summary>
        /// Inserts the <see cref="DbRuleObject"/> object into the database
        /// </summary>
        /// <param name="dbRuleObject">The <see cref="DbRuleObject"/> object to insert.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        public async static Task<bool> InsertDbRuleObjectAsync(DbRuleObject dbRuleObject, CancellationTokenSource cancellationTokenSource)
        {
            ConnectionInfo connectionInfo = new(Globals.ConfigurationManager.DatabaseConnectionString, Globals.ConfigurationManager.DatabaseProviderType);

            long ruleEntitiesInserted;
            ruleEntitiesInserted = await DbRuleService.InsertRuleAsync(connectionInfo, dbRuleObject.DbRule, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);

            logger.Debug($"{ruleEntitiesInserted} rules inserted. Rule Name: {dbRuleObject.DbRule.Name}");

            long condEntitiesInserted;
            condEntitiesInserted = await DbConditionService.InsertAsync(connectionInfo, dbRuleObject.DbConditions, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);

            logger.Debug($"{condEntitiesInserted} conditions inserted");

            if((ruleEntitiesInserted>0) && (condEntitiesInserted>0))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Builds the <see cref="DbRuleObject"/> and then inserts it into the database
        /// </summary>
        /// <param name="ruleList">List of Geotab <see cref="Rule"/> objects</param>
        /// <param name="entityStatus"></param>
        /// <param name="recordLastChanged"></param>
        /// <param name="operationType"></param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns>The count of successful inserts</returns>
        public async static Task<int> InsertDbRuleObjectsFromRuleListAsync(IList<Rule> ruleList, int entityStatus,
            DateTime recordLastChanged, DatabaseWriteOperationType operationType, CancellationTokenSource cancellationTokenSource)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            int insertCount = 0;
            foreach (Rule rule in ruleList)
            {
                using (DbRuleObject dbRuleObject = new())
                {
                    dbRuleObject.BuildRuleObject(rule, entityStatus, recordLastChanged, operationType);
                    if (await InsertDbRuleObjectAsync(dbRuleObject, cancellationTokenSource))
                    {
                        insertCount++;
                    }
                }
                cancellationToken.ThrowIfCancellationRequested();
            }
            return insertCount;
        }

        /// <summary>
        /// Updates the <see cref="DbRuleObject"/> list in the database. So  therefore all <see cref="DbRule"/> objects and <see cref="DbCondition"/> objects contained in the argument are updated.
        /// </summary>
        /// <param name="dbRuleObjectList">The <see cref="DbRuleObject"/> list to update.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns>The count of records affected</returns>
        public static int UpdateDbRuleObjectsToDatabase(List<DbRuleObject> dbRuleObjectList, CancellationTokenSource cancellationTokenSource)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            int count = 0;
            List<DbRule> dbRuleList = dbRuleObjectList.Select(o => o.DbRule).ToList();

            ConnectionInfo connectionInfo = new(Globals.ConfigurationManager.DatabaseConnectionString, Globals.ConfigurationManager.DatabaseProviderType);

            // Update all the DbRules
            Task updateRulesTask = DbRuleService.UpdateAsync(connectionInfo, dbRuleList, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
            count += dbRuleList.Count;

            // Get list of existing conditions to be deleted.
            List<DbCondition> dbConditionsToDelete = new();
            foreach (DbRuleObject dbRuleObject in dbRuleObjectList)
            {
                dbConditionsToDelete.AddRange(dbRuleObject.DbConditionsToBeDeleted);
            }

            // Get list of conditions to be inserted.
            List<DbCondition> dbConditionsToInsert = new();
            foreach (DbRuleObject dbRuleObject in dbRuleObjectList)
            {
                dbConditionsToInsert.AddRange(dbRuleObject.DbConditions);
            }

            // Delete all the DbConditions.
            Task deleteConditionsTask = DbConditionService.DeleteAsync(connectionInfo, dbConditionsToDelete, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);

            //Insert (re-create) all the DbConditions.
            Task insertConditionsTask = DbConditionService.InsertAsync(connectionInfo, dbConditionsToInsert, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
            count += dbConditionsToInsert.Count;

            Task.WaitAll(updateRulesTask, deleteConditionsTask, insertConditionsTask);

            return count;
        }
    }
}
