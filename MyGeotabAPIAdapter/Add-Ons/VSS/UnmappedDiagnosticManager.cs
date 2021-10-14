using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using NLog;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MyGeotabAPIAdapter.Add_Ons.VSS
{
    /// <summary>
    /// Assists with tracking and logging of <see cref="UnmappedDiagnostic"/>s. Generated log entries may be evaluated in order to determine whether there are additional <see cref="Diagnostic"/>s that should be mapped in the VSSPathMaps file.
    /// </summary>
    internal class UnmappedDiagnosticManager
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public IDictionary<Id, UnmappedDiagnostic> UnmappedDiagnosticsDictionary { get; }
        public DateTime UnmappedDiagnosticsLastLoggedTimeUtc { get; set; }

        readonly VSSConfiguration VSSConfiguration;

        /// <summary>
        /// Creates a new instance of the <see cref="UnmappedDiagnosticManager"/> class.
        /// </summary>
        /// <param name="vssConfiguration">The <see cref="VSS.VSSConfiguration"/> to use.</param>
        public UnmappedDiagnosticManager(VSSConfiguration vssConfiguration)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            UnmappedDiagnosticsDictionary = new Dictionary<Id, UnmappedDiagnostic>();
            UnmappedDiagnosticsLastLoggedTimeUtc = DateTime.MinValue;
            VSSConfiguration = vssConfiguration;

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Extracts the <see cref="StatusData.Diagnostic"/> information from the <paramref name="statusData"/> and adds/updates the <see cref="UnmappedDiagnosticsDictionary"/>. 
        /// </summary>
        /// <param name="statusData"></param>
        public void AddUnmappedDiagnosticToDictionary(StatusData statusData)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            if (statusData.Diagnostic != null && statusData.Diagnostic.Id != null)
            {
                // Get and hydrate the StatusData's Diagnostic.
                Diagnostic diagnostic = CacheManager.HydrateDiagnostic(statusData.Diagnostic);

                if (UnmappedDiagnosticsDictionary.ContainsKey(diagnostic.Id))
                {
                    // Update exsiting UnmappedDiagnostic in Dictionary.
                    UnmappedDiagnosticsDictionary[diagnostic.Id].OccurrencesSinceApplicationStartup += 1;
                    if (statusData.Data != null)
                    {
                        UnmappedDiagnosticsDictionary[diagnostic.Id].SampleValueString = statusData.Data.ToString();
                    }
                }
                else
                {
                    // Add new UnmappedDiagnostic to Dictionary.
                    UnmappedDiagnostic unmappedDiagnostic = new()
                    {
                        DiagnosticId = diagnostic.Id.ToString(),
                        DiagnosticName = diagnostic.Name is null ? string.Empty : diagnostic.Name,
                        OccurrencesSinceApplicationStartup = 1,
                        SampleValueString = statusData.Data is null ? string.Empty : statusData.Data.ToString()
                    };
                    UnmappedDiagnosticsDictionary.Add(diagnostic.Id, unmappedDiagnostic);
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Write information about all UnmappedDiagnostics that have been collected since application startup to the log file.
        /// </summary>
        public void LogUnmappedDiagnostics()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // Only proeed if UnmappedDiagnostics are configured to be logged and the configured time interval has elapsed since the last time they were logged.
            if (VSSConfiguration.LogUnmappedDiagnostics == true && Globals.TimeIntervalHasElapsed(UnmappedDiagnosticsLastLoggedTimeUtc, Globals.DateTimeIntervalType.Minutes, VSSConfiguration.UnmappedDiagnosticsLogIntervalMinutes))
            {
                logger.Info($"******** Begin list of unmapped diagnostics:");
                foreach (var unmappedDiagnostic in UnmappedDiagnosticsDictionary.Values)
                {
                    logger.Info($"DiagnosticId { unmappedDiagnostic.DiagnosticId }|DiagnosticName {unmappedDiagnostic.DiagnosticName}|OccurrencesSinceApplicationStartup {unmappedDiagnostic.OccurrencesSinceApplicationStartup}|SampleValueString {unmappedDiagnostic.SampleValueString}");
                }
                logger.Info($"******** End list of unmapped diagnostics");

                UnmappedDiagnosticsLastLoggedTimeUtc = DateTime.UtcNow;
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
