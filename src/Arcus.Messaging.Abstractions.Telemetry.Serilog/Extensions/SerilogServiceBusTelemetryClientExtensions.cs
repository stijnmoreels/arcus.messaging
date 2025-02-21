using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus.Telemetry;
using Arcus.Messaging.Abstractions.Telemetry;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions related to using Serilog as backend system for correlation tracking.
    /// </summary>
    public static class SerilogServiceBusTelemetryClientExtensions
    {
        /// <summary>
        /// Adds the original Serilog correlation backend system that was provided by Arcus.
        /// </summary>
        public static IServiceCollection AddSerilogServiceBusTelemetry(this IServiceCollection services)
        {
            return AddSerilogServiceBusTelemetry(services, configureOptions: null);
        }

        /// <summary>
        /// Adds the original Serilog correlation backend system that was provided by Arcus.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configureOptions">The additional options to control the behavior of the tracking.</param>
        public static IServiceCollection AddSerilogServiceBusTelemetry(this IServiceCollection services, Action<MessageCorrelationEnricherOptions> configureOptions)
        {
            return services.AddSingleton<IAzureServiceBusTelemetryClient>(provider =>
            {
                var options = new MessageCorrelationEnricherOptions();
                configureOptions?.Invoke(options);

                return new SerilogServiceBusTelemetryClient(
                    provider.GetRequiredService<TelemetryClient>(),
                    options,
                    provider.GetRequiredService<ILoggerFactory>());
            });
        }
    }
}