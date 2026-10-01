using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// Guards the two properties that bound how much the SQL Server Level 2 maintenance views read - in both adapters and in
    /// both install paths - neither of which a result-set check can see, because a view that reads too much still returns the
    /// right rows.
    /// <para>
    /// First, the table-list CTE begins <c>SELECT TOP (2147483647)</c>. Without that fence the optimizer applies
    /// <c>sys.dm_db_index_physical_stats</c> to every table in the database and discards the other schemas' rows afterwards,
    /// so on a database shared by the two adapters each view reads the other's indexes too. The fence works because no
    /// predicate may cross a TOP; that is observed optimizer behaviour rather than a documented guarantee, which is why it is
    /// pinned here.
    /// </para>
    /// <para>
    /// Second, the per-partition statistics are read once. SQL Server evaluates a CTE once per reference, so a second
    /// reference to <c>PartitionStats</c> - an aggregate CTE joined back to it - reads every index twice per run.
    /// </para>
    /// </summary>
    public class SqlServerLevel2ViewReadTests
    {
        static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && File.Exists(Path.Combine(directory.FullName, "MyGeotabAPIAdapter.sln")) == false)
            {
                directory = directory.Parent;
            }
            Assert.True(directory != null, $"Could not find MyGeotabAPIAdapter.sln above {AppContext.BaseDirectory}.");
            return directory.FullName;
        }

        /// <summary>
        /// A SQL Server CREATE VIEW batch, from its first line to the GO that ends it.
        /// </summary>
        static string ReadView(string project, string fileName, string qualifiedViewName)
        {
            var path = Path.Combine(RepositoryRoot(), project, "Scripts", "SQLServer", "v2", fileName);
            Assert.True(File.Exists(path), $"Schema script not found: {path}");
            var script = File.ReadAllText(path).Replace("\r\n", "\n");
            var matches = Regex.Matches(script, @"CREATE VIEW " + Regex.Escape(qualifiedViewName) + @"\s.*?(?=^GO\s*$)", RegexOptions.Singleline | RegexOptions.Multiline);
            Assert.True(matches.Count == 1, $"Expected exactly one CREATE VIEW for {qualifiedViewName} in {fileName}, found {matches.Count}.");
            return matches[0].Value;
        }

        public static TheoryData<string, string, string> Level2Views => new()
        {
            { "GeotabDIGAdapter", "MSSQL_CumulativeSchemaCreation.sql", "[gda].[vwStatsForLevel2DigMaintenance]" },
            { "GeotabDIGAdapter", "MSSQL_5.2.0.0_OemSubscriptionMode.sql", "[gda].[vwStatsForLevel2DigMaintenance]" },
            { "MyGeotabAPIAdapter", "MSSQL_CumulativeSchemaCreation.sql", "[dbo].[vwStatsForLevel2DBMaintenance]" },
            { "MyGeotabAPIAdapter", "MSSQL_5.2.0.0_spManagePartitionsSchemaScoping.sql", "[dbo].[vwStatsForLevel2DBMaintenance]" },
        };

        [Theory]
        [MemberData(nameof(Level2Views))]
        public void TheTableListIsFencedWithTop(string project, string fileName, string view)
        {
            var body = ReadView(project, fileName, view);
            var firstCte = Regex.Match(body, @"WITH\s+\w+\s+AS\s*\((.*?)\)\s*,", RegexOptions.Singleline);

            Assert.True(firstCte.Success, $"{view} in {fileName}: no leading CTE found.");
            Assert.Matches(@"^\s*(--[^\n]*\n\s*)*SELECT TOP \(2147483647\)", firstCte.Groups[1].Value);
        }

        [Theory]
        [MemberData(nameof(Level2Views))]
        public void ThePhysicalStatisticsAreReadOnce(string project, string fileName, string view)
        {
            var body = ReadView(project, fileName, view);

            Assert.Single(Regex.Matches(body, @"sys\.dm_db_index_physical_stats"));
            // PartitionStats is defined once and must be referenced once: every further reference is a second full read.
            Assert.Single(Regex.Matches(body, @"\bFROM\s+PartitionStats\b|\bJOIN\s+PartitionStats\b"));
            Assert.DoesNotContain("AggregateStats", body);
        }
    }
}
