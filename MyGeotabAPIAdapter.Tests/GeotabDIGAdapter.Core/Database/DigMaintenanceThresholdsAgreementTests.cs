using MyGeotabAPIAdapter.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// Unit tests asserting that the Geotab DIG Adapter's Level 1 and Level 2 maintenance thresholds agree between the two
    /// places they live: the <c>WHERE</c> clauses and column expressions of the two maintenance views, read out of the four
    /// shipped schema scripts, and the constants in <see cref="DigMaintenanceThresholds"/> that the service compares the
    /// views' rows against.
    /// <para>
    /// A disagreement between them does not fail at runtime. The view offers a candidate the service declines, or the service
    /// waits for one the view never offers, and the maintenance simply never happens - which is why this is a test rather
    /// than something a run would reveal.
    /// </para>
    /// <para>
    /// It asserts SCALE, not only presence. Each column's unit is derived from its own expression in the view - a
    /// percentage where the expression multiplies by 100, a ratio where it does not - and each constant compared against it
    /// must lie in that unit's range. Comparing literals alone would have passed on the MyGeotab API Adapter's pre-fix
    /// thresholds, which held ratio values against percentage columns and so acted at a hundredth of their intent.
    /// </para>
    /// <para>
    /// The comparison is a function of the view text and a set of thresholds, so that the tests below can show it FAILS:
    /// on a perturbed view literal, on a perturbed constant, on a view whose scale changes, and on those pre-fix values. A
    /// check never shown to fail is not evidence that the thresholds agree.
    /// </para>
    /// </summary>
    public class DigMaintenanceThresholdsAgreementTests
    {
        /// <summary>
        /// The thresholds under test. Built from <see cref="DigMaintenanceThresholds"/> for the real check, and varied one at
        /// a time for the negative ones.
        /// </summary>
        public sealed record Thresholds(
            long MssqlStatsModsCount,
            double MssqlStatsPct,
            double MssqlReorganizePct,
            double MssqlRebuildPartitionPct,
            double MssqlRebuildIndexPct,
            long MssqlMinimumIndexSizeKB,
            double PgDeadTupleRatio,
            long PgDeadTuplesCount,
            double PgAnalyzeRatio,
            double PgBloatRatio);

        static readonly Thresholds Shipped = new(
            DigMaintenanceThresholds.MSSQL_UpdateStatisticsThreshold_ModsSinceLastStatsUpdateCount,
            DigMaintenanceThresholds.MSSQL_UpdateStatisticsThreshold_PctModsSinceLastStatsUpdate,
            DigMaintenanceThresholds.MSSQL_ReorganizeThreshold_FragmentationPct,
            DigMaintenanceThresholds.MSSQL_ReindexThreshold_FragmentationPct,
            DigMaintenanceThresholds.MSSQL_ReindexByTableThreshold_HighFragPartitionPct,
            DigMaintenanceThresholds.MSSQL_MinimumIndexSizeKB,
            DigMaintenanceThresholds.PG_VacuumThreshold_DeadTupleRatio,
            DigMaintenanceThresholds.PG_VacuumThreshold_DeadTuplesCount,
            DigMaintenanceThresholds.PG_AnalyzeThreshold_ModsSinceLastAnalyzeRatio,
            DigMaintenanceThresholds.PG_ReindexThreshold_BloatRatio);

        /// <summary>
        /// The four view bodies of one install path.
        /// </summary>
        public sealed record Views(string PgLevel1, string PgLevel2, string MssqlLevel1, string MssqlLevel2);

        const string PgLevel1View = "vwStatsForLevel1DigMaintenance";
        const string PgLevel2View = "vwStatsForLevel2DigMaintenance";

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

        static string ReadScript(string provider, string fileName)
        {
            var path = Path.Combine(RepositoryRoot(), "GeotabDIGAdapter", "Scripts", provider, "v2", fileName);
            Assert.True(File.Exists(path), $"Schema script not found: {path}");
            return File.ReadAllText(path).Replace("\r\n", "\n");
        }

        /// <summary>
        /// A PostgreSQL CREATE VIEW statement, from its first line to its terminating semicolon.
        /// </summary>
        static string ExtractPgView(string script, string viewName)
        {
            var matches = Regex.Matches(script, @"CREATE VIEW gda\.""" + Regex.Escape(viewName) + @""" AS\s.*?;", RegexOptions.Singleline);
            Assert.True(matches.Count == 1, $"Expected exactly one CREATE VIEW for gda.\"{viewName}\", found {matches.Count}.");
            return matches[0].Value;
        }

        /// <summary>
        /// A SQL Server CREATE VIEW batch, from its first line to the GO that ends it.
        /// </summary>
        static string ExtractMssqlView(string script, string viewName)
        {
            var matches = Regex.Matches(script, @"CREATE VIEW \[gda\]\.\[" + Regex.Escape(viewName) + @"\]\s.*?(?=^GO\s*$)", RegexOptions.Singleline | RegexOptions.Multiline);
            Assert.True(matches.Count == 1, $"Expected exactly one CREATE VIEW for [gda].[{viewName}], found {matches.Count}.");
            return matches[0].Value;
        }

        static Views ReadViews(string installPath)
        {
            var (pgScript, mssqlScript) = installPath switch
            {
                "cumulative" => (ReadScript("PostgreSQL", "PG_CumulativeSchemaCreation.sql"), ReadScript("SQLServer", "MSSQL_CumulativeSchemaCreation.sql")),
                "upgrade" => (ReadScript("PostgreSQL", "PG_5.2.0.0_OemSubscriptionMode.sql"), ReadScript("SQLServer", "MSSQL_5.2.0.0_OemSubscriptionMode.sql")),
                _ => throw new ArgumentException(installPath)
            };
            return new Views(
                ExtractPgView(pgScript, PgLevel1View),
                ExtractPgView(pgScript, PgLevel2View),
                ExtractMssqlView(mssqlScript, PgLevel1View),
                ExtractMssqlView(mssqlScript, PgLevel2View));
        }

        static string Normalize(string sql) => Regex.Replace(sql, @"\s+", " ");

        /// <summary>
        /// The scale a view column is expressed on, read from the expression that defines it: 100 for a percentage, where
        /// the expression multiplies by 100, and 1 for a ratio, where it does not.
        /// </summary>
        static double ScaleOf(string columnExpression) => Regex.IsMatch(columnExpression, @"\*\s*100\b") ? 100 : 1;

        /// <summary>
        /// Checks that a constant is written in the unit of the column it is compared against. It must be positive, no more
        /// than the column's scale, and more than a hundredth of it: a threshold below 1% of a column's range is taken to be
        /// written in the other unit, which is exactly how a ratio of 0.1 on a percentage column reads.
        /// </summary>
        static void ExpectInScale(List<string> problems, string label, double constant, double scale)
        {
            if (constant <= scale / 100 || constant > scale)
            {
                var unit = scale == 100 ? "a PERCENTAGE (0..100)" : "a RATIO (0..1)";
                problems.Add($"{label}: the constant is {constant}, but the column it is compared against is {unit}, so the constant must lie above {scale / 100} and at most {scale}.");
            }
        }

        /// <summary>
        /// Finds a literal in a view by pattern and checks it equals the constant that must agree with it. The pattern must
        /// match exactly once; a view reworded so that it no longer does is reported, because silence would be a pass.
        /// </summary>
        static void ExpectLiteral(List<string> problems, string label, string normalizedView, string pattern, double constant)
        {
            var matches = Regex.Matches(normalizedView, pattern);
            if (matches.Count != 1)
            {
                problems.Add($"{label}: expected the view to contain exactly one match for /{pattern}/, found {matches.Count}. If the view was reworded, update this test to read the new form.");
                return;
            }
            var literal = double.Parse(matches[0].Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            if (literal != constant)
            {
                problems.Add($"{label}: the view uses {literal} and the constant is {constant}.");
            }
        }

        /// <summary>
        /// Captures the expression that defines a column, anchored on the alias of the column before it.
        /// </summary>
        static string ColumnExpression(List<string> problems, string label, string normalizedView, string pattern)
        {
            var match = Regex.Match(normalizedView, pattern);
            if (match.Success == false)
            {
                problems.Add($"{label}: could not find the expression defining the column (/{pattern}/). If the view was reworded, update this test to read the new form.");
                return string.Empty;
            }
            return match.Groups[1].Value;
        }

        /// <summary>
        /// Every way the four views and the thresholds can disagree. Empty when they agree.
        /// </summary>
        public static List<string> FindDisagreements(Views views, Thresholds t)
        {
            var problems = new List<string>();

            // PostgreSQL Level 1: three thresholds in the view's WHERE clause, all of them ratios or counts.
            var pg1 = Normalize(views.PgLevel1);
            ExpectLiteral(problems, "PostgreSQL Level 1 dead-tuple ratio", pg1, @"n_dead_tup::numeric > \(([0-9.]+) \* s\.n_live_tup::numeric\)", t.PgDeadTupleRatio);
            ExpectLiteral(problems, "PostgreSQL Level 1 analyze ratio", pg1, @"n_mod_since_analyze::numeric > \(([0-9.]+) \* s\.n_live_tup::numeric\)", t.PgAnalyzeRatio);
            ExpectLiteral(problems, "PostgreSQL Level 1 dead-tuple count", pg1, @"OR s\.n_dead_tup > ([0-9]+)\)", t.PgDeadTuplesCount);
            var pgDeadTuples = ColumnExpression(problems, "PostgreSQL Level 1 PctDeadTuples", pg1, @"AS ""DeadTuples"", (.*?) AS ""PctDeadTuples""");
            ExpectInScale(problems, "PostgreSQL Level 1 dead-tuple ratio against PctDeadTuples", t.PgDeadTupleRatio, ScaleOf(pgDeadTuples));
            var pgModsSinceAnalyze = ColumnExpression(problems, "PostgreSQL Level 1 PctModsSinceLastAnalyze", pg1, @"AS ""ModsSinceLastAnalyze"", (.*?) AS ""PctModsSinceLastAnalyze""");
            ExpectInScale(problems, "PostgreSQL Level 1 analyze ratio against PctModsSinceLastAnalyze", t.PgAnalyzeRatio, ScaleOf(pgModsSinceAnalyze));

            // PostgreSQL Level 2: no threshold in the view at all - both live only in the adapter - so a threshold added to
            // the view would hide candidates from the adapter's own.
            var pg2 = Normalize(views.PgLevel2);
            if (Regex.IsMatch(pg2, @"(bloat_ratio|IndexBloatRatio|index_size_bytes|total_size|IndexSize)""?\s*>=?\s*[0-9]"))
            {
                problems.Add("PostgreSQL Level 2: the view filters on bloat or index size. Those thresholds live only in the adapter; a filter in the view silently hides the candidates between the two.");
            }
            var pgBloat = ColumnExpression(problems, "PostgreSQL Level 2 bloat_ratio", pg2, @"AS index_size_bytes, (.*?) AS bloat_ratio");
            ExpectInScale(problems, "PostgreSQL Level 2 bloat ratio against IndexBloatRatio", t.PgBloatRatio, ScaleOf(pgBloat));

            // SQL Server Level 1: a count in the view, and a percentage the adapter compares against.
            var ms1 = Normalize(views.MssqlLevel1);
            ExpectLiteral(problems, "SQL Server Level 1 modification count", ms1, @"WHERE ModsSinceLastStatsUpdate > ([0-9]+);", t.MssqlStatsModsCount);
            var msMods = ColumnExpression(problems, "SQL Server Level 1 PctModsSinceLastStatsUpdate", ms1, @"ModsSinceLastStatsUpdate, (CASE .*? END) AS PctModsSinceLastStatsUpdate");
            ExpectInScale(problems, "SQL Server Level 1 statistics threshold against PctModsSinceLastStatsUpdate", t.MssqlStatsPct, ScaleOf(msMods));

            // SQL Server Level 2: two literals in the view, one in its definition of a highly fragmented partition, and
            // percentages throughout.
            var ms2 = Normalize(views.MssqlLevel2);
            ExpectLiteral(problems, "SQL Server Level 2 reorganize threshold", ms2, @"WHERE FragmentationPct > ([0-9.]+) AND", t.MssqlReorganizePct);
            ExpectLiteral(problems, "SQL Server Level 2 minimum index size", ms2, @"AND IndexSizeKB > ([0-9]+);", t.MssqlMinimumIndexSizeKB);
            ExpectLiteral(problems, "SQL Server Level 2 rebuild threshold (the view's highly fragmented partition)", ms2, @"SUM\(CASE WHEN FragmentationPct > ([0-9.]+) THEN 1 ELSE 0 END\) OVER \([^)]*\) AS HighFragPartitions", t.MssqlRebuildPartitionPct);
            if (Regex.IsMatch(ms2, @"ps\.\[avg_fragmentation_in_percent\] AS FragmentationPct,") == false)
            {
                problems.Add("SQL Server Level 2 FragmentationPct: expected it to be sys.dm_db_index_physical_stats.avg_fragmentation_in_percent unchanged - a PERCENTAGE - and it is not.");
            }
            ExpectInScale(problems, "SQL Server Level 2 reorganize threshold against FragmentationPct", t.MssqlReorganizePct, 100);
            ExpectInScale(problems, "SQL Server Level 2 rebuild threshold against FragmentationPct", t.MssqlRebuildPartitionPct, 100);
            var msHighFrag = ColumnExpression(problems, "SQL Server Level 2 PctHighFragPartitions", ms2, @"HighFragPartitions, (CASE .*? END) AS PctHighFragPartitions");
            ExpectInScale(problems, "SQL Server Level 2 whole-index rebuild threshold against PctHighFragPartitions", t.MssqlRebuildIndexPct, ScaleOf(msHighFrag));

            // The two SQL Server fragmentation thresholds are a ladder: reorganize below rebuild.
            if (t.MssqlReorganizePct >= t.MssqlRebuildPartitionPct)
            {
                problems.Add($"SQL Server Level 2: the reorganize threshold ({t.MssqlReorganizePct}) must be below the rebuild threshold ({t.MssqlRebuildPartitionPct}), or no partition is ever reorganized.");
            }

            return problems;
        }

        [Theory]
        [InlineData("cumulative")]
        [InlineData("upgrade")]
        public void ShippedViewsAndConstants_Agree(string installPath)
        {
            var problems = FindDisagreements(ReadViews(installPath), Shipped);

            Assert.True(problems.Count == 0, $"The {installPath} scripts and DigMaintenanceThresholds disagree:\n  " + string.Join("\n  ", problems));
        }

        [Fact]
        public void CumulativeAndUpgradeViews_AreIdentical()
        {
            // A view is the one kind of object whose body a threshold lives in, so the two install paths carrying different
            // bodies would give two deployments different maintenance. The install-path fingerprint catches this too; this
            // says so without a database.
            Assert.Equal(ReadViews("cumulative"), ReadViews("upgrade"));
        }

        /// <summary>
        /// One literal in one view changed, and the disagreement the check must then report.
        /// </summary>
        public static IEnumerable<object[]> ViewPerturbations() => new[]
        {
            new object[] { "PgLevel1", "(0.2 * s.n_live_tup", "(0.3 * s.n_live_tup", "dead-tuple ratio" },
            new object[] { "PgLevel1", "(0.1 * s.n_live_tup", "(0.15 * s.n_live_tup", "analyze ratio" },
            new object[] { "PgLevel1", "OR s.n_dead_tup > 1000)", "OR s.n_dead_tup > 5000)", "dead-tuple count" },
            new object[] { "PgLevel1", "(s.n_dead_tup::numeric / NULLIF(s.n_live_tup, 0)::numeric) AS \"PctDeadTuples\"", "(s.n_dead_tup::numeric / NULLIF(s.n_live_tup, 0)::numeric * 100) AS \"PctDeadTuples\"", "against PctDeadTuples" },
            new object[] { "PgLevel2", "FROM orderedrows;", "FROM orderedrows WHERE \"IndexBloatRatio\" > 0.5;", "filters on bloat" },
            new object[] { "MssqlLevel1", "WHERE ModsSinceLastStatsUpdate > 1000;", "WHERE ModsSinceLastStatsUpdate > 2000;", "modification count" },
            new object[] { "MssqlLevel1", "/ RecordCount) * 100", "/ RecordCount)", "against PctModsSinceLastStatsUpdate" },
            new object[] { "MssqlLevel2", "WHERE FragmentationPct > 10", "WHERE FragmentationPct > 15", "reorganize threshold" },
            new object[] { "MssqlLevel2", "WHEN FragmentationPct > 30 THEN", "WHEN FragmentationPct > 40 THEN", "rebuild threshold" },
            new object[] { "MssqlLevel2", "/ TotalPartitions) * 100", "/ TotalPartitions)", "against PctHighFragPartitions" },
        };

        [Theory]
        [MemberData(nameof(ViewPerturbations))]
        public void AChangedViewLiteral_IsReported(string view, string original, string replacement, string expectedProblem)
        {
            var shipped = ReadViews("cumulative");
            string Perturb(string body)
            {
                var normalized = Normalize(body);
                Assert.True(normalized.Contains(original), $"The perturbation's anchor '{original}' is not in the {view} view, so it would test nothing.");
                return normalized.Replace(original, replacement);
            }
            var perturbed = view switch
            {
                "PgLevel1" => shipped with { PgLevel1 = Perturb(shipped.PgLevel1) },
                "PgLevel2" => shipped with { PgLevel2 = Perturb(shipped.PgLevel2) },
                "MssqlLevel1" => shipped with { MssqlLevel1 = Perturb(shipped.MssqlLevel1) },
                "MssqlLevel2" => shipped with { MssqlLevel2 = Perturb(shipped.MssqlLevel2) },
                _ => throw new ArgumentException(view)
            };

            var problems = FindDisagreements(perturbed, Shipped);

            Assert.Contains(problems, problem => problem.Contains(expectedProblem));
        }

        /// <summary>
        /// One constant changed, and the disagreement the check must then report.
        /// </summary>
        public static IEnumerable<object[]> ConstantPerturbations() => new[]
        {
            new object[] { Shipped with { PgDeadTupleRatio = 0.25 }, "dead-tuple ratio" },
            new object[] { Shipped with { PgAnalyzeRatio = 0.2 }, "analyze ratio" },
            new object[] { Shipped with { PgDeadTuplesCount = 999 }, "dead-tuple count" },
            new object[] { Shipped with { PgBloatRatio = 30 }, "bloat ratio" },
            new object[] { Shipped with { MssqlStatsModsCount = 500 }, "modification count" },
            new object[] { Shipped with { MssqlReorganizePct = 12 }, "reorganize threshold" },
            new object[] { Shipped with { MssqlRebuildPartitionPct = 35 }, "rebuild threshold" },
            new object[] { Shipped with { MssqlMinimumIndexSizeKB = 8 }, "minimum index size" },
        };

        [Theory]
        [MemberData(nameof(ConstantPerturbations))]
        public void AChangedConstant_IsReported(Thresholds perturbed, string expectedProblem)
        {
            var problems = FindDisagreements(ReadViews("cumulative"), perturbed);

            Assert.Contains(problems, problem => problem.Contains(expectedProblem));
        }

        [Fact]
        public void TheMyGeotabAPIAdapterPreFixValues_AreReportedAsTheWrongScale()
        {
            // The API Adapter shipped 0.1 and 0.5 against these two percentage columns, meaning 10% and 50% and acting at
            // 0.1% and 0.5%. Neither value appears in DIG's views, so a check that only compared literals would have had
            // nothing to say; the scale check is what reports both.
            var preFix = Shipped with { MssqlStatsPct = 0.1, MssqlRebuildIndexPct = 0.5 };

            var problems = FindDisagreements(ReadViews("cumulative"), preFix);

            Assert.Contains(problems, problem => problem.Contains("statistics threshold against PctModsSinceLastStatsUpdate") && problem.Contains("PERCENTAGE"));
            Assert.Contains(problems, problem => problem.Contains("whole-index rebuild threshold against PctHighFragPartitions") && problem.Contains("PERCENTAGE"));
        }

        [Fact]
        public void EveryThreshold_NamesItsUnit_AndEachProviderUsesItsOwn()
        {
            // The per-provider unit rule of DigMaintenanceThresholds, enforced on every constant - including one added later.
            // A SQL Server view returns percentages and a PostgreSQL view returns ratios, so a PG_ "Pct" or an MSSQL_
            // "Ratio" is the unit written for the wrong provider.
            var constants = typeof(DigMaintenanceThresholds).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral)
                .ToList();
            Assert.NotEmpty(constants);

            var problems = new List<string>();
            foreach (var constant in constants)
            {
                var value = Convert.ToDouble(constant.GetRawConstantValue(), System.Globalization.CultureInfo.InvariantCulture);
                var units = new[] { "Pct", "Ratio", "Count", "Bytes", "KB" }.Where(unit => constant.Name.Contains(unit, StringComparison.Ordinal)).ToList();
                if (units.Count != 1)
                {
                    problems.Add($"{constant.Name}: the name must carry exactly one unit (Pct, Ratio, Count, Bytes or KB); it carries {units.Count}.");
                    continue;
                }
                var isPostgreSQL = constant.Name.StartsWith("PG_", StringComparison.Ordinal);
                var isSqlServer = constant.Name.StartsWith("MSSQL_", StringComparison.Ordinal);
                if (isPostgreSQL == isSqlServer)
                {
                    problems.Add($"{constant.Name}: the name must start with PG_ or MSSQL_.");
                }
                switch (units[0])
                {
                    case "Pct":
                        if (isPostgreSQL) problems.Add($"{constant.Name}: PostgreSQL's views return ratios, so a PostgreSQL threshold is never a percentage.");
                        if (value <= 1 || value > 100) problems.Add($"{constant.Name}: a percentage must lie above 1 and at most 100; it is {value}.");
                        break;
                    case "Ratio":
                        if (isSqlServer) problems.Add($"{constant.Name}: SQL Server's views return percentages, so a SQL Server threshold is never a ratio.");
                        if (value <= 0.01 || value > 1) problems.Add($"{constant.Name}: a ratio must lie above 0.01 and at most 1; it is {value}.");
                        break;
                    default:
                        if (value < 1 || value != Math.Floor(value)) problems.Add($"{constant.Name}: a {units[0]} must be a whole number of at least 1; it is {value}.");
                        break;
                }
            }

            Assert.True(problems.Count == 0, string.Join("\n", problems));
        }
    }
}
