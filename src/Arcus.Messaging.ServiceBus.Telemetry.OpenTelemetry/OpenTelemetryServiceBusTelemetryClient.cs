using System.Collections.Concurrent;
using System.Diagnostics;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.Telemetry;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.ServiceBus.Telemetry.OpenTelemetry
{
    /// <summary>
    /// 
    /// </summary>
    public class OpenTelemetryServiceBusTelemetryClient : IAzureServiceBusTelemetryClient
    {
        private readonly ConcurrentDictionary<string, ActivitySource> _sources = new ConcurrentDictionary<string, ActivitySource>();

        /// <summary>
        /// Tracks an incoming Azure Service bus request that gets consumed by the application.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        public MessageCorrelationResult StartServiceBusRequest(
            ServiceBusReceiver receiver,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo currentCorrelation,
            MessageTelemetryOptions options)
        {
            ActivitySource source = _sources.GetOrAdd(options.OperationName, name => new ActivitySource(name));

            var context = new ActivityContext(
                ActivityTraceId.CreateFromString(currentCorrelation.TransactionId),
                ActivitySpanId.CreateFromString(currentCorrelation.OperationParentId),
                ActivityTraceFlags.None);

            Activity activity = source.CreateActivity(
                name: options.OperationName,
                kind: ActivityKind.Server,
                context);

            activity?.Start();

            var watch = Stopwatch.StartNew();
            MessageCorrelationInfo correlation = currentCorrelation;

            if (activity != null)
            {
                correlation = new MessageCorrelationInfo(
                    activity.SpanId.ToString(),
                    activity.TraceId.ToString(),
                    activity.ParentSpanId.ToString());

                activity.SetTag("az.namespace", "Microsoft.ServiceBus");
                activity.SetTag("messaging.system", "servicebus");
                activity.SetTag("messaging.operation", "receive");

                activity.SetTag("ServiceBus-Endpoint", receiver?.FullyQualifiedNamespace ?? "<not-available>");
                activity.SetTag("ServiceBus-Entity", receiver?.EntityPath ?? "<not-available>");
                activity.SetTag("ServiceBus-EntityType", (messageContext?.EntityType ?? ServiceBusEntityType.Unknown).ToString());
            }

            return new MessageCorrelationResult(correlation, (isSuccessful) =>
            {
                watch.Stop();

                if (activity != null)
                {
                    activity.SetStatus(isSuccessful ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
                    activity.SetEndTime(activity.StartTimeUtc.Add(watch.Elapsed));
                    activity.Dispose();
                }
            });
        }
    }
}
