using Microsoft.Extensions.Configuration;
using MyGeotabAPIAdapter.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// Unit tests covering how <see cref="AdapterConfiguration"/> reads the settings whose value must name a
    /// member of an enum.
    /// <para>
    /// These settings were read by switching over the raw configuration string against nameof() labels, which
    /// made each of them case-sensitive while the settings surrounding them are not. The documentation says
    /// nothing about the distinction, so a value differing only in case looked like a working configuration and
    /// stopped the application from starting.
    /// </para>
    /// <para>
    /// The tests build on the appsettings.json the application actually ships with and vary one setting from it,
    /// so each case is a real configuration file differing from a working one in exactly the axis under test.
    /// The shipped file is used in preference to this project's appsettingsTest.json because the latter carries
    /// only the keys the tests that read it happen to name, and so is not a configuration the application can be
    /// started from; building from the shipped file makes these tests a check that it still can be.
    /// </para>
    /// </summary>
    public class AdapterConfigurationTests
    {
        const string ShippedAppSettingsFileName = "appsettings.json";
        const string ArgNameFeedStartOption = "AppSettings:GeneralFeedSettings:FeedStartOption";
        const string ArgNameFeedStartSpecificTimeUTC = "AppSettings:GeneralFeedSettings:FeedStartSpecificTimeUTC";
        const string ArgNameStatusDataLocationServiceOperationMode = "AppSettings:DataEnhancementServices:StatusData:StatusDataLocationServiceOperationMode";
        const string ArgNameFaultDataLocationServiceOperationMode = "AppSettings:DataEnhancementServices:FaultData:FaultDataLocationServiceOperationMode";
        const string ArgNameLevel2DatabaseMaintenanceWindowStartTimeUTC = "DatabaseSettings:Level2DatabaseMaintenanceWindowStartTimeUTC";

        /// <summary>
        /// Builds an <see cref="AdapterConfiguration"/> from the shipped configuration file, with the supplied
        /// settings applied over the top of it.
        /// </summary>
        static AdapterConfiguration BuildConfiguration(Dictionary<string, string?>? overriddenSettings = null)
        {
            // The file provider behind AddJsonFile refuses a path that climbs out of its base path, so the base
            // path is the adapter project rather than this one.
            // The test project's folder is found by walking up from the output directory. Splitting that path on "bin\"
            // found it on Windows only: elsewhere the separator is "/", nothing was split off, and the adapter project was
            // looked for inside the output directory.
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && File.Exists(Path.Combine(directory.FullName, "MyGeotabAPIAdapter.Tests.csproj")) == false)
            {
                directory = directory.Parent;
            }
            string testProjectPath = directory?.FullName
                ?? throw new InvalidOperationException($"No folder containing MyGeotabAPIAdapter.Tests.csproj was found above '{AppContext.BaseDirectory}'.");
            string adapterProjectPath = Path.GetFullPath(Path.Combine(testProjectPath, "..", "MyGeotabAPIAdapter"));
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(adapterProjectPath)
                .AddJsonFile(ShippedAppSettingsFileName);
            if (overriddenSettings != null)
            {
                configurationBuilder.AddInMemoryCollection(overriddenSettings);
            }
            return new AdapterConfiguration(new ConfigurationHelper(configurationBuilder.Build()));
        }

        static Dictionary<string, string?> Override(string keyString, string? valueString)
        {
            return new Dictionary<string, string?> { { keyString, valueString } };
        }

        [Theory]
        [InlineData("2024-01-01T06:00:00+03:00", 3, 0)]
        [InlineData("2024-01-01T04:15:00", 4, 15)]
        [InlineData("2020-06-23T06:00:00Z", 6, 0)]
        public void Level2WindowStartTime_IsAlwaysUtc_WhateverTheHostTimeZone(string configured, int hour, int minute)
        {
            // 06:00 at UTC+03:00 is 03:00 UTC, and a value with no offset is taken to be UTC. Read as a round trip, the first
            // became the HOST's local time (Kind Local) - so on a host not running UTC the window moved by the host's offset.
            var adapterConfiguration = BuildConfiguration(Override(ArgNameLevel2DatabaseMaintenanceWindowStartTimeUTC, configured));

            Assert.Equal(DateTimeKind.Utc, adapterConfiguration.Level2DatabaseMaintenanceWindowStartTimeUTC.Kind);
            Assert.Equal(new TimeSpan(hour, minute, 0), adapterConfiguration.Level2DatabaseMaintenanceWindowStartTimeUTC.TimeOfDay);
        }

        [Fact]
        public void TheShippedConfigurationFile_IsAccepted()
        {
            var adapterConfiguration = BuildConfiguration();

            Assert.Equal(FeedStartOption.CurrentTime, adapterConfiguration.FeedStartOption);
            Assert.Equal(OperationMode.Continuous, adapterConfiguration.StatusDataLocationServiceOperationMode);
            Assert.Equal(OperationMode.Continuous, adapterConfiguration.FaultDataLocationServiceOperationMode);
        }

        [Theory]
        [InlineData("currenttime", FeedStartOption.CurrentTime)]
        [InlineData("CURRENTTIME", FeedStartOption.CurrentTime)]
        [InlineData("feedversion", FeedStartOption.FeedVersion)]
        [InlineData("FEEDVERSION", FeedStartOption.FeedVersion)]
        [InlineData("  CurrentTime  ", FeedStartOption.CurrentTime)]
        public void FeedStartOption_DifferingOnlyInCaseOrSpacing_IsAccepted(string configItemValue, FeedStartOption expected)
        {
            var adapterConfiguration = BuildConfiguration(Override(ArgNameFeedStartOption, configItemValue));

            Assert.Equal(expected, adapterConfiguration.FeedStartOption);
        }

        [Fact]
        public void FeedStartOption_SpecificTimeInLowerCase_IsAcceptedAndStillReadsItsCompanionSetting()
        {
            // SpecificTime is the one member with follow-on work, so it is checked separately: accepting the
            // value is only half of it if the companion setting stops being read.
            var adapterConfiguration = BuildConfiguration(new Dictionary<string, string?>
            {
                { ArgNameFeedStartOption, "specifictime" },
                { ArgNameFeedStartSpecificTimeUTC, "2021-03-04T05:06:07Z" }
            });

            Assert.Equal(FeedStartOption.SpecificTime, adapterConfiguration.FeedStartOption);
            Assert.Equal(new DateTime(2021, 3, 4, 5, 6, 7, DateTimeKind.Utc), adapterConfiguration.FeedStartSpecificTimeUTC.ToUniversalTime());
        }

        [Theory]
        [InlineData("Whenever")]
        [InlineData("Current Time")]
        public void FeedStartOption_NamingNoMember_IsRefusedAndTheMessageNamesTheKeyTheValueAndTheAlternatives(string configItemValue)
        {
            var exception = Assert.Throws<Exception>(() => BuildConfiguration(Override(ArgNameFeedStartOption, configItemValue)));

            Assert.Contains(ArgNameFeedStartOption, exception.Message);
            Assert.Contains(configItemValue, exception.Message);
            Assert.Contains(nameof(FeedStartOption.CurrentTime), exception.Message);
            Assert.Contains(nameof(FeedStartOption.SpecificTime), exception.Message);
            Assert.Contains(nameof(FeedStartOption.FeedVersion), exception.Message);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("99")]
        [InlineData("CurrentTime, FeedVersion")]
        public void FeedStartOption_GivenANumberOrAList_IsStillRefused(string configItemValue)
        {
            // Reading this setting through Enum.TryParse rather than a switch over the raw string is what makes
            // it case-insensitive; on its own it would also start accepting these, which the switch refused.
            var exception = Assert.Throws<Exception>(() => BuildConfiguration(Override(ArgNameFeedStartOption, configItemValue)));

            Assert.Contains(ArgNameFeedStartOption, exception.Message);
        }

        [Theory]
        [InlineData(ArgNameStatusDataLocationServiceOperationMode)]
        [InlineData(ArgNameFaultDataLocationServiceOperationMode)]
        public void LocationServiceOperationMode_DifferingOnlyInCase_IsAccepted(string keyString)
        {
            var adapterConfiguration = BuildConfiguration(Override(keyString, "continuous"));

            var actual = keyString == ArgNameStatusDataLocationServiceOperationMode
                ? adapterConfiguration.StatusDataLocationServiceOperationMode
                : adapterConfiguration.FaultDataLocationServiceOperationMode;
            Assert.Equal(OperationMode.Continuous, actual);
        }

        [Theory]
        [InlineData(ArgNameStatusDataLocationServiceOperationMode)]
        [InlineData(ArgNameFaultDataLocationServiceOperationMode)]
        public void LocationServiceOperationMode_ScheduledInLowerCase_IsAcceptedAndStillReadsItsCompanionSettings(string keyString)
        {
            var adapterConfiguration = BuildConfiguration(Override(keyString, "scheduled"));

            if (keyString == ArgNameStatusDataLocationServiceOperationMode)
            {
                Assert.Equal(OperationMode.Scheduled, adapterConfiguration.StatusDataLocationServiceOperationMode);
                Assert.NotEqual(default, adapterConfiguration.StatusDataLocationServiceDailyStartTimeUTC);
                Assert.NotEqual(0, adapterConfiguration.StatusDataLocationServiceDailyRunTimeSeconds);
            }
            else
            {
                Assert.Equal(OperationMode.Scheduled, adapterConfiguration.FaultDataLocationServiceOperationMode);
                Assert.NotEqual(default, adapterConfiguration.FaultDataLocationServiceDailyStartTimeUTC);
                Assert.NotEqual(0, adapterConfiguration.FaultDataLocationServiceDailyRunTimeSeconds);
            }
        }

        [Theory]
        [InlineData(ArgNameStatusDataLocationServiceOperationMode, "Occasional")]
        [InlineData(ArgNameStatusDataLocationServiceOperationMode, "0")]
        [InlineData(ArgNameStatusDataLocationServiceOperationMode, "Continuous, Scheduled")]
        [InlineData(ArgNameFaultDataLocationServiceOperationMode, "Occasional")]
        [InlineData(ArgNameFaultDataLocationServiceOperationMode, "0")]
        [InlineData(ArgNameFaultDataLocationServiceOperationMode, "Continuous, Scheduled")]
        public void LocationServiceOperationMode_NamingNoMember_IsRefusedAndTheMessageNamesTheKeyAndTheValue(string keyString, string configItemValue)
        {
            var exception = Assert.Throws<Exception>(() => BuildConfiguration(Override(keyString, configItemValue)));

            Assert.Contains(keyString, exception.Message);
            Assert.Contains(configItemValue, exception.Message);
            Assert.Contains(nameof(OperationMode.Continuous), exception.Message);
            Assert.Contains(nameof(OperationMode.Scheduled), exception.Message);
        }
    }
}
