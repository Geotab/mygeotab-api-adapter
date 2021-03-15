using Geotab.Checkmate.ObjectModel.Exceptions;
using static MyGeotabAPIAdapter.Database.Common;
using MyGeotabAPIAdapter.Database.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Represents a database rule object with its hierarchy of child DbCondition objects. Used to assist with database processing.
    /// </summary>
    public class DbRuleObject : IDisposable
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private bool disposed;

        /// <summary>
        /// Represents the <see cref="MyGeotabAPIAdapter.Database.Models.DbRule"/> object related to this <see cref="DbRuleObject"/>
        /// </summary>
        /// <returns></returns>
        public DbRule DbRule { get; private set; } = new DbRule();

        /// <summary>
        /// Represents a list of <see cref="MyGeotabAPIAdapter.Database.Models.DbCondition"/> objects for this <see cref="DbRuleObject"/>
        /// </summary>
        /// <returns></returns>
        public IList<DbCondition> DbConditions { get; private set; } = new List<DbCondition>();

        /// <summary>
        /// Represents a list of <see cref="MyGeotabAPIAdapter.Database.Models.DbCondition"/> objects already existing in the adapter database for this <see cref="DbRuleObject"/> and which are to be deleted and replaced by <see cref="DbConditions"/> as part of an update.
        /// </summary>
        /// <returns></returns>
        public IList<DbCondition> DbConditionsToBeDeleted { get; set; } = new List<DbCondition>();

        /// <summary>
        /// Returns the Id of the <see cref="DbRule"/> that is represented by this <see cref="DbRuleObject"/> or an empty string if not yet assigned.
        /// </summary>
        public string GeotabId
        {
            get 
            {
                if (this.DbRule != null)
                {
                    return this.DbRule.GeotabId;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Build the <see cref="DbRuleObject"/> by using the <see cref="MyGeotabAPIAdapter.Database.Models.DbRule"/> and <see cref="DbCondition"/> list objects.
        /// </summary>
        /// <param name="dbRule"></param>
        /// <param name="dbConditions"></param>
        public void BuildFromDatabaseObjects(DbRule dbRule, IList<DbCondition> dbConditions)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.DbRule = dbRule;
            this.DbConditions = dbConditions;

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Processes the supplied <see cref="Rule"/> object and produces the <see cref="MyGeotabAPIAdapter.Database.Models.DbRule"/> and <see cref="DbCondition"/> objects contained within it.
        /// </summary>
        /// <param name="rule">A <see cref="Rule"/> object</param>
        /// <param name="entityStatus">The <see cref="Database.Common.DatabaseRecordStatus"/> to be applied to the <see cref="MyGeotabAPIAdapter.Database.Models.DbRule"/>.</param>
        /// <param name="recordLastChanged">The timestamp to be applied to the <see cref="MyGeotabAPIAdapter.Database.Models.DbRule"/>.</param>
        /// <param name="operationType">The <see cref="Database.Common.DatabaseWriteOperationType"/> to be applied to the <see cref="MyGeotabAPIAdapter.Database.Models.DbRule"/>.</param>
        /// <returns>A correctly formatted <see cref="DbRuleObject"/></returns>
        public void BuildRuleObject(Rule rule, int entityStatus,
            DateTime recordLastChanged, DatabaseWriteOperationType operationType)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

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
            if (rule.Condition == null)
            {
                logger.Debug($"Rule '{rule.Id}' has no conditions.");
            }
            else 
            {
                ProcessConditionHierarchy(rule.Condition, entityStatus, recordLastChanged, operationType);
            }
            this.DbRule = dbRule;

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }
            if (disposing)
            {
                this.DbRule = null;
                this.DbConditions.Clear();
            }
            disposed = true;
        }

        /// <summary>
        /// Enumerates the root condition and its hierarchy of children to produce a list of DbCondtion objects that are added to the <see cref="DbCondition"/> list object of the class.
        /// </summary>
        /// <param name="condition">Root condition of a rule</param>
        /// <returns>asynchronous task</returns>
        void ProcessConditionHierarchy(Condition condition, int entityStatus,
            DateTime recordLastChanged, DatabaseWriteOperationType operationType)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // Add the root condition - executed asynchronously to speed things up
            AddCondition(condition, entityStatus, recordLastChanged, operationType);

            if (condition.Children == null)
            {
                // No children to process; exit.
                logger.Debug($"Condition Id: {condition.Id} has no children to process.");
                return;
            }
            Condition currentCondition;
            int countSiblings = condition.Children.Count;
            for (int i = 0; i < countSiblings; i++)
            {
                currentCondition = condition.Children[i];
                logger.Debug(currentCondition.Id.ToString());
                // Process any child conditions - executed asynchronously to speed things up
                ProcessConditionHierarchy(currentCondition, entityStatus, recordLastChanged, operationType);
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Adds a single <see cref="Condition"/> object to the <see cref="DbCondition"/> list object of the class.
        /// </summary>
        /// <param name="condition">The <see cref="Condition"/> object to add</param>
        /// <param name="entityStatus">The <see cref="Database.Common.DatabaseRecordStatus"/> to be applied to the <see cref="MyGeotabAPIAdapter.Database.Models.DbCondition"/>.</param>
        /// <param name="recordLastChanged">The timestamp to be applied to the <see cref="MyGeotabAPIAdapter.Database.Models.DbCondition"/>.</param>
        /// <param name="operationType">The <see cref="Database.Common.DatabaseWriteOperationType"/> to be applied to the <see cref="MyGeotabAPIAdapter.Database.Models.DbCondition"/>.</param>
        /// <returns></returns>
        void AddCondition(Condition condition, int entityStatus,
            DateTime recordLastChanged, DatabaseWriteOperationType operationType)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            string parentId = "";
            if (condition.Parent != null)
            {
                parentId = condition.Parent.Id.ToString();
            }
            logger.Debug($"parentId: {parentId}");
            DbCondition dbCondition = ObjectMapper.GetDbCondition(condition, parentId,
                entityStatus, recordLastChanged, operationType);
            DbConditions.Add(dbCondition);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
