﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.Telemetry;
using Arcus.Observability.Telemetry.Core;
using Azure.Messaging.EventHubs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Context;
using ILogger = Microsoft.Extensions.Logging.ILogger;

#pragma warning disable CS0618 // Type or member is obsolete: EventHubs-related projects will be removed anyway.

namespace Arcus.Messaging.Abstractions.EventHubs.MessageHandling
{
    /// <summary>
    /// Represents an <see cref="IMessageRouter"/> that can route Azure EventHubs <see cref="EventData"/>s.
    /// </summary>
    public class AzureEventHubsMessageRouter : MessageRouter, IAzureEventHubsMessageRouter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureEventHubsMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IAzureEventHubsMessageHandler{TMessage}"/> instances.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        public AzureEventHubsMessageRouter(IServiceProvider serviceProvider)
            : this(serviceProvider, NullLogger<AzureEventHubsMessageRouter>.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureEventHubsMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IAzureEventHubsMessageHandler{TMessage}"/> instances.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        public AzureEventHubsMessageRouter(IServiceProvider serviceProvider, ILogger<AzureEventHubsMessageRouter> logger)
            : this(serviceProvider, new AzureEventHubsMessageRouterOptions(), logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureEventHubsMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IAzureEventHubsMessageHandler{TMessage}"/> instances.</param>
        /// <param name="options">The consumer-configurable options to change the behavior of the router.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        public AzureEventHubsMessageRouter(IServiceProvider serviceProvider, AzureEventHubsMessageRouterOptions options)
            : this(serviceProvider, options, NullLogger<AzureEventHubsMessageRouter>.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureEventHubsMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IAzureEventHubsMessageHandler{TMessage}"/> instances.</param>
        /// <param name="options">The consumer-configurable options to change the behavior of the router.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        public AzureEventHubsMessageRouter(IServiceProvider serviceProvider, AzureEventHubsMessageRouterOptions options, ILogger<AzureEventHubsMessageRouter> logger)
            : this(serviceProvider, options, (ILogger) logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureEventHubsMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IAzureEventHubsMessageHandler{TMessage}"/> instances.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        protected AzureEventHubsMessageRouter(IServiceProvider serviceProvider, ILogger logger)
            : this(serviceProvider, new AzureEventHubsMessageRouterOptions(), logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureEventHubsMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IAzureEventHubsMessageHandler{TMessage}"/> instances.</param>
        /// <param name="options">The consumer-configurable options to change the behavior of the router.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        protected AzureEventHubsMessageRouter(IServiceProvider serviceProvider, AzureEventHubsMessageRouterOptions options, ILogger logger)
            : base(serviceProvider, options, logger)
        {
            EventHubsOptions = options ?? new AzureEventHubsMessageRouterOptions();
        }

        /// <summary>
        /// Gets the consumer-configurable options to change the behavior of the Azure Service Bus router.
        /// </summary>
        protected AzureEventHubsMessageRouterOptions EventHubsOptions { get; }

        /// <summary>
        /// Handle a new <paramref name="message"/> that was received by routing them through registered <see cref="IAzureEventHubsMessageHandler{TMessage}"/>s
        /// and optionally through an registered <see cref="IFallbackMessageHandler"/> if none of the message handlers were able to process the <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        /// <param name="messageContext">The context providing more information concerning the processing.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when no message handlers or none matching message handlers are found to process the message.</exception>
        public async Task RouteMessageAsync(
            EventData message,
            AzureEventHubsMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            string messageBody = message.Data.ToString();
            await RouteMessageAsync(messageBody, messageContext, correlationInfo, cancellationToken);
        }

        /// <summary>
        /// Handle a new <paramref name="message"/> that was received by routing them through registered <see cref="IMessageHandler{TMessage,TMessageContext}"/>s
        /// and optionally through an registered <see cref="IFallbackMessageHandler"/> if none of the message handlers were able to process the <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        /// <param name="messageContext">The context providing more information concerning the processing.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when no message handlers or none matching message handlers are found to process the message.</exception>
#pragma warning disable CS0672 // Member overrides obsolete member: ventHubs-related projects will be removed anyway.
        public override async Task RouteMessageAsync<TMessageContext>(
#pragma warning restore CS0672 // Member overrides obsolete member
            string message,
            TMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            var isSuccessful = false;
            using (DurationMeasurement measurement = DurationMeasurement.Start())
            using (IServiceScope serviceScope = ServiceProvider.CreateScope())
#pragma warning disable CS0618 // Type or member is obsolete: EventHubs-functionality will be removed in v3.0 anyway.
            using (LogContext.Push(new MessageCorrelationInfoEnricher(correlationInfo, Options.CorrelationEnricher)))
#pragma warning restore CS0618 // Type or member is obsolete
            {
                try
                {
                    var accessor = serviceScope.ServiceProvider.GetService<IMessageCorrelationInfoAccessor>();
                    accessor?.SetCorrelationInfo(correlationInfo);

#pragma warning disable CS0618 // Type or member is obsolete: EventHubs-functionality will be removed in v3.0 anyway.
                    await RouteMessageAsync(serviceScope.ServiceProvider, message, messageContext, correlationInfo, cancellationToken);
#pragma warning restore CS0618 // Type or member is obsolete
                    isSuccessful = true;
                }
                finally
                {
                    string eventHubsNamespace = "<not-available>";
                    string consumerGroup = "<not-available>";
                    string eventHubsName = "<not-available>";

                    if (messageContext is AzureEventHubsMessageContext context)
                    {
                        eventHubsNamespace = context.EventHubsNamespace;
                        consumerGroup = context.ConsumerGroup;
                        eventHubsName = context.EventHubsName;
                    }

                    Logger.LogEventHubsRequest(
                        eventHubsNamespace,
                        consumerGroup,
                        eventHubsName,
                        Options.Telemetry.OperationName,
                        isSuccessful,
                        measurement);
                }
            }
        }
    }
}
