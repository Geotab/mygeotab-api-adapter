namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// Contains miscellaneous utility methods.
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// Possible values to be used in RecordStatus fields in database tables.
        /// </summary>
        public enum DatabaseRecordStatus { Active = 1, Deleted = 0}

        /// <summary>
        /// Database write operation types.
        /// </summary>
        public enum DatabaseWriteOperationType { None, BulkInsert, Insert, BulkUpdate, Update, BulkDelete, Delete}
    }
}
