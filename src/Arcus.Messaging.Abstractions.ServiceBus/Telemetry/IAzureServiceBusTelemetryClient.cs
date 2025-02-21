using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Azure.Messaging.ServiceBus;

namespace Arcus.Messaging.Abstractions.ServiceBus.Telemetry
{
    /// <summary>
    /// Represents a way to track correlated Azure Service bus telemetry.
    /// </summary>
    public interface IAzureServiceBusTelemetryClient
    {
        /// <summary>
        /// Tracks an incoming Azure Service bus request that gets consumed by the application.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        public MessageCorrelationResult StartServiceBusRequest(
            ServiceBusReceiver receiver,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo currentCorrelation,
            MessageTelemetryOptions options);
    }
}