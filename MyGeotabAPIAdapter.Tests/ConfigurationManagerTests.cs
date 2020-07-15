using Microsoft.Extensions.Configuration;
using Moq;
using MyGeotabAPIAdapter;
using System;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    public class ConfigurationManagerTests
    {
        IConfiguration configuration;
        ConfigurationManager configurationManager = new ConfigurationManager();

        public ConfigurationManagerTests()
        {
            //Initialise configuration test environment
            string projectPath = AppDomain.CurrentDomain.BaseDirectory.Split(new String[] { @"bin\" }, StringSplitOptions.None)[0];
            configuration = new ConfigurationBuilder()
               .SetBasePath(projectPath)
               .AddJsonFile("appsettingsTest.json")
               .Build();
            configurationManager.Configuration = configuration;
        }

        [Fact]
        public void GetConfigKeyValueString_SimpleValuesShouldCalculate()
        {
            //Arrange
            string expected = "SQLite";

            //Act
            string keyString = "DatabaseSettings:DatabaseProviderType";
            string sectionString = "";
            string actual = configurationManager.GetConfigKeyValueString(keyString, sectionString);

            //Assert
            Assert.Equal(actual, expected);
            Assert.Throws<Exception>(() => configurationManager.GetConfigKeyValueString("AppSettings:TestEmptyString"));
        }

        [Fact]
        public void GetConfigKeyValueString_CheckSection()
        {
            //Arrange
            string expected = "SQLite";

            //Act
            string keyString = "DatabaseProviderType";
            string sectionString = "DatabaseSettings";
            string actual = configurationManager.GetConfigKeyValueString(keyString, sectionString);

            //Assert
            Assert.Equal(actual, expected);
        }

        [Fact]
        public void GetConfigKeyValueInt_SimpleValuesShouldCalculate()
        {
            //Arrange
            int expected = 300;

            //Act
            string keyString = "AppSettings:DeviceCacheUpdateIntervalMinutes";
            string sectionString = "";
            int actual = configurationManager.GetConfigKeyValueInt(keyString, sectionString);

            //Assert
            Assert.Equal(actual, expected);
            Assert.Throws<Exception>(() => configurationManager.GetConfigKeyValueInt("AppSettings:TestEmptyInt"));
        }

        [Fact]
        public void GetConfigKeyValueBoolean_SimpleValuesShouldCalculate()
        {
            //Arrange
            bool expected = true;

            //Act
            string keyString = "AppSettings:EnableLogRecordFeed";
            string sectionString = "";
            bool actual = configurationManager.GetConfigKeyValueBoolean(keyString, sectionString);

            //Assert
            Assert.Equal(actual, expected);
            Assert.Throws<Exception>(() => configurationManager.GetConfigKeyValueBoolean("AppSettings:TestEmptyString"));
        }
    }
}
