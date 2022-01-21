using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbLogRecordT"/> entities.
    /// </summary>
    public class DbLogRecordTRepository : BaseRepository2<DbLogRecordT>
    {
        readonly IOptimizerDatabaseObjectNames optimizerDatabaseObjectNames;

        public DbLogRecordTRepository(IConnectionContext context, IOptimizerDatabaseObjectNames optimizerDatabaseObjectNames) : base(context) 
        {
            this.optimizerDatabaseObjectNames = optimizerDatabaseObjectNames;
        }

        /// <summary>
        /// Executes a stored procedure to retrieve a batch of <see cref="DbLogRecordT"/> entities satisfying the requirements of the supplied parameter values.
        /// </summary>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="minDateTime">Only include records where the DateTime is greater than this value. If <c>null</c>, <see cref="BaseRepository2{T}.MinDateTimeForDatabaseParameters"/> will be substituted.</param>
        /// <param name="maxDateTime">Only include records where the DateTime is less than this value. If <c>null</c>, <see cref="BaseRepository2{T}.MaxDateTimeForDatabaseParameters"/> will be substituted.</param>
        /// <param name="resultsLimit">The maximum number of entities to return. If null, no limit is applied.</param>
        /// <returns></returns>
        public async Task<IEnumerable<DbLogRecordT>> GetLogRecordTBatchAsync(CancellationTokenSource methodCancellationTokenSource, DateTime? minDateTime, DateTime? maxDateTime, int? resultsLimit = null)
        {
            if (minDateTime == null)
            {
                minDateTime = MinDateTimeForDatabaseParameters;
            }

            if (maxDateTime == null)
            {
                maxDateTime = MaxDateTimeForDatabaseParameters;
            }

            var parameters = new
            {
                MinDateTimeUTC = minDateTime,
                MaxDateTimeUTC = maxDateTime,
                ResultsLimit = resultsLimit
            };
            var results = await base.ExecuteStoredProcedureQueryAsync(optimizerDatabaseObjectNames.GetLogRecordTBatchStoredProcedureName, parameters, methodCancellationTokenSource);
            return results;
        }
    }
}
