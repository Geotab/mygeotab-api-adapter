using MyGeotabAPIAdapter.Configuration;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// Unit tests for <see cref="ConfigurationEnumParser"/>.
    /// <para>
    /// Each refusal below is a value that <see cref="System.Enum.TryParse{TEnum}(string, bool, out TEnum)"/>
    /// accepts on its own. They are the reason this class exists: every one of them produced a successfully
    /// parsed value that named no member an application would ever act on, and so turned a configuration typo
    /// into a symptom somewhere else.
    /// </para>
    /// </summary>
    public class ConfigurationEnumParserTests
    {
        [Theory]
        [InlineData("Continuous", OperationMode.Continuous)]
        [InlineData("Scheduled", OperationMode.Scheduled)]
        public void TryParse_WithTheExactMemberName_Matches(string configItemValue, OperationMode expected)
        {
            Assert.True(ConfigurationEnumParser.TryParse(configItemValue, out OperationMode actual));
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("continuous")]
        [InlineData("CONTINUOUS")]
        [InlineData("cOnTiNuOuS")]
        public void TryParse_IgnoresCase(string configItemValue)
        {
            // The defect this class was written for: a value differing only in case was refused, and nothing in
            // the configuration documentation said that this particular setting was case-sensitive.
            Assert.True(ConfigurationEnumParser.TryParse(configItemValue, out OperationMode actual));
            Assert.Equal(OperationMode.Continuous, actual);
        }

        [Theory]
        [InlineData(" Continuous")]
        [InlineData("Continuous ")]
        [InlineData("  Continuous  ")]
        public void TryParse_IgnoresSurroundingWhitespace(string configItemValue)
        {
            Assert.True(ConfigurationEnumParser.TryParse(configItemValue, out OperationMode actual));
            Assert.Equal(OperationMode.Continuous, actual);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("1")]
        public void TryParse_RefusesANumericValueThatIsInsideTheEnum(string configItemValue)
        {
            // Enum.TryParse accepts these and returns a real member, so an operator who wrote a number into a
            // setting documented as taking a name would get a silently different configuration than the one the
            // file appears to describe.
            Assert.False(ConfigurationEnumParser.TryParse(configItemValue, out OperationMode _));
        }

        [Theory]
        [InlineData("99")]
        [InlineData("-5")]
        [InlineData("2147483647")]
        public void TryParse_RefusesANumericValueThatIsOutsideTheEnum(string configItemValue)
        {
            // Enum.TryParse returns true here and casts the number to the enum type without checking that the
            // result is a member. Every switch over the value then falls to its default arm.
            Assert.False(ConfigurationEnumParser.TryParse(configItemValue, out OperationMode _));
        }

        [Theory]
        [InlineData("Continuous, Scheduled")]
        [InlineData("Continuous,Scheduled")]
        public void TryParse_RefusesACommaSeparatedList(string configItemValue)
        {
            // Enum.TryParse treats a comma-separated list as a set of flags to OR together even when the enum
            // carries no FlagsAttribute, so this parses to a single member that the operator did not name.
            Assert.False(ConfigurationEnumParser.TryParse(configItemValue, out OperationMode _));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        [InlineData("Sporadic")]
        [InlineData("Contin uous")]
        public void TryParse_RefusesAValueThatNamesNoMember(string? configItemValue)
        {
            Assert.False(ConfigurationEnumParser.TryParse(configItemValue!, out OperationMode _));
        }

        [Fact]
        public void TryParse_OnRefusal_LeavesTheResultAtItsDefault()
        {
            Assert.False(ConfigurationEnumParser.TryParse("Sporadic", out OperationMode actual));
            Assert.Equal(default, actual);
        }

        [Fact]
        public void DescribeValidValues_ListsEveryMember()
        {
            Assert.Equal("'Continuous', 'Scheduled'", ConfigurationEnumParser.DescribeValidValues<OperationMode>());
        }

        [Fact]
        public void DescribeValidValues_OmitsTheMembersDeclaredUnsupported()
        {
            // A member that is defined on the enum but is not a valid configuration value must not be advertised
            // to an operator as though it were one.
            Assert.Equal("'Continuous'", ConfigurationEnumParser.DescribeValidValues(OperationMode.Scheduled));
        }
    }
}
