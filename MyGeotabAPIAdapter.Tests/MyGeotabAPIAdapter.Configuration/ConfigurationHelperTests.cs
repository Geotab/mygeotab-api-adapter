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
            DateTime.TryParse("2020-06-23T08:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime expected);

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
    }
}
