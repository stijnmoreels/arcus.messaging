using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to add an <see cref="IAzureServiceBusMessageHandler{TMessage}"/>'s implementations.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class ServiceBusMessageHandlerCollectionExtensions
    {
        /// <summary>
        /// Adds an <see cref="ICircuitBreakerEventHandler"/> implementation for a specific message pump to the application services.
        /// </summary>
        /// <typeparam name="TEventHandler">The custom type of the event handler.</typeparam>
        /// <param name="collection">The application services to register the event handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection WithCircuitBreakerStateChangedEventHandler<TEventHandler>(
            this ServiceBusMessageHandlerCollection collection)
            where TEventHandler : ICircuitBreakerEventHandler
        {
            return WithCircuitBreakerStateChangedEventHandler(collection, provider => ActivatorUtilities.CreateInstance<TEventHandler>(provider));
        }

        /// <summary>
        /// Adds an <see cref="ICircuitBreakerEventHandler"/> implementation for a specific message pump to the application services.
        /// </summary>
        /// <typeparam name="TEventHandler">The custom type of the event handler.</typeparam>
        /// <param name="collection">The application services to register the event handler.</param>
        /// <param name="implementationFactory">The factory function to create the custom <see cref="ICircuitBreakerEventHandler"/> implementation.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/> or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection WithCircuitBreakerStateChangedEventHandler<TEventHandler>(
            this ServiceBusMessageHandlerCollection collection,
            Func<IServiceProvider, TEventHandler> implementationFactory)
            where TEventHandler : ICircuitBreakerEventHandler
        {
            if (collection is null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (implementationFactory is null)
            {
                throw new ArgumentNullException(nameof(implementationFactory));
            }

            collection.Services.AddCircuitBreakerEventHandler(collection.JobId, implementationFactory);
            return collection;
        }

        /// <summary>
        /// Adds a <see cref="IServiceBusMessageCorrelationScope"/> implementation for a specific message pump to the application services.
        /// </summary>
        /// <typeparam name="TScope">The custom correlation scope implementation to track Azure Service bus message requests.</typeparam>
        /// <param name="collection">The application services to register the message correlation scope.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection WithServiceBusRequestTracking<TScope>(this ServiceBusMessageHandlerCollection collection)
            where TScope : IServiceBusMessageCorrelationScope
        {
            return WithServiceBusRequestTracking(collection, provider => ActivatorUtilities.CreateInstance<TScope>(provider));
        }

        /// <summary>
        /// Adds a <see cref="IServiceBusMessageCorrelationScope"/> implementation for a specific message pump to the application services.
        /// </summary>
        /// <param name="collection">The application services to register the message correlation scope.</param>
        /// <param name="implementationFactory">The factory method to create the custom <see cref="IServiceBusMessageCorrelationScope"/> implementation.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/> or the <paramref name="implementationFactory"/> is <c>null</c>/</exception>
        public static ServiceBusMessageHandlerCollection WithServiceBusRequestTracking<TScope>(
            this ServiceBusMessageHandlerCollection collection,
            Func<IServiceProvider, TScope> implementationFactory)
            where TScope : IServiceBusMessageCorrelationScope
        {
            if (collection is null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (implementationFactory is null)
            {
                throw new ArgumentNullException(nameof(implementationFactory));
            }

            collection.Services.AddSingleton<IServiceBusMessageCorrelationScope>(provider => implementationFactory(provider));
            return collection;
        }
    }

    /// <summary>
    /// Represents the way how incoming Azure Service bus request messages
    /// within a service-to-service correlation operation are tracked in a custom telemetry system,.
    /// </summary>
    public interface IServiceBusMessageCorrelationScope
    {
        /// <summary>
        /// Starts a new Azure Service bus request operation on the telemetry system.
        /// </summary>
        /// <param name="messageContext">The message context for the currently received Azure Service bus message.</param>
        /// <param name="options">The user-configurable options to manipulate the telemetry.</param>
        MessageCorrelationResult StartOperation(AzureServiceBusMessageContext messageContext, ServiceBusMessageTelemetryOptions options);
    }

    /// <summary>
    /// Represents an <see cref="IServiceBusMessageCorrelationScope"/> that tracks the incoming Azure Service bus request message
    /// with the Serilog-registered service-to-service telemetry system.
    /// </summary>
    public class SerilogServiceBusMessageCorrelationScope : IServiceBusMessageCorrelationScope
    {
        private readonly TelemetryClient _client;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogServiceBusMessageCorrelationScope"/> class.
        /// </summary>
        public SerilogServiceBusMessageCorrelationScope(
            TelemetryClient client,
            ILogger<SerilogServiceBusMessageCorrelationScope> logger)
        {
            _client = client;
            _logger = logger;
        }

        /// <summary>
        /// Starts a new Azure Service bus request operation on the telemetry system.
        /// </summary>
        /// <param name="messageContext">The message context for the currently received Azure Service bus message.</param>
        /// <param name="options">The user-configurable options to manipulate the telemetry.</param>
        public MessageCorrelationResult StartOperation(AzureServiceBusMessageContext messageContext, ServiceBusMessageTelemetryOptions options)
        {
            (string transactionId, string operationParentId) = messageContext.Properties.GetTraceParent();

            var telemetry = new RequestTelemetry();
            telemetry.Context.Operation.Id = transactionId;
            telemetry.Context.Operation.ParentId = operationParentId;

            IOperationHolder<RequestTelemetry> operationHolder = _client.StartOperation(telemetry);
            var correlationInfo = new MessageCorrelationInfo(telemetry.Id, transactionId, operationParentId);

            return new SerilogMessageCorrelationResult(messageContext, options, correlationInfo, _client, operationHolder, _logger);
        }

        private sealed class SerilogMessageCorrelationResult : MessageCorrelationResult
        {
            private readonly AzureServiceBusMessageContext _messageContext;
            private readonly ServiceBusMessageTelemetryOptions _options;
            private readonly TelemetryClient _client;
            private readonly IOperationHolder<RequestTelemetry> _operation;
            private readonly ILogger _logger;

            /// <summary>
            /// Initializes a new instance of the <see cref="SerilogMessageCorrelationResult"/> class.
            /// </summary>
            internal SerilogMessageCorrelationResult(
                AzureServiceBusMessageContext messageContext,
                ServiceBusMessageTelemetryOptions options,
                MessageCorrelationInfo correlation,
                TelemetryClient client,
                IOperationHolder<RequestTelemetry> operation,
                ILogger logger) : base(correlation)
            {
                _messageContext = messageContext;
                _options = options;
                _client = client;
                _operation = operation;
                _logger = logger;
            }

            /// <summary>
            /// Finalizes the tracked operation in the concrete telemetry system, based on the operation results.
            /// </summary>
            /// <param name="isSuccessful">The boolean flag to indicate whether the operation was successful.</param>
            /// <param name="startTime">The date when the operation started.</param>
            /// <param name="duration">The time it took for the operation to run.</param>
            protected override void StopOperation(bool isSuccessful, DateTimeOffset startTime, TimeSpan duration)
            {
                _logger.LogServiceBusRequest(
                    _messageContext.FullyQualifiedNamespace,
                    _messageContext.EntityPath,
                    _options.OperationName,
                    isSuccessful, duration, startTime,
                    _messageContext.EntityType);
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                _client.TelemetryConfiguration.DisableTelemetry = true;
                _operation.Dispose();
                _client.TelemetryConfiguration.DisableTelemetry = false;
            }
        }
    }

    /// <summary>
    /// Represents an <see cref="IServiceBusMessageCorrelationScope"/> that tracks the incoming Azure Service bus request message
    /// with the OpenTelemetry-registered service-to-service telemetry system.
    /// </summary>
    public class OpenTelemetryServiceBusMessageCorrelationScope : IServiceBusMessageCorrelationScope
    {
        private readonly ConcurrentDictionary<string, ActivitySource> _sources = new ConcurrentDictionary<string, ActivitySource>();

        /// <summary>
        /// Starts a new Azure Service bus request operation on the telemetry system.
        /// </summary>
        /// <param name="messageContext">The message context for the currently received Azure Service bus message.</param>
        /// <param name="options">The user-configurable options to manipulate the telemetry.</param>
        public MessageCorrelationResult StartOperation(AzureServiceBusMessageContext messageContext, ServiceBusMessageTelemetryOptions options)
        {
            ActivitySource source = _sources.GetOrAdd(options.OperationName, name => new ActivitySource(name));

            (string transactionId, string operationParentId) = messageContext.Properties.GetTraceParent();
            var context = new ActivityContext(
                ActivityTraceId.CreateFromString(transactionId),
                ActivitySpanId.CreateFromString(operationParentId),
                ActivityTraceFlags.None);

            Activity activity = source.CreateActivity(
                name: options.OperationName,
                kind: ActivityKind.Server,
                context);

            if (activity != null)
            {
                activity.Start();
                var correlation = new MessageCorrelationInfo(
                    activity.SpanId.ToString(),
                    activity.TraceId.ToString(),
                    activity.ParentSpanId.ToString());

                activity.SetTag("az.namespace", "Microsoft.ServiceBus");
                activity.SetTag("messaging.system", "servicebus");
                activity.SetTag("messaging.operation", "receive");

                activity.SetTag("ServiceBus-Endpoint", messageContext.FullyQualifiedNamespace);
                activity.SetTag("ServiceBus-Entity", messageContext.EntityPath);
                activity.SetTag("ServiceBus-EntityType", (messageContext?.EntityType).ToString());

                return new OpenTelemetryMessageCorrelationResult(activity, correlation);
            }

            return new OpenTelemetryMessageCorrelationResult(
                activity: null,
                new MessageCorrelationInfo(Guid.NewGuid().ToString(), transactionId, operationParentId));
        }

        private sealed class OpenTelemetryMessageCorrelationResult : MessageCorrelationResult
        {
            private readonly Activity _activity;

            /// <summary>
            /// Initializes a new instance of the <see cref="OpenTelemetryMessageCorrelationResult"/> class.
            /// </summary>
            internal OpenTelemetryMessageCorrelationResult(Activity activity, MessageCorrelationInfo correlation) : base(correlation)
            {
                _activity = activity;
            }

            protected override void StopOperation(bool isSuccessful, DateTimeOffset startTime, TimeSpan duration)
            {
                if (_activity != null)
                {
                    _activity.SetStatus(isSuccessful ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
                    _activity.SetEndTime(_activity.StartTimeUtc.Add(duration));
                    _activity.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Represents the user-configurable options to manipulate telemetry-related configuration.
    /// </summary>
    public class ServiceBusMessageTelemetryOptions
    {
        private string _operationName;

        /// <summary>
        /// Gets or sets the name of the operation that is used when a request telemetry is tracked.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string OperationName
        {
            get => _operationName;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Requires a non-blank operation name", nameof(value));
                }

                _operationName = value;
            }
        }
    }
}
