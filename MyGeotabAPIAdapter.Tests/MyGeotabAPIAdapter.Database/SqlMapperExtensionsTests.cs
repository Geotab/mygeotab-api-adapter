using Dapper.Contrib.Extensions;
using MyGeotabAPIAdapter.Database.DataAccess;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// Test data for <see cref="SqlMapperExtensionsTests.FormatTableNameForSql_SimpleTableName_ReturnsQuotedTableName"/>.
    /// </summary>
    public class FormatTableNameForSql_SimpleTableName_TestData : TheoryData<string, string, string>
    {
        public FormatTableNameForSql_SimpleTableName_TestData()
        {
            // (tableName, databaseType, expectedResult)
            // PostgreSQL - uses double quotes
            Add("OServiceTracking", "npgsqlconnection", "\"OServiceTracking\"");
            Add("OServiceTracking", "npgsql", "\"OServiceTracking\"");
            
            // SQL Server - uses brackets
            Add("OServiceTracking", "sqlconnection", "[OServiceTracking]");
            Add("OServiceTracking", "sqlceconnection", "[OServiceTracking]");
            Add("OServiceTracking", "microsoft.data.sqlclient", "[OServiceTracking]");
            Add("OServiceTracking", "system.data.sqlclient", "[OServiceTracking]");
            
            // MySQL - uses backticks
            Add("OServiceTracking", "mysqlconnection", "`OServiceTracking`");
            
            // Oracle - uses double quotes
            Add("OServiceTracking", "oracleconnection", "\"OServiceTracking\"");
            Add("OServiceTracking", "oracle.manageddataaccess.client", "\"OServiceTracking\"");
            
            // Firebird - uses double quotes
            Add("OServiceTracking", "fbconnection", "\"OServiceTracking\"");
            
            // Unknown/default - uses double quotes (ANSI SQL standard)
            Add("OServiceTracking", "unknownconnection", "\"OServiceTracking\"");
            Add("OServiceTracking", null, "\"OServiceTracking\"");
        }
    }

    /// <summary>
    /// Test data for <see cref="SqlMapperExtensionsTests.FormatTableNameForSql_SchemaQualifiedTableName_ReturnsProperlyQuotedSchemaAndTable"/>.
    /// </summary>
    public class FormatTableNameForSql_SchemaQualifiedTableName_TestData : TheoryData<string, string, string>
    {
        public FormatTableNameForSql_SchemaQualifiedTableName_TestData()
        {
            // (tableName, databaseType, expectedResult)
            // PostgreSQL - uses double quotes
            Add("gda.OServiceTracking", "npgsqlconnection", "\"gda\".\"OServiceTracking\"");
            Add("public.Devices", "npgsql", "\"public\".\"Devices\"");
            
            // SQL Server - uses brackets
            Add("gda.OServiceTracking", "sqlconnection", "[gda].[OServiceTracking]");
            Add("dbo.Users", "sqlconnection", "[dbo].[Users]");
            Add("gda.OServiceTracking", "microsoft.data.sqlclient", "[gda].[OServiceTracking]");
            
            // MySQL - uses backticks
            Add("gda.OServiceTracking", "mysqlconnection", "`gda`.`OServiceTracking`");
            
            // Oracle - uses double quotes
            Add("gda.OServiceTracking", "oracleconnection", "\"gda\".\"OServiceTracking\"");
            Add("GDA.OSERVICETRACKING", "oracle.manageddataaccess.client", "\"GDA\".\"OSERVICETRACKING\"");
            
            // Firebird - uses double quotes
            Add("gda.OServiceTracking", "fbconnection", "\"gda\".\"OServiceTracking\"");
        }
    }

    /// <summary>
    /// Test data for edge cases in <see cref="SqlMapperExtensionsTests.FormatTableNameForSql_EdgeCases_HandlesCorrectly"/>.
    /// </summary>
    public class FormatTableNameForSql_EdgeCases_TestData : TheoryData<string, string, string>
    {
        public FormatTableNameForSql_EdgeCases_TestData()
        {
            // (tableName, databaseType, expectedResult)
            // Empty string
            Add("", "npgsqlconnection", "");
            Add("", "sqlconnection", "");
            
            // Null
            Add(null!, "npgsqlconnection", null!);
            Add(null!, "sqlconnection", null!);
            
            // Table name starting with dot (edge case - treated as simple table name)
            Add(".OServiceTracking", "npgsqlconnection", "\".OServiceTracking\"");
            
            // Table name ending with dot (edge case - treated as simple table name)
            Add("OServiceTracking.", "npgsqlconnection", "\"OServiceTracking.\"");
            
            // Multiple dots (only first dot is used for schema split)
            Add("schema.sub.table", "npgsqlconnection", "\"schema\".\"sub.table\"");
            Add("schema.sub.table", "sqlconnection", "[schema].[sub.table]");
        }
    }

    /// <summary>
    /// Unit tests for <see cref="SqlMapperExtensions"/> methods.
    /// </summary>
    public class SqlMapperExtensionsTests
    {
        /// <summary>
        /// Tests that FormatTableNameForSql correctly quotes simple table names for different database types.
        /// </summary>
        [Theory]
        [ClassData(typeof(FormatTableNameForSql_SimpleTableName_TestData))]
        public void FormatTableNameForSql_SimpleTableName_ReturnsQuotedTableName(string tableName, string databaseType, string expectedResult)
        {
            // Act
            var result = SqlMapperExtensions.FormatTableNameForSql(tableName, databaseType);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        /// <summary>
        /// Tests that FormatTableNameForSql correctly quotes schema-qualified table names for different database types.
        /// </summary>
        [Theory]
        [ClassData(typeof(FormatTableNameForSql_SchemaQualifiedTableName_TestData))]
        public void FormatTableNameForSql_SchemaQualifiedTableName_ReturnsProperlyQuotedSchemaAndTable(string tableName, string databaseType, string expectedResult)
        {
            // Act
            var result = SqlMapperExtensions.FormatTableNameForSql(tableName, databaseType);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        /// <summary>
        /// Tests that FormatTableNameForSql handles edge cases correctly.
        /// </summary>
        [Theory]
        [ClassData(typeof(FormatTableNameForSql_EdgeCases_TestData))]
        public void FormatTableNameForSql_EdgeCases_HandlesCorrectly(string tableName, string databaseType, string expectedResult)
        {
            // Act
            var result = SqlMapperExtensions.FormatTableNameForSql(tableName, databaseType);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        /// <summary>
        /// Tests that FormatTableNameForSql uses default database type (npgsqlconnection) when called without databaseType parameter.
        /// </summary>
        [Fact]
        public void FormatTableNameForSql_NoDbTypeProvided_UsesDefaultPostgresQuoting()
        {
            // Arrange
            var tableName = "OServiceTracking";
            var schemaQualifiedTableName = "gda.OServiceTracking";

            // Act
            var simpleResult = SqlMapperExtensions.FormatTableNameForSql(tableName);
            var schemaResult = SqlMapperExtensions.FormatTableNameForSql(schemaQualifiedTableName);

            // Assert
            Assert.Equal("\"OServiceTracking\"", simpleResult);
            Assert.Equal("\"gda\".\"OServiceTracking\"", schemaResult);
        }

        /// <summary>
        /// Tests that database type comparison is case-insensitive.
        /// </summary>
        [Theory]
        [InlineData("SQLCONNECTION", "[TestTable]")]
        [InlineData("SqlConnection", "[TestTable]")]
        [InlineData("sqlconnection", "[TestTable]")]
        [InlineData("NPGSQLCONNECTION", "\"TestTable\"")]
        [InlineData("NpgsqlConnection", "\"TestTable\"")]
        [InlineData("MYSQLCONNECTION", "`TestTable`")]
        [InlineData("MySqlConnection", "`TestTable`")]
        public void FormatTableNameForSql_DatabaseTypeCaseInsensitive_ReturnsCorrectQuoting(string databaseType, string expectedResult)
        {
            // Arrange
            var tableName = "TestTable";

            // Act
            var result = SqlMapperExtensions.FormatTableNameForSql(tableName, databaseType);

            // Assert
            Assert.Equal(expectedResult, result);
        }
    }
}