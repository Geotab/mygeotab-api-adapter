using Microsoft.Extensions.Configuration;
using System;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    public class ConfigurationManagerTests
    {
        readonly IConfiguration configuration;

        public ConfigurationManagerTests()
        {
            //Initialise configuration test environment
            string projectPath = AppDomain.CurrentDomain.BaseDirectory.Split(new String[] { @"bin\" }, StringSplitOptions.None)[0];
            configuration = new ConfigurationBuilder()
               .SetBasePath(projectPath)
               .AddJsonFile("appsettingsTest.json")
               .Build();
            ConfigurationManager.Configuration = configuration;
        }

        [Fact]
        public void GetConfigKeyValueString_SimpleValuesShouldCalculate()
        {
            //Arrange
            string expected = "SQLite";

            //Act
            string keyString = "DatabaseSettings:DatabaseProviderType";
            string sectionString = "";
            string actual = ConfigurationManager.GetConfigKeyValueString(keyString, sectionString);

            //Assert
            Assert.Equal(actual, expected);
            Assert.Throws<Exception>(() => ConfigurationManager.GetConfigKeyValueString("AppSettings:TestEmptyString"));
        }

        [Fact]
        public void GetConfigKeyValueString_CheckSection()
        {
            //Arrange
            string expected = "SQLite";

            //Act
            string keyString = "DatabaseProviderType";
            string sectionString = "DatabaseSettings";
            string actual = ConfigurationManager.GetConfigKeyValueString(keyString, sectionString);

            //Assert
            Assert.Equal(actual, expected);
        }

        [Fact]
        public void GetConfigKeyValueInt_SimpleValuesShouldCalculate()
        {
            //Arrange
            int expected = 10;

            //Act
            string keyString = "AppSettings:Caches:Device:DeviceCacheUpdateIntervalMinutes";
            string sectionString = "";
            int actual = ConfigurationManager.GetConfigKeyValueInt(keyString, sectionString);

            //Assert
            Assert.Equal(actual, expected);
            Assert.Throws<Exception>(() => ConfigurationManager.GetConfigKeyValueInt("AppSettings:TestEmptyInt"));
        }

        [Fact]
        public void GetConfigKeyValueBoolean_SimpleValuesShouldCalculate()
        {
            //Arrange
            bool expected = true;

            //Act
            string keyString = "AppSettings:Feeds:LogRecord:EnableLogRecordFeed";
            string sectionString = "";
            bool actual = ConfigurationManager.GetConfigKeyValueBoolean(keyString, sectionString);

            //Assert
            Assert.Equal(actual, expected);
            Assert.Throws<Exception>(() => ConfigurationManager.GetConfigKeyValueBoolean("AppSettings:TestEmptyString"));
        }
    }
}
