using Microsoft.Extensions.Configuration;
using MyGeotabAPIAdapter.Configuration;
using System;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// Unit tests for <see cref="IConfigurationHelper"/> implementations.
    /// </summary>
    public class ConfigurationHelperTests
    {
        readonly IConfiguration configuration;
        readonly IConfigurationHelper configurationHelper;

        public ConfigurationHelperTests()
        {
            //Initialise configuration test environment.
            string projectPath = AppDomain.CurrentDomain.BaseDirectory.Split(new String[] { @"bin\" }, StringSplitOptions.None)[0];
            configuration = new ConfigurationBuilder()
               .SetBasePath(projectPath)
               .AddJsonFile("appsettingsTest.json")
               .Build();

            // Initialize ConfigurationHelper.
            configurationHelper = new ConfigurationHelper(configuration);

            // Initialize other objects that are required but not actually used in these tests.
        }

        [Fact]
        public void GetConfigKeyValueBoolean_MethodTests()
        {
            //Arrange
            bool expected = true;

            //Act
            string keyString = "AppSettings:Feeds:LogRecord:EnableLogRecordFeed";
            string sectionString = "";
            bool actual = configurationHelper.GetConfigKeyValueBoolean(keyString, sectionString);

            //Assert
            Assert.Equal(actual, expected);
            Assert.Throws<Exception>(() => configurationHelper.GetConfigKeyValueBoolean("AppSettings:TestEmptyString"));
        }

        [Fact]
        public void GetConfigKeyValueDateTime_MethodTests()
        {
            //Arrange
            DateTime.TryParse("2020-06-23T06:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime expected);

            //Act
            string keyString = "AppSettings:GeneralFeedSettings:FeedStartSpecificTimeUTC";
            string sectionString = "";
            DateTime actual = configurationHelper.GetConfigKeyValueDateTime(keyString, sectionString);

            //Assert
            Assert.Equal(actual, expected);
            Assert.Throws<Exception>(() => configurationHelper.GetConfigKeyValueDateTime("AppSettings:TestEmptyDateTime"));
        }

        [Fact]
        public void GetConfigKeyValueInt_MethodTests()
        {
            //Arrange
            int expected = 10;

            //Act
            string keyString = "AppSettings:Caches:Device:DeviceCacheUpdateIntervalMinutes";
            string sectionString = "";
            int actual = configurationHelper.GetConfigKeyValueInt(keyString, sectionString);

            //Assert
            Assert.Equal(actual, expected);
            Assert.Throws<Exception>(() => configurationHelper.GetConfigKeyValueInt("AppSettings:TestEmptyInt"));
        }

        [Fact]
        public void GetConfigKeyValueString_MethodTests()
        {
            //Arrange
            string expected = "SQLServer";

            //Act
            string keyString = "DatabaseSettings:DatabaseProviderType";
            string sectionString = "";
            string actual = configurationHelper.GetConfigKeyValueString(keyString, sectionString);

            //Assert
            Assert.Equal(actual, expected);
            Assert.Throws<Exception>(() => configurationHelper.GetConfigKeyValueString("AppSettings:TestEmptyString"));
        }

        [Fact]
        public void GetConfigKeyValueBooleanOrDefault_ReturnsConfiguredValueWhenPresent()
        {
            //Act
            bool actual = configurationHelper.GetConfigKeyValueBooleanOrDefault("AppSettings:Feeds:LogRecord:EnableLogRecordFeed", false);

            //Assert
            Assert.True(actual);
        }

        [Fact]
        public void GetConfigKeyValueBooleanOrDefault_ReturnsDefaultWhenAbsent()
        {
            //Act
            bool actualWhenDefaultIsTrue = configurationHelper.GetConfigKeyValueBooleanOrDefault("AppSettings:ThisKeyDoesNotExist", true);
            bool actualWhenDefaultIsFalse = configurationHelper.GetConfigKeyValueBooleanOrDefault("AppSettings:ThisKeyDoesNotExist", false);

            //Assert
            Assert.True(actualWhenDefaultIsTrue);
            Assert.False(actualWhenDefaultIsFalse);
        }

        [Fact]
        public void GetConfigKeyValueBooleanOrDefault_ThrowsWhenPresentButInvalid()
        {
            //Assert - an absent key is tolerated; a key whose value cannot be parsed is still an error, because
            //silently substituting a default for a typo would hide the typo.
            Assert.Throws<Exception>(() => configurationHelper.GetConfigKeyValueBooleanOrDefault("DatabaseSettings:DatabaseProviderType", true));
        }

        [Fact]
        public void GetConfigKeyValueBooleanOrDefault_ThrowsWhenPresentButEmpty()
        {
            //Assert - an empty value is present, not absent, so it is refused like any other value that cannot be
            //parsed. Taking the default for it would, for a setting whose default deletes data, do the opposite of what
            //an operator who blanked the setting most likely meant.
            Assert.Throws<Exception>(() => configurationHelper.GetConfigKeyValueBooleanOrDefault("AppSettings:TestEmptyValue", true));
        }

        [Fact]
        public void GetConfigKeyValueIntOrDefault_ThrowsWhenPresentButEmpty()
        {
            //Assert - as for the Boolean accessor.
            Assert.Throws<Exception>(() => configurationHelper.GetConfigKeyValueIntOrDefault("AppSettings:TestEmptyValue", 42));
        }

        [Fact]
        public void GetConfigKeyValueIntOrDefault_ReturnsConfiguredValueWhenPresent()
        {
            //Act
            int actual = configurationHelper.GetConfigKeyValueIntOrDefault("AppSettings:Caches:Device:DeviceCacheUpdateIntervalMinutes", 99);

            //Assert
            Assert.Equal(10, actual);
        }

        [Fact]
        public void GetConfigKeyValueIntOrDefault_ReturnsDefaultWhenAbsent()
        {
            //Act
            int actual = configurationHelper.GetConfigKeyValueIntOrDefault("AppSettings:ThisKeyDoesNotExist", 42);

            //Assert
            Assert.Equal(42, actual);
        }

        [Fact]
        public void GetConfigKeyValueIntOrDefault_ReturnsDefaultWhenConfiguredValueIsOutOfRange()
        {
            //Act - the configured value is 10, which falls outside the 100-200 range, so the default is used.
            //As with the existing accessor, the default must itself be inside the range.
            int actual = configurationHelper.GetConfigKeyValueIntOrDefault("AppSettings:Caches:Device:DeviceCacheUpdateIntervalMinutes", 150, "", false, 100, 200);

            //Assert
            Assert.Equal(150, actual);
        }

        [Fact]
        public void GetConfigKeyValueStringOrDefault_ReturnsConfiguredValueWhenPresent()
        {
            //Act
            string actual = configurationHelper.GetConfigKeyValueStringOrDefault("DatabaseSettings:DatabaseProviderType", "PostgreSQL");

            //Assert
            Assert.Equal("SQLServer", actual);
        }

        [Fact]
        public void GetConfigKeyValueStringOrDefault_ReturnsDefaultWhenAbsent()
        {
            //Act
            string actual = configurationHelper.GetConfigKeyValueStringOrDefault("AppSettings:ThisKeyDoesNotExist", "Vin");

            //Assert
            Assert.Equal("Vin", actual);
        }
    }
}
