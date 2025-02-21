using Arcus.Messaging.Abstractions.ServiceBus.Telemetry;
using Arcus.Messaging.ServiceBus.Telemetry.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace OpenTelemetry.Trace
{
    public static class OpenTelemetryTraceProviderExtensions
    {
        /// <summary>
        /// Adds OpenTelemetry correlation backend system provided by Arcus.
        /// This is only necessary when there is no built-in Microsoft observability available.
        /// </summary>
        public static TracerProviderBuilder AddServiceBusInstrumentation(this TracerProviderBuilder traces)
        {
            return traces.ConfigureServices(services =>
            {
                services.TryAddSingleton<IAzureServiceBusTelemetryClient, OpenTelemetryServiceBusTelemetryClient>();
            });
        }
    }
}
