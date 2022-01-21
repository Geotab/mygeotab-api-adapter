using MyGeotabAPIAdapter.Logging;
using NLog;
using System;
using System.Reflection;

namespace MyGeotabAPIAdapter.Database.EntityPersisters
{
    /// <summary>
    /// A class that assists with logging persistence opertaion details.
    /// </summary>
    public class EntityPersistenceLogger : IEntityPersistenceLogger
    {
        const string From = "from";
        const string In = "in";
        const string Into = "into";

        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityPersistenceLogger"/> class.
        /// </summary>
        public EntityPersistenceLogger()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public void LogPersistenceOperationDetails(Common.DatabaseWriteOperationType databaseWriteOperationType, long affectedEntityCount, Databases database, string databaseTableName, TimeSpan elapsedTime, Logging.LogLevel logLevel)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            string databaseOperationPreposition = "";
            switch (databaseWriteOperationType)
            {
                case Common.DatabaseWriteOperationType.Insert:
                    databaseOperationPreposition = Into;
                    break;
                case Common.DatabaseWriteOperationType.Update:
                    databaseOperationPreposition = In;
                    break;
                case Common.DatabaseWriteOperationType.Delete:
                    databaseOperationPreposition = From;
                    break;
                case Common.DatabaseWriteOperationType.None:
                    break;
                default:
                    throw new NotSupportedException($"The {databaseWriteOperationType} DatabaseWriteOperationType is not supported by this method.");
            }

            double recordsProcessedPerSecond = (double)affectedEntityCount / (double)elapsedTime.TotalSeconds;
           
            var message = $"Completed {databaseWriteOperationType} of {affectedEntityCount} records {databaseOperationPreposition} the {databaseTableName} table in the {database} database in {elapsedTime.TotalSeconds} seconds ({recordsProcessedPerSecond} per second throughput).";

            switch (logLevel)
            {
                case Logging.LogLevel.Debug:
                    logger.Debug(message);
                    break;
                case Logging.LogLevel.Error:
                    logger.Error(message);
                    break;
                case Logging.LogLevel.Fatal:
                    logger.Fatal(message);
                    break;
                case Logging.LogLevel.Info:
                    logger.Info(message);
                    break;
                case Logging.LogLevel.Off:
                    break;
                case Logging.LogLevel.Trace:
                    logger.Trace(message);
                    break;
                case Logging.LogLevel.Warn:
                    logger.Warn(message);
                    break;
                default:
                    break;
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
