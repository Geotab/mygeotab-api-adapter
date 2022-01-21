#nullable enable
using Dapper;
using MyGeotabAPIAdapter.Database.DataAccess;
using System;
using System.Globalization;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    public class GetSqlForGetAllAsyncTestData : TheoryData<bool, string, string, string, string?, string, int?, DateTime?>
    {
        const string ConnectionProviderTypeNpgsql = "Npgsql";
        const string ConnectionProviderTypeSQLite = "System.Data.SQLite";
        const string ConnectionProviderTypeMicrosoftSqlClient = "Microsoft.Data.SqlClient";
        const string ConnectionProviderTypeSystemSqlClient = "System.Data.SqlClient";
        const string ConnectionProviderTypeOracle = "Oracle.ManagedDataAccess.Client";

        public GetSqlForGetAllAsyncTestData()
        {
            var changedSince = DateTime.ParseExact("2021-11-04 17:37:25.729025", "yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture);
            var timeZoneOffset = TimeZoneInfo.Local.GetUtcOffset(changedSince);
            var resultsLimit = 10000;
            string expectedOutput = "";

            // *** VALID TESTS ***

            // resultsLimit null, changedSince null - all supported database types:
            expectedOutput = "select * from \"Devices\" order by \"id\"";
            Add(false, expectedOutput, "Devices", "id", "RecordLastChangedUtc", ConnectionProviderTypeNpgsql, null, null);
            Add(false, expectedOutput, "Devices", "id", "RecordLastChangedUtc", ConnectionProviderTypeSQLite, null, null);
            Add(false, expectedOutput, "Devices", "id", "RecordLastChangedUtc", ConnectionProviderTypeMicrosoftSqlClient, null, null);
            Add(false, expectedOutput, "Devices", "id", "RecordLastChangedUtc", ConnectionProviderTypeSystemSqlClient, null, null);
            Add(false, expectedOutput, "Devices", "id", "RecordLastChangedUtc", ConnectionProviderTypeOracle, null, null);

            // resultsLimit null, changedSince specified - all supported database types:
            expectedOutput = "select * from \"Devices\" where \"RecordLastChangedUtc\" > '2021-11-04 17:37:25.729025' order by \"id\"";
            Add(false, expectedOutput, "Devices", "id", "RecordLastChangedUtc", ConnectionProviderTypeNpgsql, null, changedSince);
            expectedOutput = $"select * from \"Devices\" where \"RecordLastChangedUtc\" > '2021-11-04 17:37:25.7290250{timeZoneOffset.Hours}' order by \"id\"";
            Add(false, expectedOutput, "Devices", "id", "RecordLastChangedUtc", ConnectionProviderTypeSQLite, null, changedSince);
            expectedOutput = "select * from \"Devices\" where \"RecordLastChangedUtc\" > '2021-11-04 17:37:25.7290250' order by \"id\"";
            Add(false, expectedOutput, "Devices", "id", "RecordLastChangedUtc", ConnectionProviderTypeMicrosoftSqlClient, null, changedSince);
            expectedOutput = "select * from \"Devices\" where \"RecordLastChangedUtc\" > '2021-11-04 17:37:25.7290250' order by \"id\"";
            Add(false, expectedOutput, "Devices", "id", "RecordLastChangedUtc", ConnectionProviderTypeSystemSqlClient, null, changedSince);
            expectedOutput = "select * from \"Devices\" where \"RecordLastChangedUtc\" > TIMESTAMP '2021-11-04 17:37:25.729025' order by \"id\"";
            Add(false, expectedOutput, "Devices", "id", "RecordLastChangedUtc", ConnectionProviderTypeOracle, null, changedSince);

            // resultsLimit specified, changedSince null - all supported database types:
            expectedOutput = "select * from \"Devices\" order by \"id\" limit 10000";
            Add(false, expectedOutput, "Devices", "id", "RecordLastChangedUtc", ConnectionProviderTypeNpgsql, resultsLimit, null);
            expectedOutput = "select * from \"Devices\" order by \"id\" limit 10000";
            Add(false, expectedOutput, "Devices", "id", "RecordLastChangedUtc", ConnectionProviderTypeSQLite, resultsLimit, null);
            expectedOutput = "select top (10000) * from \"Devices\" order by \"id\"";
            Add(false, expectedOutput, "Devices", "id", "RecordLastChangedUtc", ConnectionProviderTypeMicrosoftSqlClient, resultsLimit, null);
            expectedOutput = "select top (10000) * from \"Devices\" order by \"id\"";
            Add(false, expectedOutput, "Devices", "id", "RecordLastChangedUtc", ConnectionProviderTypeSystemSqlClient, resultsLimit, null);
            expectedOutput = "select * from (select * from \"Devices\" order by \"id\") where rownum <= 10000;";
            Add(false, expectedOutput, "Devices", "id", "RecordLastChangedUtc", ConnectionProviderTypeOracle, resultsLimit, null);

            // resultsLimit specified, changedSince specified - all supported database types:
            expectedOutput = "select * from \"Devices\" where \"RecordLastChangedUtc\" > '2021-11-04 17:37:25.729025' order by \"id\" limit 10000";
            Add(false, expectedOutput, "Devices", "id", "RecordLastChangedUtc", ConnectionProviderTypeNpgsql, resultsLimit, changedSince);
            expectedOutput = $"select * from \"Devices\" where \"RecordLastChangedUtc\" > '2021-11-04 17:37:25.7290250{timeZoneOffset.Hours}' order by \"id\" limit 10000";
            Add(false, expectedOutput, "Devices", "id", "RecordLastChangedUtc", ConnectionProviderTypeSQLite, resultsLimit, changedSince);
            expectedOutput = "select top (10000) * from \"Devices\" where \"RecordLastChangedUtc\" > '2021-11-04 17:37:25.7290250' order by \"id\"";
            Add(false, expectedOutput, "Devices", "id", "RecordLastChangedUtc", ConnectionProviderTypeMicrosoftSqlClient, resultsLimit, changedSince);
            expectedOutput = "select top (10000) * from \"Devices\" where \"RecordLastChangedUtc\" > '2021-11-04 17:37:25.7290250' order by \"id\"";
            Add(false, expectedOutput, "Devices", "id", "RecordLastChangedUtc", ConnectionProviderTypeSystemSqlClient, resultsLimit, changedSince);
            expectedOutput = "select * from (select * from \"Devices\" where \"RecordLastChangedUtc\" > TIMESTAMP '2021-11-04 17:37:25.729025' order by \"id\") where rownum <= 10000;";
            Add(false, expectedOutput, "Devices", "id", "RecordLastChangedUtc", ConnectionProviderTypeOracle, resultsLimit, changedSince);

            // Object type with different changedSinceColumnName:
            expectedOutput = "select * from \"LogRecords\" where \"RecordCreationTimeUtc\" > '2021-11-04 17:37:25.729025' order by \"id\"";
            Add(false, expectedOutput, "LogRecords", "id", "RecordCreationTimeUtc", ConnectionProviderTypeNpgsql, null, changedSince);


            // *** INVALID TESTS ***

            // Unsupported database type, changedSince specified:
            Add(true, "", "Devices", "id", "RecordLastChangedUtc", "SomeOtherDatabaseType", null, changedSince);
            // Unsupported database type, resultsLimit specified:
            Add(true, "", "Devices", "id", "RecordLastChangedUtc", "SomeOtherDatabaseType", resultsLimit, null);
        }
    }

    public class GetSqlForGetByParamAsyncTestData : TheoryData<bool, (string, DynamicParameters), string, string, dynamic, string?, string, int?, DateTime?>
    {
        const string ConnectionProviderTypeNpgsql = "Npgsql";
        const string ConnectionProviderTypeSQLite = "System.Data.SQLite";
        const string ConnectionProviderTypeMicrosoftSqlClient = "Microsoft.Data.SqlClient";
        const string ConnectionProviderTypeSystemSqlClient = "System.Data.SqlClient";
        const string ConnectionProviderTypeOracle = "Oracle.ManagedDataAccess.Client";

        public GetSqlForGetByParamAsyncTestData()
        {
            var changedSince = DateTime.ParseExact("2021-11-04 17:37:25.729025", "yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture);
            var timeZoneOffset = TimeZoneInfo.Local.GetUtcOffset(changedSince);
            var resultsLimit = 10000;
            dynamic dynamicParams = new { Id = 88, OtherParam = "SomeValue" };
            (string, DynamicParameters) expectedOutput = new ("", new DynamicParameters());

            // *** VALID TESTS ***

            // resultsLimit null, changedSince null - all supported database types:
            expectedOutput.Item1 = "select * from \"Devices\" where  Id = @Id and OtherParam = @OtherParam order by \"id\"";
            Add(false, expectedOutput, "Devices", "id", dynamicParams, "RecordLastChangedUtc", ConnectionProviderTypeNpgsql, null, null);
            expectedOutput.Item1 = "select * from \"Devices\" where  Id = @Id and OtherParam = @OtherParam order by \"id\"";
            Add(false, expectedOutput, "Devices", "id", dynamicParams, "RecordLastChangedUtc", ConnectionProviderTypeSQLite, null, null);
            expectedOutput.Item1 = "select * from \"Devices\" where  Id = @Id and OtherParam = @OtherParam order by \"id\"";
            Add(false, expectedOutput, "Devices", "id", dynamicParams, "RecordLastChangedUtc", ConnectionProviderTypeMicrosoftSqlClient, null, null);
            expectedOutput.Item1 = "select * from \"Devices\" where  Id = @Id and OtherParam = @OtherParam order by \"id\"";
            Add(false, expectedOutput, "Devices", "id", dynamicParams, "RecordLastChangedUtc", ConnectionProviderTypeSystemSqlClient, null, null);
            expectedOutput.Item1 = "select * from \"Devices\" where  Id = @Id and OtherParam = @OtherParam order by \"id\"";
            Add(false, expectedOutput, "Devices", "id", dynamicParams, "RecordLastChangedUtc", ConnectionProviderTypeOracle, null, null);

            // resultsLimit null, changedSince specified - all supported database types:
            expectedOutput.Item1 = "select * from \"Devices\" where \"RecordLastChangedUtc\" > '2021-11-04 17:37:25.729025' Id = @Id and OtherParam = @OtherParam order by \"id\"";
            Add(false, expectedOutput, "Devices", "id", dynamicParams, "RecordLastChangedUtc", ConnectionProviderTypeNpgsql, null, changedSince);
            expectedOutput.Item1 = $"select * from \"Devices\" where \"RecordLastChangedUtc\" > '2021-11-04 17:37:25.7290250{timeZoneOffset.Hours}' Id = @Id and OtherParam = @OtherParam order by \"id\"";
            Add(false, expectedOutput, "Devices", "id", dynamicParams, "RecordLastChangedUtc", ConnectionProviderTypeSQLite, null, changedSince);
            expectedOutput.Item1 = "select * from \"Devices\" where \"RecordLastChangedUtc\" > '2021-11-04 17:37:25.7290250' Id = @Id and OtherParam = @OtherParam order by \"id\"";
            Add(false, expectedOutput, "Devices", "id", dynamicParams, "RecordLastChangedUtc", ConnectionProviderTypeMicrosoftSqlClient, null, changedSince);
            expectedOutput.Item1 = "select * from \"Devices\" where \"RecordLastChangedUtc\" > '2021-11-04 17:37:25.7290250' Id = @Id and OtherParam = @OtherParam order by \"id\"";
            Add(false, expectedOutput, "Devices", "id", dynamicParams, "RecordLastChangedUtc", ConnectionProviderTypeSystemSqlClient, null, changedSince);
            expectedOutput.Item1 = "select * from \"Devices\" where \"RecordLastChangedUtc\" > TIMESTAMP '2021-11-04 17:37:25.729025' Id = @Id and OtherParam = @OtherParam order by \"id\"";
            Add(false, expectedOutput, "Devices", "id", dynamicParams, "RecordLastChangedUtc", ConnectionProviderTypeOracle, null, changedSince);

            // resultsLimit specified, changedSince null - all supported database types:
            expectedOutput.Item1 = "select * from \"Devices\" where  Id = @Id and OtherParam = @OtherParam order by \"id\" limit 10000";
            Add(false, expectedOutput, "Devices", "id", dynamicParams, "RecordLastChangedUtc", ConnectionProviderTypeNpgsql, resultsLimit, null);
            expectedOutput.Item1 = "select * from \"Devices\" where  Id = @Id and OtherParam = @OtherParam order by \"id\" limit 10000";
            Add(false, expectedOutput, "Devices", "id", dynamicParams, "RecordLastChangedUtc", ConnectionProviderTypeSQLite, resultsLimit, null);
            expectedOutput.Item1 = "select top (10000) * from \"Devices\" where  Id = @Id and OtherParam = @OtherParam order by \"id\"";
            Add(false, expectedOutput, "Devices", "id", dynamicParams, "RecordLastChangedUtc", ConnectionProviderTypeMicrosoftSqlClient, resultsLimit, null);
            expectedOutput.Item1 = "select top (10000) * from \"Devices\" where  Id = @Id and OtherParam = @OtherParam order by \"id\"";
            Add(false, expectedOutput, "Devices", "id", dynamicParams, "RecordLastChangedUtc", ConnectionProviderTypeSystemSqlClient, resultsLimit, null);
            expectedOutput.Item1 = "select * from (select * from \"Devices\" where  Id = @Id and OtherParam = @OtherParam order by \"id\") where rownum <= 10000;";
            Add(false, expectedOutput, "Devices", "id", dynamicParams, "RecordLastChangedUtc", ConnectionProviderTypeOracle, resultsLimit, null);

            // resultsLimit specified, changedSince specified - all supported database types:
            expectedOutput.Item1 = "select * from \"Devices\" where \"RecordLastChangedUtc\" > '2021-11-04 17:37:25.729025' Id = @Id and OtherParam = @OtherParam order by \"id\" limit 10000";
            Add(false, expectedOutput, "Devices", "id", dynamicParams, "RecordLastChangedUtc", ConnectionProviderTypeNpgsql, resultsLimit, changedSince);
            expectedOutput.Item1 = $"select * from \"Devices\" where \"RecordLastChangedUtc\" > '2021-11-04 17:37:25.7290250{timeZoneOffset.Hours}' Id = @Id and OtherParam = @OtherParam order by \"id\" limit 10000";
            Add(false, expectedOutput, "Devices", "id", dynamicParams, "RecordLastChangedUtc", ConnectionProviderTypeSQLite, resultsLimit, changedSince);
            expectedOutput.Item1 = "select top (10000) * from \"Devices\" where \"RecordLastChangedUtc\" > '2021-11-04 17:37:25.7290250' Id = @Id and OtherParam = @OtherParam order by \"id\"";
            Add(false, expectedOutput, "Devices", "id", dynamicParams, "RecordLastChangedUtc", ConnectionProviderTypeMicrosoftSqlClient, resultsLimit, changedSince);
            expectedOutput.Item1 = "select top (10000) * from \"Devices\" where \"RecordLastChangedUtc\" > '2021-11-04 17:37:25.7290250' Id = @Id and OtherParam = @OtherParam order by \"id\"";
            Add(false, expectedOutput, "Devices", "id", dynamicParams, "RecordLastChangedUtc", ConnectionProviderTypeSystemSqlClient, resultsLimit, changedSince);
            expectedOutput.Item1 = "select * from (select * from \"Devices\" where \"RecordLastChangedUtc\" > TIMESTAMP '2021-11-04 17:37:25.729025' Id = @Id and OtherParam = @OtherParam order by \"id\") where rownum <= 10000;";
            Add(false, expectedOutput, "Devices", "id", dynamicParams, "RecordLastChangedUtc", ConnectionProviderTypeOracle, resultsLimit, changedSince);

            // Object type with different changedSinceColumnName:
            expectedOutput.Item1 = "select * from \"LogRecords\" where \"RecordCreationTimeUtc\" > '2021-11-04 17:37:25.729025' Id = @Id and OtherParam = @OtherParam order by \"id\"";
            Add(false, expectedOutput, "LogRecords", "id", dynamicParams, "RecordCreationTimeUtc", ConnectionProviderTypeNpgsql, null, changedSince);


            // *** INVALID TESTS ***

            expectedOutput = new("", new DynamicParameters());
            // Unsupported database type, changedSince specified:
            Add(true, expectedOutput, "Devices", "id", dynamicParams, "RecordLastChangedUtc", "SomeOtherDatabaseType", null, changedSince);
            // Unsupported database type, resultsLimit specified:
            Add(true, expectedOutput, "Devices", "id", dynamicParams, "RecordLastChangedUtc", "SomeOtherDatabaseType", resultsLimit, null);
        }
    }

    public class SqlMapperSqlBuilderTests
    {
        [Theory]
        [ClassData(typeof(GetSqlForGetAllAsyncTestData))]
        public void GetSqlForGetAllAsyncTests(bool shouldThrowException, string expectedOutput, string tableName, string keyColumnName, string? changedSinceColumnName, string connectionProviderType, int? resultsLimit = null, DateTime? changedSince = null)
        {
            if (shouldThrowException == true)
            {
                var exception = Record.Exception(() => SqlMapperSqlBuilder.GetSqlForGetAllAsync(tableName, keyColumnName, changedSinceColumnName, connectionProviderType, resultsLimit, changedSince));
                Assert.NotNull(exception);
            }
            else
            { 
                var result = SqlMapperSqlBuilder.GetSqlForGetAllAsync(tableName, keyColumnName, changedSinceColumnName, connectionProviderType, resultsLimit, changedSince);
                Assert.Equal(expectedOutput, result);
            }
        }

        [Theory]
        [ClassData(typeof(GetSqlForGetByParamAsyncTestData))]
        public void GetSqlForGetByParamAsyncTests(bool shouldThrowException, (string, DynamicParameters) expectedOutput, string tableName, string keyColumnName, dynamic parms, string? changedSinceColumnName, string connectionProviderType, int? resultsLimit = null, DateTime? changedSince = null)
        {
            if (shouldThrowException == true)
            {
                var exception = Record.Exception(() => SqlMapperSqlBuilder.GetSqlForGetByParamAsync(tableName, keyColumnName, parms, changedSinceColumnName, connectionProviderType, resultsLimit, changedSince));
                Assert.NotNull(exception);
            }
            else
            {
                (string, DynamicParameters) result = SqlMapperSqlBuilder.GetSqlForGetByParamAsync(tableName, keyColumnName, parms, changedSinceColumnName, connectionProviderType, resultsLimit, changedSince);
                Assert.Equal(expectedOutput.Item1, result.Item1);
            }
        }
    }
}
