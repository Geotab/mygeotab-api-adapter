using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using MyGeotabAPIAdapter.Configuration.Add_Ons.VSS;
using MyGeotabAPIAdapter.Helpers;
using NLog;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MyGeotabAPIAdapter.Add_Ons.VSS
{
    /// <summary>
    /// A class that assists with tracking and logging of <see cref="UnmappedDiagnostic"/>s. Generated log entries may be evaluated in order to determine whether there are additional <see cref="Diagnostic"/>s that should be mapped in the VSSPathMaps file.
    /// </summary>
    internal class UnmappedDiagnosticManager : IUnmappedDiagnosticManager
    {
        readonly IDateTimeHelper dateTimeHelper;
        readonly IGenericGeotabObjectHydrator<Diagnostic> diagnosticGeotabObjectHydrator;
        readonly IVSSConfiguration vssConfiguration;

        static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public IDictionary<Id, UnmappedDiagnostic> UnmappedDiagnosticsDictionary { get; }
        public DateTime UnmappedDiagnosticsLastLoggedTimeUtc { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="UnmappedDiagnosticManager"/> class.
        /// </summary>
        public UnmappedDiagnosticManager(IDateTimeHelper dateTimeHelper, IGenericGeotabObjectHydrator<Diagnostic> diagnosticGeotabObjectHydrator, IVSSConfiguration vssConfiguration)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.dateTimeHelper = dateTimeHelper;
            this.diagnosticGeotabObjectHydrator = diagnosticGeotabObjectHydrator;
            this.vssConfiguration = vssConfiguration;

            UnmappedDiagnosticsDictionary = new Dictionary<Id, UnmappedDiagnostic>();
            UnmappedDiagnosticsLastLoggedTimeUtc = DateTime.MinValue;

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public void AddUnmappedDiagnosticToDictionary(StatusData statusData)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            if (statusData.Diagnostic != null && statusData.Diagnostic.Id != null)
            {
                // Get and hydrate the StatusData's Diagnostic.
                Diagnostic diagnostic = diagnosticGeotabObjectHydrator.HydrateEntity(statusData.Diagnostic, NoDiagnostic.Value);

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

        /// <inheritdoc/>
        public void LogUnmappedDiagnostics()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // Only proeed if UnmappedDiagnostics are configured to be logged and the configured time interval has elapsed since the last time they were logged.
            if (vssConfiguration.LogUnmappedDiagnostics == true && dateTimeHelper.TimeIntervalHasElapsed(UnmappedDiagnosticsLastLoggedTimeUtc, DateTimeIntervalType.Minutes, vssConfiguration.UnmappedDiagnosticsLogIntervalMinutes))
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
