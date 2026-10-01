using System;
using MyGeotabAPIAdapter.Database.EntityMappers;
using MyGeotabAPIAdapter.Database.Models;
using Xunit;

namespace MyGeotabAPIAdapter.Tests.GeotabDIGAdapter.Core.Database.Models
{
    /// <summary>
    /// Pins the device-subscription OperationType encoding.
    /// <para>
    /// These tests exist because the encoding has a failure mode nothing else catches. Both 0 and 1 are legal values, so getting one wrong does not throw, does not fail a merge and does not show up in a log - it silently performs the opposite operation. A Terminate that resolves to an active subscription stops that device transmitting and spends termination-guard budget, and reports success while doing it.
    /// </para>
    /// <para>
    /// Nor do the other gates see it. The merge suite drives SQL directly and never instantiates the service, so a consistently inverted procedure and a consistently inverted set of test inputs produce a byte-identical golden transcript whether or not the C# agrees with them. Until these tests were written, nothing under test referenced this encoding at all.
    /// </para>
    /// </summary>
    public class DeviceSubscriptionOperationTypeTests
    {
        #region Absolute values

        [Fact]
        public void Terminate_IsZero()
        {
            Assert.Equal(0, DeviceSubscriptionOperationType.Terminate);
        }

        [Fact]
        public void Subscribe_IsOne()
        {
            Assert.Equal(1, DeviceSubscriptionOperationType.Subscribe);
        }

        #endregion

        #region Alignment with the state the instruction produces

        /// <summary>
        /// The reason for the numbering, asserted as such rather than as two unrelated constants. An instruction and the subscription state it produces carry the same number, so there is nothing to invert when reading one against the other. If either column is ever renumbered, this is the test that should fail and say why.
        /// </summary>
        [Fact]
        public void Terminate_AlignsWith_SubscriptionStatusTerminated()
        {
            Assert.Equal(DbGdaDeviceSubscription.SubscriptionStatusTerminated,
                         DeviceSubscriptionOperationType.Terminate);
        }

        [Fact]
        public void Subscribe_AlignsWith_SubscriptionStatusActive()
        {
            Assert.Equal(DbGdaDeviceSubscription.SubscriptionStatusActive,
                         DeviceSubscriptionOperationType.Subscribe);
        }

        /// <summary>
        /// Guards the property the alignment rests on, independently of the two values above: whichever numbers the two columns use, they must run in the same direction. A renumbering that kept both pairs distinct but flipped one relative to the other would pass every other assertion here.
        /// </summary>
        [Fact]
        public void OperationType_And_SubscriptionStatus_RunInTheSameDirection()
        {
            var operationTypeDirection =
                DeviceSubscriptionOperationType.Subscribe.CompareTo(DeviceSubscriptionOperationType.Terminate);
            var subscriptionStatusDirection =
                DbGdaDeviceSubscription.SubscriptionStatusActive.CompareTo(DbGdaDeviceSubscription.SubscriptionStatusTerminated);

            Assert.Equal(Math.Sign(subscriptionStatusDirection), Math.Sign(operationTypeDirection));
        }

        #endregion

        #region The default value is not a safe one

        /// <summary>
        /// Records a consequence of the numbering rather than testing the framework: an OperationType left unset is Terminate, because the default of a byte is 0. Anything constructing an instruction must set the value explicitly, and this is here so that the next person to read the constant discovers that from a test rather than from a terminated fleet.
        /// </summary>
        [Fact]
        public void UnsetOperationType_MeansTerminate_WhichIsWhyItMustAlwaysBeSetExplicitly()
        {
            var queueRecord = new DbGdaQDeviceSubscription();

            Assert.Equal(DeviceSubscriptionOperationType.Terminate, queueRecord.OperationType);
        }

        #endregion

        #region The queue-to-staging mapper carries the value through unchanged

        [Theory]
        [InlineData(DeviceSubscriptionOperationType.Subscribe)]
        [InlineData(DeviceSubscriptionOperationType.Terminate)]
        public void Mapper_CarriesOperationTypeThroughUnchanged(byte operationType)
        {
            var mapper = new DbGdaQDeviceSubscriptionDbGdaStgDeviceSubscriptionEntityMapper();
            var queueRecord = new DbGdaQDeviceSubscription
            {
                id = 42,
                OrderingOrdinal = 7,
                OperationType = operationType,
                ThirdPartyId = "VIN-TEST-001",
                IdentifierType = "Vin",
                GeotabSerialNumber = "G9TEST123456"
            };

            var staged = mapper.CreateEntity(queueRecord, Guid.NewGuid(), queueRecord.OperationType);

            Assert.Equal(operationType, staged.OperationType);
        }

        /// <summary>
        /// The mapper takes the operation type as its own parameter rather than reading it off the queue record, so a caller can disagree with the record it is staging. That is deliberate - snapshot intake stages rows the queue never carried - but it means the parameter, not the record, decides. Asserted so that a future change which starts reading the record instead is caught here rather than in a merge.
        /// </summary>
        [Fact]
        public void Mapper_StagesTheOperationTypeItIsGiven_NotTheOneOnTheRecord()
        {
            var mapper = new DbGdaQDeviceSubscriptionDbGdaStgDeviceSubscriptionEntityMapper();
            var queueRecord = new DbGdaQDeviceSubscription
            {
                OperationType = DeviceSubscriptionOperationType.Subscribe,
                ThirdPartyId = "VIN-TEST-002",
                GeotabSerialNumber = "G9TEST223456"
            };

            var staged = mapper.CreateEntity(queueRecord, Guid.NewGuid(),
                                             DeviceSubscriptionOperationType.Terminate);

            Assert.Equal(DeviceSubscriptionOperationType.Terminate, staged.OperationType);
        }

        #endregion
    }
}
