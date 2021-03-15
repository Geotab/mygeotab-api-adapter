using Dapper;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for for entities based on SQL.
    /// </summary>
    public class DbGeneralRepository : BaseRepository<dynamic>
    {
        /// <summary>
        /// Executes a query using the supplied <see cref="ConnectionInfo"/> and SQL statement and returns the results as an <see cref="IEnumerable{T}"/> containing dynamic data.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="sql">The SQL statement to be executed.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0063:Use simple 'using' statement", Justification = "<Pending>")]
        public async Task<IEnumerable<dynamic>> ExecDynamicQueryAsync(ConnectionInfo connectionInfo, string sql, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            try
            {
                CancellationTokenSource timeoutCancellationTokenSource = new();
                timeoutCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(commandTimeout));

                SetConnection(connectionInfo);
                using (var connection = await new ConnectionProvider(connectionInfo).GetOpenConnectionAsync())
                {
                    var result = await connection.QueryAsync(sql, null, null);
                    cancellationToken.ThrowIfCancellationRequested();
                    return result;
                }
            }
            catch (OperationCanceledException exception)
            {
                cancellationTokenSource.Cancel();
                throw new DatabaseConnectionException($"Database operation did not complete within the allowed time of {commandTimeout} seconds.", exception);
            }
            catch (Exception exception)
            {
                throw new DatabaseConnectionException($"Exception encountered while attempting database operation.", exception);
            }
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
        public async Task<IEnumerable<T>> ExecDynamicTypeQueryAsync<T>(ConnectionInfo connectionInfo, string sql, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            try
            {
                CancellationTokenSource timeoutCancellationTokenSource = new();
                timeoutCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(commandTimeout));

                SetConnection(connectionInfo);
                using (var connection = await new ConnectionProvider(connectionInfo).GetOpenConnectionAsync())
                {
                    var result = await connection.QueryAsync<T>(sql);
                    cancellationToken.ThrowIfCancellationRequested();
                    return result;
                }
            }
            catch (OperationCanceledException exception)
            {
                cancellationTokenSource.Cancel();
                throw new DatabaseConnectionException($"Database operation did not complete within the allowed time of {commandTimeout} seconds.", exception);
            }
            catch (Exception exception)
            {
                throw new DatabaseConnectionException($"Exception encountered while attempting database operation.", exception);
            }
        }
    }
}
