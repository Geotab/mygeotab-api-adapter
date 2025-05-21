using Microsoft.Data.SqlClient;
using MyGeotabAPIAdapter.Database.Enums;
using Npgsql;
using System;
using System.Text.RegularExpressions;

namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// Helper class with methods relating to foreign key exceptions (PostgreSQL and SQL Server supported).
    /// </summary>
    public static class ForeignKeyExceptionHelper
    {
        // Optional: Compiled Regex for slight performance gain if called extremely frequently (unlikely for exceptions)
        private static readonly Regex SqlServerConstraintRegex =
            new Regex(@"constraint ""([^""]+)""", RegexOptions.Compiled);

        /// <summary>
        /// Checks if the exception is a foreign key violation.
        /// </summary>
        /// <param name="exception">The exception to check.</param>
        /// <returns><c>true</c> if the exception indicates a foreign key violation; otherwise, <c>false</c>.</returns>
        public static bool IsForeignKeyViolationException(Exception exception)
        {
            if (exception == null)
            {
                return false;
            }
            if (exception is PostgresException pgException)
            {
                return pgException.SqlState == PostgresErrorCodes.ForeignKeyViolation;
            }
            if (exception is SqlException sqlException)
            {
                return sqlException.Number == SqlServerErrorCodes.ForeignKeyViolation;
            }
            return false;
        }

        /// <summary>
        /// Retrieves the constraint name from the exception message if it's a known database exception type
        /// and the constraint name can be determined.
        /// </summary>
        /// <param name="exception">The exception to check.</param>
        /// <returns>The constraint name if found; otherwise, an empty string.</returns>
        public static string GetConstraintNameFromException(Exception exception)
        {
            if (exception == null)
            {
                return string.Empty;
            }
            if (exception is PostgresException pgException)
            {
                return pgException.ConstraintName ?? string.Empty;
            }
            if (exception is SqlException sqlException)
            {
                // Sadly, SQL Server requires parsing the message to find the constraint name.
                var match = SqlServerConstraintRegex.Match(sqlException.Message);
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value;
                }
            }
            return string.Empty;
        }
    }
}