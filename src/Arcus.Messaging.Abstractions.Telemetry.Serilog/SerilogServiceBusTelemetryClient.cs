using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus.Telemetry;
using Arcus.Observability.Telemetry.Core;
using Azure.Messaging.ServiceBus;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Arcus.Messaging.Abstractions.Telemetry
{
    /// <summary>
    /// 
    /// </summary>
    public class SerilogServiceBusTelemetryClient : IAzureServiceBusTelemetryClient
    {
        private readonly TelemetryClient _client;
        private readonly MessageCorrelationEnricherOptions _options;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogServiceBusTelemetryClient" /> class.
        /// </summary>
        public SerilogServiceBusTelemetryClient(
            TelemetryClient client,
            MessageCorrelationEnricherOptions options,
            ILoggerFactory loggerFactory)
        {
            _client = client;
            _options = options;
            _logger = loggerFactory.CreateLogger<AzureServiceBusMessageRouter>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public MessageCorrelationResult StartServiceBusRequest(
            ServiceBusReceiver receiver,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo currentCorrelation,
            MessageTelemetryOptions options)
        {
            var telemetry = new RequestTelemetry();
            telemetry.Id = currentCorrelation.OperationId;
            telemetry.Context.Operation.Id = currentCorrelation.TransactionId;
            telemetry.Context.Operation.ParentId = currentCorrelation.OperationParentId;

            IOperationHolder<RequestTelemetry> operationHolder = _client.StartOperation(telemetry);
            var newCorrelation = new MessageCorrelationInfo(
                operationHolder.Telemetry.Id, currentCorrelation.TransactionId, currentCorrelation.OperationParentId);

            IDisposable disposable = LogContext.Push(new MessageCorrelationInfoEnricher(newCorrelation, _options));
            var measurement = DurationMeasurement.Start();

            return new MessageCorrelationResult(currentCorrelation, (isSuccessful) =>
            {
                _logger.LogServiceBusRequest(
                    receiver?.FullyQualifiedNamespace ?? "<not-available>",
                    receiver?.EntityPath ?? "<not-available>",
                    options.OperationName,
                    isSuccessful,
                    measurement,
                    messageContext?.EntityType ?? ServiceBusEntityType.Unknown);

                measurement.Dispose();

                _client.TelemetryConfiguration.DisableTelemetry = true;
                operationHolder.Dispose();
                _client.TelemetryConfiguration.DisableTelemetry = false;

                disposable.Dispose();
            });
        }
    }
}