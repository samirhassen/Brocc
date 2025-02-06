
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Enrichers;
using Serilog.Events;
using System;

namespace Serilog.Enrichers

{
    /// <summary>
    /// Enriches log events with a MachineName property containing <see cref="Environment.MachineName"/>.
    /// </summary>
    public class MachineNameEnricher : ILogEventEnricher
    {
        LogEventProperty _cachedProperty;
        /// <summary>
        /// The property name added to enriched log events.
        /// </summary>
        public const string MachineNamePropertyName = "MachineName";

        /// <summary>   
        /// Enrich the log event.
        /// </summary>
        /// <param name="logEvent">The log event to enrich.</param>
        /// <param name="propertyFactory">Factory for creating new properties to add to the event.</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
#if ENV_USER_NAME
            _cachedProperty = _cachedProperty ?? propertyFactory.CreateProperty(MachineNamePropertyName, Environment.MachineName);
#else
            var machineName = Environment.GetEnvironmentVariable("COMPUTERNAME");
            if (string.IsNullOrWhiteSpace(machineName))
                machineName = Environment.GetEnvironmentVariable("HOSTNAME");
           _cachedProperty = _cachedProperty ?? propertyFactory.CreateProperty(MachineNamePropertyName, machineName);
#endif
            logEvent.AddPropertyIfAbsent(_cachedProperty);
        }
    }
}
namespace Serilog

{
    /// <summary>
    /// Extends <see cref="LoggerConfiguration"/> to add enrichers for <see cref="System.Environment"/>.
    /// capabilities.
    /// </summary>

    public static class EnvironmentLoggerConfigurationExtensions

    {
        /// <summary>
        /// Enrich log events with a MachineName property containing the current <see cref="Environment.MachineName"/>.
 
        /// </summary>

        /// <param name="enrichmentConfiguration">Logger enrichment configuration.</param>

        /// <returns>Configuration object allowing method chaining.</returns>

        public static LoggerConfiguration WithMachineName(
           this LoggerEnrichmentConfiguration enrichmentConfiguration)

        {
            if (enrichmentConfiguration == null) throw new ArgumentNullException(nameof(enrichmentConfiguration));
            return enrichmentConfiguration.With<MachineNameEnricher>();
        }
    }
}