namespace MyGeotabAPIAdapter.Database.Enums
{
    /// <summary>
    /// Enumeration of SQL Server error codes. Expand this as needed.
    /// </summary>
    /// <remarks>
    /// See: <see href="https://learn.microsoft.com/en-us/sql/relational-databases/errors-events/database-engine-events-and-errors?view=sql-server-ver16">SQL Server Error Codes</see>
    /// </remarks>
    public static class SqlServerErrorCodes
    {
        public const int ForeignKeyViolation = 547;
        public const int PrimaryKeyViolation = 2601;
        public const int UniqueConstraintViolation = 2627;
    }
}
