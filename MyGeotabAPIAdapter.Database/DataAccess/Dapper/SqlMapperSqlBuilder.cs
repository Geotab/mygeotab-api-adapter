#nullable enable
using Dapper;
using System;
using System.Text;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A helper class designed to assist in SQL statement generation for methods in the SqlMapperExtensions class.
    /// </summary>
    public static class SqlMapperSqlBuilder
    {
        const string ConnectionProviderTypeNpgsql = "Npgsql";
        const string ConnectionProviderTypeSQLite = "System.Data.SQLite";
        const string ConnectionProviderTypeMicrosoftSqlClient = "Microsoft.Data.SqlClient";
        const string ConnectionProviderTypeSystemSqlClient = "System.Data.SqlClient";
        const string ConnectionProviderTypeOracle = "Oracle.ManagedDataAccess.Client";

        /// <summary>
        /// Generates a SQL statement using the input parameter values. SQL statements are tailored to all supported database types.
        /// </summary>
        /// <param name="tableName">The name of the database table to select from.</param>
        /// <param name="keyColumnName">The name of the key column in the database table on which to order results by.</param>
        /// <param name="changedSinceColumnName">OPTIONAL - The name of the database column containing the data timestamp. If provided and the see<paramref name="changedSince"/> parameter is set to <c>true</c>, only records where this column value is greater than the see<paramref name="changedSince"/> parameter value will be selected.</param>
        /// <param name="connectionProviderType">The connection provider type (database type).</param>
        /// <param name="resultsLimit">OPTIONAL - The maximum number of records to return. If <c>null</c>, all records matching the search criteria will be returned.</param>
        /// <param name="changedSince">OPTIONAL - Only used if the <paramref name="changedSinceColumnName"/> is provided. Limits selected results to those where the <paramref name="changedSinceColumnName"/> column has a value greater than this <see cref="DateTime"/>.</param>
        /// <returns></returns>
        public static string GetSqlForGetAllAsync(string tableName, string keyColumnName, string? changedSinceColumnName, string connectionProviderType, int? resultsLimit = null, DateTime? changedSince = null)
        {
            var whereChangedSincePortionOfSqlStatement = "";
            if (changedSince != null && changedSinceColumnName != null)
            {
                DateTime changedSinceDateTime = (DateTime)changedSince;
                whereChangedSincePortionOfSqlStatement = connectionProviderType switch
                {
                    ConnectionProviderTypeNpgsql =>
                        $" where \"{changedSinceColumnName}\" > \'{changedSinceDateTime:yyyy-MM-dd HH:mm:ss.ffffff}\'",
                    ConnectionProviderTypeSQLite =>
                        $" where \"{changedSinceColumnName}\" > \'{changedSinceDateTime:yyyy-MM-dd HH:mm:ss.fffffffz}\'",
                    ConnectionProviderTypeMicrosoftSqlClient or ConnectionProviderTypeSystemSqlClient =>
                        $" where \"{changedSinceColumnName}\" > \'{changedSinceDateTime:yyyy-MM-dd HH:mm:ss.fffffff}\'",
                    ConnectionProviderTypeOracle =>
                        $" where \"{changedSinceColumnName}\" > TIMESTAMP \'{changedSinceDateTime:yyyy-MM-dd HH:mm:ss.ffffff}\'",
                    _ => throw new NotImplementedException($"Support for the '{connectionProviderType}' connection provider type has not been implemented in this method."),
                };
            }

            string? sqlStatement;
            // If a resultsLimit is specified, apply the limit to the SQL statement based on the database provider type.
            if (resultsLimit == null || resultsLimit < 1)
            {
                sqlStatement = $"select * from \"{tableName}\"{whereChangedSincePortionOfSqlStatement} order by \"{keyColumnName}\"";
            }
            else
            {
                sqlStatement = connectionProviderType switch
                {
                    ConnectionProviderTypeNpgsql or ConnectionProviderTypeSQLite =>
                        $"select * from \"{tableName}\"{whereChangedSincePortionOfSqlStatement} order by \"{keyColumnName}\" limit {resultsLimit}",
                    ConnectionProviderTypeMicrosoftSqlClient or ConnectionProviderTypeSystemSqlClient =>
                        $"select top ({resultsLimit}) * from \"{tableName}\"{whereChangedSincePortionOfSqlStatement} order by \"{keyColumnName}\"",
                    ConnectionProviderTypeOracle =>
                        $"select * from (select * from \"{tableName}\"{whereChangedSincePortionOfSqlStatement} order by \"{keyColumnName}\") where rownum <= {resultsLimit};",
                    _ => throw new NotImplementedException($"Support for the '{connectionProviderType}' connection provider type has not been implemented in this method."),
                };
            }
            return sqlStatement;
        }

        /// <summary>
        /// Generates a SQL statement using the input parameter values. SQL statements are tailored to all supported database types.
        /// </summary>
        /// <param name="tableName">The name of the database table to select from.</param>
        /// <param name="keyColumnName">The name of the key column in the database table on which to order results by.</param>
        /// <param name="parms">A set of <see cref="DynamicParameters"/> to be included in the WHERE clause of the SQL statement. Note that only the AND operator will be used.</param>
        /// <param name="changedSinceColumnName">OPTIONAL - The name of the database column containing the data timestamp. If provided and the see<paramref name="changedSince"/> parameter is set to <c>true</c>, only records where this column value is greater than the see<paramref name="changedSince"/> parameter value will be selected.</param>
        /// <param name="connectionProviderType">The connection provider type (database type).</param>
        /// <param name="resultsLimit">OPTIONAL - The maximum number of records to return. If <c>null</c>, all records matching the search criteria will be returned.</param>
        /// <param name="changedSince">OPTIONAL - Only used if the <paramref name="changedSinceColumnName"/> is provided. Limits selected results to those where the <paramref name="changedSinceColumnName"/> column has a value greater than this <see cref="DateTime"/>.</param>
        /// <returns></returns>
        public static (string sqlStatement, DynamicParameters dynParms) GetSqlForGetByParamAsync(string tableName, string keyColumnName, dynamic parms, string? changedSinceColumnName, string connectionProviderType, int? resultsLimit = null, DateTime? changedSince = null)
        {
            var whereChangedSincePortionOfSqlStatement = "where ";
            var sqlParameterChar = "@";

            if (changedSince != null && changedSinceColumnName != null)
            {
                DateTime changedSinceDateTime = (DateTime)changedSince;
                whereChangedSincePortionOfSqlStatement = connectionProviderType switch
                {
                    ConnectionProviderTypeNpgsql =>
                        $"where \"{changedSinceColumnName}\" > \'{changedSinceDateTime:yyyy-MM-dd HH:mm:ss.ffffff}\'",
                    ConnectionProviderTypeSQLite =>
                        $"where \"{changedSinceColumnName}\" > \'{changedSinceDateTime:yyyy-MM-dd HH:mm:ss.fffffffz}\'",
                    ConnectionProviderTypeMicrosoftSqlClient or ConnectionProviderTypeSystemSqlClient =>
                        $"where \"{changedSinceColumnName}\" > \'{changedSinceDateTime:yyyy-MM-dd HH:mm:ss.fffffff}\'",
                    ConnectionProviderTypeOracle =>
                        $"where \"{changedSinceColumnName}\" > TIMESTAMP \'{changedSinceDateTime:yyyy-MM-dd HH:mm:ss.ffffff}\'",
                    _ => throw new NotImplementedException($"Support for the '{connectionProviderType}' connection provider type has not been implemented in this method."),
                };
            }

            var sb = new StringBuilder();

            // If a resultsLimit is specified, apply the limit to the SQL statement based on the database provider type. For some providers, the resultsLimit needs to be added later.
            switch (connectionProviderType)
            {
                case ConnectionProviderTypeNpgsql:
                case ConnectionProviderTypeSQLite:
                    sb.Append($"select * from \"{tableName}\" {whereChangedSincePortionOfSqlStatement} ");
                    break;
                case ConnectionProviderTypeMicrosoftSqlClient:
                case ConnectionProviderTypeSystemSqlClient:
                    if (resultsLimit == null || resultsLimit < 1)
                    {
                        sb.Append($"select * from \"{tableName}\" {whereChangedSincePortionOfSqlStatement} ");
                    }
                    else
                    {
                        sb.Append($"select top ({resultsLimit}) * from \"{tableName}\" {whereChangedSincePortionOfSqlStatement} ");
                    }
                    break;
                case ConnectionProviderTypeOracle:
                    if (resultsLimit == null || resultsLimit < 1)
                    {
                        sb.Append($"select * from \"{tableName}\" {whereChangedSincePortionOfSqlStatement} ");
                    }
                    else
                    {
                        sb.Append($"select * from (select * from \"{tableName}\" {whereChangedSincePortionOfSqlStatement} ");
                    }
                    break;
                default:
                    throw new NotImplementedException($"Support for the '{connectionProviderType}' connection provider type has not been implemented in this method.");
            }

            var allProperties = parms.GetType().GetProperties();
            DynamicParameters dynParms = new();

            for (var i = 0; i < allProperties.Length; i++)
            {
                // Add to the SQL statement where clause based on dynamic parameters
                var property = allProperties[i];
                sb.AppendFormat("{0} = {1}{2}", property.Name, sqlParameterChar, property.Name);
                if (i < allProperties.Length - 1)
                    sb.AppendFormat(" and ");

                // Create the DynamicParameters
                dynParms.Add(String.Format("{0}{1}", sqlParameterChar, property.Name), property.GetValue(parms, null));
            }

            // Complete building the SQL statement.
            switch (connectionProviderType)
            {
                case ConnectionProviderTypeNpgsql:
                case ConnectionProviderTypeSQLite:
                    sb.Append($" order by \"{keyColumnName}\"");
                    if (resultsLimit != null && resultsLimit > 0)
                    {
                        sb.Append($" limit {resultsLimit}");
                    }
                    break;
                case ConnectionProviderTypeMicrosoftSqlClient:
                case ConnectionProviderTypeSystemSqlClient:
                    sb.Append($" order by \"{keyColumnName}\"");
                    break;
                case ConnectionProviderTypeOracle:
                    sb.Append($" order by \"{keyColumnName}\"");
                    if (resultsLimit != null && resultsLimit > 0)
                    {
                        sb.Append($") where rownum <= {resultsLimit};");
                    }
                    break;
                default:
                    throw new NotImplementedException($"Support for the '{connectionProviderType}' connection provider type has not been implemented in this method.");
            }

            string sqlStatement = sb.ToString();
            return new (sqlStatement, dynParms);
        }
    }
}
