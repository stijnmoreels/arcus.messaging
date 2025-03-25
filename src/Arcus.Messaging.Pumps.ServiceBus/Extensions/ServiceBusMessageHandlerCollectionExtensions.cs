using System;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;

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
        public static ServiceBusMessageHandlerCollection WithServiceBusRequestTracking(
            this ServiceBusMessageHandlerCollection collection,
            Func<IServiceProvider, IServiceBusMessageCorrelationScope> implementationFactory)
        {
            if (collection is null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (implementationFactory is null)
            {
                throw new ArgumentNullException(nameof(implementationFactory));
            }

            collection.Services.AddSingleton(provider => new ServiceBusMessageCorrelationScopeRegistration(implementationFactory(provider), collection.JobId));
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
        /// <returns></returns>
        MessageCorrelationResult StartOperation(AzureServiceBusMessageContext messageContext, ServiceBusMessageTelemetryOptions options);
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

    internal class ServiceBusMessageCorrelationScopeRegistration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusMessageCorrelationScopeRegistration"/> class.
        /// </summary>
        public ServiceBusMessageCorrelationScopeRegistration(
            IServiceBusMessageCorrelationScope correlationScope,
            string jobId)
        {
            CorrelationScope = correlationScope;
            JobId = jobId;
        }

        public string JobId { get; }
        public IServiceBusMessageCorrelationScope CorrelationScope { get; }
    }
}
