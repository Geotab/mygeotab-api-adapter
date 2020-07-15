using MyGeotabAPIAdapter.Database.DataAccess;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Logic
{
    /// <summary>
    /// A service class that handles database CRUD operations for for entities based on SQL.
    /// </summary>
    public class DbGeneralService
    {
        /// <summary>
        /// Executes a query using the supplied <see cref="ConnectionInfo"/> and SQL statement and returns the results as an <see cref="IEnumerable{T}"/> containing dynamic data.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="sql">The SQL statement to be executed.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<IEnumerable<dynamic>> ExecDynamicQueryAsync(ConnectionInfo connectionInfo, string sql, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbGeneralRepository().ExecDynamicQueryAsync(connectionInfo, sql, cancellationTokenSource, commandTimeout);
        }

        /// <summary>
        /// Executes a query using the supplied <see cref="ConnectionInfo"/> and SQL statement and returns the results as an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="System.Type"/> of object to be retrieved.</typeparam>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="sql">The SQL statement to be executed.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<IEnumerable<T>> ExecDynamicTypeQueryAsync<T>(ConnectionInfo connectionInfo, string sql, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbGeneralRepository().ExecDynamicTypeQueryAsync<T>(connectionInfo, sql, cancellationTokenSource, commandTimeout);
        }
    }
}
