using MyGeotabAPIAdapter.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// Test data for <see cref="StringHelperTests.AreEqualTests_AllPossibilities(string, string, bool)"/>.
    /// </summary>
    public class TestData_AreEqualTests_AllPossibilities : TheoryData<string, string, bool>
    {
        public TestData_AreEqualTests_AllPossibilities()
        {
            Add("A", "A", true);
            Add("", "a", false);
            Add("A", null, false);
            Add("A", "", false);
            Add(null, null, true);
            Add("", "", true);
            Add(null, "", true);
            Add("", null, true);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="StringHelper"/> class.
    /// </summary>
    public class StringHelperTests
    {
        private readonly ITestOutputHelper output;

        public StringHelperTests(ITestOutputHelper output)
        {
            // Initialize objects as required:
            this.output = output;
        }

        [Theory]
        [ClassData(typeof(TestData_AreEqualTests_AllPossibilities))]
        public void AreEqualTests_AllPossibilities(string s1, string s2, bool expected)
        {
            var stringHelper = new StringHelper();
            var result = stringHelper.AreEqual(s1, s2);
            output.WriteLine($"s1: {s1}, s2: {s2}, result: {result}");
            Assert.Equal(expected, result);
        }
    }
}
