using MyGeotabAPIAdapter.Database;
using System;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// Unit tests for <see cref="ConnectionInfo"/>'s handling of the DatabaseProviderType configuration value.
    /// <para>
    /// This type is shared by every application in the solution, so the value it is given here is the
    /// 'DatabaseSettings:DatabaseProviderType' setting of whichever one is starting. Each test below describes a
    /// misconfiguration that this constructor is the right place to diagnose, and that it previously either
    /// refused for the wrong reason, reported without the one fact the operator could act on, or accepted.
    /// </para>
    /// </summary>
    public class ConnectionInfoTests
    {
        const string TestConnectionString = "Server=test;Database=geotabadapterdb";
        const int TestTimeoutSecondsForDatabaseTasks = 600;

        static ConnectionInfo BuildConnectionInfo(string dataAccessProviderType)
        {
            return new ConnectionInfo(TestConnectionString, dataAccessProviderType, Databases.AdapterDatabase, TestTimeoutSecondsForDatabaseTasks);
        }

        [Theory]
        [InlineData("PostgreSQL", "Npgsql")]
        [InlineData("SQLServer", "System.Data.SqlClient")]
        public void Constructor_WithTheDocumentedValue_RegistersTheProviderFactory(string dataAccessProviderType, string expectedConnectionProviderType)
        {
            var connectionInfo = BuildConnectionInfo(dataAccessProviderType);

            Assert.Equal(expectedConnectionProviderType, connectionInfo.ConnectionProviderType);
            Assert.Equal(TestConnectionString, connectionInfo.ConnectionString);
            Assert.Equal(Databases.AdapterDatabase, connectionInfo.Database);
            Assert.Equal(TestTimeoutSecondsForDatabaseTasks, connectionInfo.TimeoutSecondsForDatabaseTasks);
        }

        [Theory]
        [InlineData("postgresql", ConnectionInfo.DataAccessProviderType.PostgreSQL)]
        [InlineData("POSTGRESQL", ConnectionInfo.DataAccessProviderType.PostgreSQL)]
        [InlineData("sqlserver", ConnectionInfo.DataAccessProviderType.SQLServer)]
        [InlineData("SQLSERVER", ConnectionInfo.DataAccessProviderType.SQLServer)]
        [InlineData("  PostgreSQL  ", ConnectionInfo.DataAccessProviderType.PostgreSQL)]
        public void Constructor_WithAValueDifferingOnlyInCaseOrSpacing_IsAccepted(string dataAccessProviderType, ConnectionInfo.DataAccessProviderType expected)
        {
            // Previously refused: the value was parsed case-sensitively, so a configuration file saying
            // 'postgresql' failed to start an application while one saying 'PostgreSQL' did not - and no
            // documentation told the operator that this setting, alone among the settings around it, was
            // case-sensitive.
            var connectionInfo = BuildConnectionInfo(dataAccessProviderType);

            Assert.Equal(expected, connectionInfo.ProviderType);
        }

        [Fact]
        public void Constructor_WithAnUnrecognizedValue_ReportsTheValueItWasGiven()
        {
            // Previously the message interpolated the connectionProviderType field, which is still null at the
            // point of the throw. The operator was told: Unsupported DataAccessProviderType ('').
            var exception = Assert.Throws<ArgumentException>(() => BuildConnectionInfo("Oracle"));

            Assert.Contains("Oracle", exception.Message);
        }

        [Fact]
        public void Constructor_WithAnUnrecognizedValue_ListsTheValuesItWouldAccept()
        {
            var exception = Assert.Throws<ArgumentException>(() => BuildConnectionInfo("Oracle"));

            Assert.Contains(nameof(ConnectionInfo.DataAccessProviderType.PostgreSQL), exception.Message);
            Assert.Contains(nameof(ConnectionInfo.DataAccessProviderType.SQLServer), exception.Message);
        }

        [Fact]
        public void Constructor_WithAnUnrecognizedValue_DoesNotOfferNullValueAsAnAlternative()
        {
            // NullValue is a member of the enum but is not a database provider, so advertising it in the list of
            // valid values would send the operator to a value that is refused a line later.
            var exception = Assert.Throws<ArgumentException>(() => BuildConnectionInfo("Oracle"));

            Assert.DoesNotContain(nameof(ConnectionInfo.DataAccessProviderType.NullValue), exception.Message);
        }

        [Fact]
        public void Constructor_WithNullValue_IsRefused()
        {
            // Previously accepted: NullValue fell through the switch to 'default: break;', so no provider factory
            // was registered, ConnectionProviderType stayed null, and the constructor returned successfully. The
            // failure surfaced later, somewhere else, as a different symptom.
            var exception = Assert.Throws<ArgumentException>(() => BuildConnectionInfo(nameof(ConnectionInfo.DataAccessProviderType.NullValue)));

            Assert.Contains(nameof(ConnectionInfo.DataAccessProviderType.NullValue), exception.Message);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("1")]
        [InlineData("2")]
        [InlineData("99")]
        public void Constructor_WithANumericValue_IsRefused(string dataAccessProviderType)
        {
            // '0' and '1' previously selected a provider, and '2' selected NullValue; '99' was cast straight
            // through and matched no arm of the switch. None of them is a value the configuration documentation
            // describes, and a number in this setting is a mistake rather than a shorthand.
            var exception = Assert.Throws<ArgumentException>(() => BuildConnectionInfo(dataAccessProviderType));

            Assert.Contains(dataAccessProviderType, exception.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithAnEmptyValue_IsRefused(string dataAccessProviderType)
        {
            Assert.Throws<ArgumentException>(() => BuildConnectionInfo(dataAccessProviderType));
        }

        [Fact]
        public void Constructor_WithACommaSeparatedList_IsRefused()
        {
            // Enum.TryParse ORs a comma-separated list into a single member even for an enum that carries no
            // FlagsAttribute, so this previously selected SQLServer without saying so.
            var exception = Assert.Throws<ArgumentException>(() => BuildConnectionInfo("PostgreSQL, SQLServer"));

            Assert.Contains("PostgreSQL, SQLServer", exception.Message);
        }
    }
}
