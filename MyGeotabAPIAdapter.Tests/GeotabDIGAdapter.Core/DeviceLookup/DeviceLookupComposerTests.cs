using MyGeotabAPIAdapter.DeviceLookup;
using System;
using System.Collections.Generic;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// Unit tests for <see cref="IDeviceLookupComposer"/> implementations. These cover the rules that decide what
    /// happens when two device lookup sources disagree, which are the rules a hybrid deployment depends on and the
    /// only place in the transmit path where a mistake silently sends one vehicle's telemetry as another's.
    /// </summary>
    public class DeviceLookupComposerTests
    {
        const string ProvisionedDevicesSource = "ProvisionedDevices";
        const string DeviceSubscriptionsSource = "DeviceSubscriptions";

        readonly IDeviceLookupComposer deviceLookupComposer = new DeviceLookupComposer();

        static DeviceLookupEntry Entry(string thirdPartyId, string geotabSerialNumber, DateTime? recordLastChangedUtc = null)
        {
            return new DeviceLookupEntry
            {
                ThirdPartyId = thirdPartyId,
                GeotabSerialNumber = geotabSerialNumber,
                RecordLastChangedUtc = recordLastChangedUtc ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };
        }

        static DeviceLookupContribution Contribution(string sourceName, params DeviceLookupEntry[] entries)
        {
            return new DeviceLookupContribution { SourceName = sourceName, Entries = entries };
        }

        static DeviceLookupContribution ContributionWithWithdrawn(string sourceName, string[] unauthorizedThirdPartyIds, params DeviceLookupEntry[] entries)
        {
            return new DeviceLookupContribution { SourceName = sourceName, Entries = entries, UnauthorizedThirdPartyIds = unauthorizedThirdPartyIds };
        }

        [Fact]
        public void Compose_WithNoContributions_ReturnsAnEmptyMap()
        {
            //Act
            var result = deviceLookupComposer.Compose(new List<DeviceLookupContribution>());

            //Assert
            Assert.Empty(result.ThirdPartyIdToSerialNoMap);
            Assert.Empty(result.CorruptingCollisionMessages);
            Assert.Empty(result.AmbiguousCollisionMessages);
        }

        [Fact]
        public void Compose_WithOneSource_ReturnsEveryDevice()
        {
            //Arrange
            var contributions = new List<DeviceLookupContribution>
            {
                Contribution(ProvisionedDevicesSource, Entry("VIN-1", "G1"), Entry("VIN-2", "G2"))
            };

            //Act
            var result = deviceLookupComposer.Compose(contributions);

            //Assert
            Assert.Equal(2, result.ThirdPartyIdToSerialNoMap.Count);
            Assert.Equal("G1", result.ThirdPartyIdToSerialNoMap["VIN-1"]);
            Assert.Equal("G2", result.ThirdPartyIdToSerialNoMap["VIN-2"]);
            Assert.Empty(result.CorruptingCollisionMessages);
            Assert.Empty(result.AmbiguousCollisionMessages);
        }

        [Fact]
        public void Compose_WithTwoSourcesAndNoOverlap_MergesThem()
        {
            //Arrange
            var contributions = new List<DeviceLookupContribution>
            {
                Contribution(ProvisionedDevicesSource, Entry("VIN-1", "G1")),
                Contribution(DeviceSubscriptionsSource, Entry("VIN-2", "G2"))
            };

            //Act
            var result = deviceLookupComposer.Compose(contributions);

            //Assert
            Assert.Equal(2, result.ThirdPartyIdToSerialNoMap.Count);
            Assert.Empty(result.CorruptingCollisionMessages);
            Assert.Empty(result.AmbiguousCollisionMessages);
        }

        [Fact]
        public void Compose_WhenBothSourcesAgreeOnADevice_KeepsItAndCountsTheAgreement()
        {
            //Arrange
            var contributions = new List<DeviceLookupContribution>
            {
                Contribution(ProvisionedDevicesSource, Entry("VIN-1", "G1")),
                Contribution(DeviceSubscriptionsSource, Entry("VIN-1", "G1"))
            };

            //Act
            var result = deviceLookupComposer.Compose(contributions);

            //Assert
            Assert.Single(result.ThirdPartyIdToSerialNoMap);
            Assert.Equal("G1", result.ThirdPartyIdToSerialNoMap["VIN-1"]);
            Assert.Equal(1, result.AgreeingDuplicateCount);
            Assert.Empty(result.AmbiguousCollisionMessages);
            Assert.Empty(result.CorruptingCollisionMessages);
        }

        [Fact]
        public void Compose_WhenSourcesDisagreeOnAnIdentifiersSerialNumber_TheMoreRecentlyChangedRecordWins()
        {
            //Arrange
            var older = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newer = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            var contributions = new List<DeviceLookupContribution>
            {
                Contribution(ProvisionedDevicesSource, Entry("VIN-1", "G-OLD", older)),
                Contribution(DeviceSubscriptionsSource, Entry("VIN-1", "G-NEW", newer))
            };

            //Act
            var result = deviceLookupComposer.Compose(contributions);

            //Assert
            Assert.Equal("G-NEW", result.ThirdPartyIdToSerialNoMap["VIN-1"]);
            Assert.Single(result.AmbiguousCollisionMessages);
            Assert.Empty(result.CorruptingCollisionMessages);
        }

        [Fact]
        public void Compose_WhenSourcesDisagreeAndTheEarlierSourceIsFresher_TheEarlierSourceWins()
        {
            //Arrange - the tiebreak is decided by the data, not by the order the sources are composed in.
            var older = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newer = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            var contributions = new List<DeviceLookupContribution>
            {
                Contribution(ProvisionedDevicesSource, Entry("VIN-1", "G-NEW", newer)),
                Contribution(DeviceSubscriptionsSource, Entry("VIN-1", "G-OLD", older))
            };

            //Act
            var result = deviceLookupComposer.Compose(contributions);

            //Assert
            Assert.Equal("G-NEW", result.ThirdPartyIdToSerialNoMap["VIN-1"]);
            Assert.Single(result.AmbiguousCollisionMessages);
        }

        [Fact]
        public void Compose_WhenTwoIdentifiersShareASerialNumber_ExcludesBoth()
        {
            //Arrange
            var contributions = new List<DeviceLookupContribution>
            {
                Contribution(ProvisionedDevicesSource, Entry("VIN-1", "G1")),
                Contribution(DeviceSubscriptionsSource, Entry("VIN-2", "G1"))
            };

            //Act
            var result = deviceLookupComposer.Compose(contributions);

            //Assert
            Assert.Empty(result.ThirdPartyIdToSerialNoMap);
            Assert.Single(result.CorruptingCollisionMessages);
            Assert.Equal(2, result.ExcludedDeviceCount);
            Assert.Contains("VIN-1", result.CorruptingCollisionMessages[0]);
            Assert.Contains("VIN-2", result.CorruptingCollisionMessages[0]);
        }

        [Fact]
        public void Compose_WhenTwoIdentifiersShareASerialNumber_LeavesEveryOtherDeviceAlone()
        {
            //Arrange
            var contributions = new List<DeviceLookupContribution>
            {
                Contribution(ProvisionedDevicesSource, Entry("VIN-1", "G1"), Entry("VIN-3", "G3")),
                Contribution(DeviceSubscriptionsSource, Entry("VIN-2", "G1"), Entry("VIN-4", "G4"))
            };

            //Act
            var result = deviceLookupComposer.Compose(contributions);

            //Assert
            Assert.Equal(2, result.ThirdPartyIdToSerialNoMap.Count);
            Assert.Equal("G3", result.ThirdPartyIdToSerialNoMap["VIN-3"]);
            Assert.Equal("G4", result.ThirdPartyIdToSerialNoMap["VIN-4"]);
            Assert.Equal(2, result.ExcludedDeviceCount);
        }

        [Fact]
        public void Compose_WhenASerialNumberIsSharedByThreeIdentifiers_ExcludesAllOfThem()
        {
            //Arrange
            var contributions = new List<DeviceLookupContribution>
            {
                Contribution(ProvisionedDevicesSource, Entry("VIN-1", "G1"), Entry("VIN-2", "G1")),
                Contribution(DeviceSubscriptionsSource, Entry("VIN-3", "G1"))
            };

            //Act
            var result = deviceLookupComposer.Compose(contributions);

            //Assert
            Assert.Empty(result.ThirdPartyIdToSerialNoMap);
            Assert.Single(result.CorruptingCollisionMessages);
            Assert.Equal(3, result.ExcludedDeviceCount);
        }

        [Fact]
        public void Compose_WhenTheAmbiguityTiebreakResolvesASerialNumberConflict_DoesNotExclude()
        {
            //Arrange - VIN-1 held G1 in one source and G2 in the other, and VIN-2 also holds G1. Once the tiebreak
            //moves VIN-1 to G2 there is no longer a shared serial number, so nothing should be excluded.
            var older = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newer = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            var contributions = new List<DeviceLookupContribution>
            {
                Contribution(ProvisionedDevicesSource, Entry("VIN-1", "G1", older), Entry("VIN-2", "G1", older)),
                Contribution(DeviceSubscriptionsSource, Entry("VIN-1", "G2", newer))
            };

            //Act
            var result = deviceLookupComposer.Compose(contributions);

            //Assert
            Assert.Equal(2, result.ThirdPartyIdToSerialNoMap.Count);
            Assert.Equal("G2", result.ThirdPartyIdToSerialNoMap["VIN-1"]);
            Assert.Equal("G1", result.ThirdPartyIdToSerialNoMap["VIN-2"]);
            Assert.Empty(result.CorruptingCollisionMessages);
            Assert.Equal(0, result.ExcludedDeviceCount);
        }

        [Fact]
        public void Compose_TreatsIdentifiersThatDifferOnlyByCaseAsDifferentDevices()
        {
            //Arrange - casing is never folded, because it can be the only thing separating two valid identifiers.
            var contributions = new List<DeviceLookupContribution>
            {
                Contribution(ProvisionedDevicesSource, Entry("vin-1", "G1")),
                Contribution(DeviceSubscriptionsSource, Entry("VIN-1", "G2"))
            };

            //Act
            var result = deviceLookupComposer.Compose(contributions);

            //Assert
            Assert.Equal(2, result.ThirdPartyIdToSerialNoMap.Count);
            Assert.Equal("G1", result.ThirdPartyIdToSerialNoMap["vin-1"]);
            Assert.Equal("G2", result.ThirdPartyIdToSerialNoMap["VIN-1"]);
            Assert.Empty(result.AmbiguousCollisionMessages);
            Assert.Empty(result.CorruptingCollisionMessages);
        }

        [Fact]
        public void Compose_TreatsSerialNumbersThatDifferOnlyByCaseAsDifferentDevices()
        {
            //Arrange
            var contributions = new List<DeviceLookupContribution>
            {
                Contribution(ProvisionedDevicesSource, Entry("VIN-1", "g1")),
                Contribution(DeviceSubscriptionsSource, Entry("VIN-2", "G1"))
            };

            //Act
            var result = deviceLookupComposer.Compose(contributions);

            //Assert
            Assert.Equal(2, result.ThirdPartyIdToSerialNoMap.Count);
            Assert.Empty(result.CorruptingCollisionMessages);
        }

        [Fact]
        public void Compose_SkipsEntriesWithNoIdentifierOrNoSerialNumber()
        {
            //Arrange
            var contributions = new List<DeviceLookupContribution>
            {
                Contribution(ProvisionedDevicesSource, Entry("", "G1"), Entry("VIN-2", ""), Entry("VIN-3", "G3"))
            };

            //Act
            var result = deviceLookupComposer.Compose(contributions);

            //Assert
            Assert.Single(result.ThirdPartyIdToSerialNoMap);
            Assert.Equal("G3", result.ThirdPartyIdToSerialNoMap["VIN-3"]);
        }

        [Fact]
        public void Compose_WithNoWithdrawnIdentifiers_ReturnsAnEmptyWithdrawnSet()
        {
            //Arrange
            var contributions = new List<DeviceLookupContribution>
            {
                Contribution(DeviceSubscriptionsSource, Entry("VIN-1", "G1"))
            };

            //Act
            var result = deviceLookupComposer.Compose(contributions);

            //Assert
            Assert.Empty(result.UnauthorizedThirdPartyIdToSourceName);
        }

        [Fact]
        public void Compose_WithdrawnIdentifier_IsReportedAgainstTheSourceThatWithdrewIt()
        {
            //Arrange
            var contributions = new List<DeviceLookupContribution>
            {
                ContributionWithWithdrawn(DeviceSubscriptionsSource, ["VIN-2"], Entry("VIN-1", "G1"))
            };

            //Act
            var result = deviceLookupComposer.Compose(contributions);

            //Assert
            Assert.Single(result.ThirdPartyIdToSerialNoMap);
            Assert.Equal(DeviceSubscriptionsSource, result.UnauthorizedThirdPartyIdToSourceName["VIN-2"]);
        }

        [Fact]
        public void Compose_WithdrawnByEverySourceAndAuthorisedByNone_IsWithdrawn()
        {
            //Arrange
            var contributions = new List<DeviceLookupContribution>
            {
                ContributionWithWithdrawn(ProvisionedDevicesSource, ["VIN-1"]),
                ContributionWithWithdrawn(DeviceSubscriptionsSource, ["VIN-1", "VIN-2"])
            };

            //Act
            var result = deviceLookupComposer.Compose(contributions);

            //Assert
            Assert.Empty(result.ThirdPartyIdToSerialNoMap);
            Assert.Equal(2, result.UnauthorizedThirdPartyIdToSourceName.Count);
            Assert.Equal(ProvisionedDevicesSource, result.UnauthorizedThirdPartyIdToSourceName["VIN-1"]);
            Assert.Equal(DeviceSubscriptionsSource, result.UnauthorizedThirdPartyIdToSourceName["VIN-2"]);
        }

        [Fact]
        public void Compose_WithdrawnByOneSourceButAuthorisedByAnother_IsNotWithdrawn()
        {
            //Arrange
            // A hybrid deployment where one control plane has forgotten a device the other still authorises. The
            // authorisation has to win, or the stale plane would silently stop telemetry the live one permits.
            var contributions = new List<DeviceLookupContribution>
            {
                ContributionWithWithdrawn(ProvisionedDevicesSource, ["VIN-1"]),
                Contribution(DeviceSubscriptionsSource, Entry("VIN-1", "G1"))
            };

            //Act
            var result = deviceLookupComposer.Compose(contributions);

            //Assert
            Assert.Equal("G1", result.ThirdPartyIdToSerialNoMap["VIN-1"]);
            Assert.Empty(result.UnauthorizedThirdPartyIdToSourceName);
        }

        [Fact]
        public void Compose_IdentifierExcludedByACorruptingCollision_IsNotWithdrawn()
        {
            //Arrange
            // The subtle case. Both identifiers are excluded from the composed map, so a withdrawn set derived from
            // the map alone would call them withdrawn and start failing their telemetry at once. Exclusion is a
            // refusal to guess, not a withdrawal of authorisation, and its telemetry must keep being held until an
            // operator resolves the conflict.
            var contributions = new List<DeviceLookupContribution>
            {
                ContributionWithWithdrawn(ProvisionedDevicesSource, ["VIN-3"], Entry("VIN-1", "G1")),
                Contribution(DeviceSubscriptionsSource, Entry("VIN-2", "G1"))
            };

            //Act
            var result = deviceLookupComposer.Compose(contributions);

            //Assert
            Assert.Empty(result.ThirdPartyIdToSerialNoMap);
            Assert.Equal(2, result.ExcludedDeviceCount);
            Assert.Single(result.UnauthorizedThirdPartyIdToSourceName);
            Assert.Equal(ProvisionedDevicesSource, result.UnauthorizedThirdPartyIdToSourceName["VIN-3"]);
            Assert.DoesNotContain("VIN-1", result.UnauthorizedThirdPartyIdToSourceName.Keys);
            Assert.DoesNotContain("VIN-2", result.UnauthorizedThirdPartyIdToSourceName.Keys);
        }

        [Fact]
        public void Compose_WithdrawnIdentifiers_AreComparedOrdinally()
        {
            //Arrange
            // Casing is never folded anywhere in this design, so a case-only difference is a different device and
            // must not withdraw the one that is authorised.
            var contributions = new List<DeviceLookupContribution>
            {
                ContributionWithWithdrawn(DeviceSubscriptionsSource, ["vin-1"], Entry("VIN-1", "G1"))
            };

            //Act
            var result = deviceLookupComposer.Compose(contributions);

            //Assert
            Assert.Equal("G1", result.ThirdPartyIdToSerialNoMap["VIN-1"]);
            Assert.Single(result.UnauthorizedThirdPartyIdToSourceName);
            Assert.Equal(DeviceSubscriptionsSource, result.UnauthorizedThirdPartyIdToSourceName["vin-1"]);
        }

        [Fact]
        public void Compose_SkipsWithdrawnIdentifiersThatAreEmpty()
        {
            //Arrange
            var contributions = new List<DeviceLookupContribution>
            {
                ContributionWithWithdrawn(DeviceSubscriptionsSource, ["", "VIN-2"])
            };

            //Act
            var result = deviceLookupComposer.Compose(contributions);

            //Assert
            Assert.Single(result.UnauthorizedThirdPartyIdToSourceName);
            Assert.Equal(DeviceSubscriptionsSource, result.UnauthorizedThirdPartyIdToSourceName["VIN-2"]);
        }
    }
}
