using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Abstractions.ServiceBus
{
    /// <summary>
    /// Represents the contextual information concerning an Azure Service Bus message.
    /// </summary>
    public class AzureServiceBusMessageContext : MessageContext
    {
        private readonly ServiceBusReceiver _receiver;
        private readonly ServiceBusReceivedMessage _message;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessageContext"/> class.
        /// </summary>
        /// <param name="messageId">The unique identifier of the message.</param>
        /// <param name="jobId">Unique identifier of the message pump.</param>
        /// <param name="systemProperties">The contextual properties provided on the message provided by the Azure Service Bus runtime.</param>
        /// <param name="properties">The contextual properties provided on the message provided by the message publisher.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="systemProperties"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        [Obsolete("Will be removed in v3.0, please use the factory method instead: " + nameof(AzureServiceBusMessageContext) + "." + nameof(Create))]
        public AzureServiceBusMessageContext(
            string messageId,
            string jobId,
            AzureServiceBusSystemProperties systemProperties,
            IReadOnlyDictionary<string, object> properties)
            : this(messageId, jobId, systemProperties, properties, ServiceBusEntityType.Unknown)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessageContext"/> class.
        /// </summary>
        /// <param name="messageId">The unique identifier of the message.</param>
        /// <param name="jobId">Unique identifier of the message pump.</param>
        /// <param name="systemProperties">The contextual properties provided on the message provided by the Azure Service Bus runtime.</param>
        /// <param name="properties">The contextual properties provided on the message provided by the message publisher.</param>
        /// <param name="entityType">The type of the Azure Service Bus entity on which a message with the ID <paramref name="messageId"/> was received.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="systemProperties"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        [Obsolete("Will be removed in v3.0, please use the factory method instead: " + nameof(AzureServiceBusMessageContext) + "." + nameof(Create))]
        public AzureServiceBusMessageContext(
            string messageId,
            string jobId,
            AzureServiceBusSystemProperties systemProperties,
            IReadOnlyDictionary<string, object> properties,
            ServiceBusEntityType entityType)
            : base(messageId, jobId, properties?.ToDictionary(item => item.Key, item => item.Value) ?? throw new ArgumentNullException(nameof(properties)))
        {
            if (systemProperties is null)
            {
                throw new ArgumentNullException(nameof(systemProperties));
            }

            SystemProperties = systemProperties;
            LockToken = systemProperties.LockToken;
            DeliveryCount = systemProperties.DeliveryCount;
            EntityType = entityType;
        }

        private AzureServiceBusMessageContext(
            string jobId,
            ServiceBusEntityType entityType,
            ServiceBusReceiver receiver,
            ServiceBusReceivedMessage message)
            : base(message.MessageId, jobId, message.ApplicationProperties.ToDictionary(item => item.Key, item => item.Value))
        {
            _receiver = receiver;
            _message = message;

            FullyQualifiedNamespace = receiver.FullyQualifiedNamespace;
            EntityPath = receiver.EntityPath;
            EntityType = entityType;
            SystemProperties = AzureServiceBusSystemProperties.CreateFrom(message);
            LockToken = message.LockToken;
            DeliveryCount = message.DeliveryCount;
        }

        /// <summary>
        /// Gets the fully qualified Azure Service bus namespace that the message pump is associated with.
        /// This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </summary>
        public string FullyQualifiedNamespace { get; }

        /// <summary>
        /// Gets the path of the Azure Service bus entity that the message pump is connected to,
        /// specific to the Azure Service bus namespace that contains it.
        /// </summary>
        public string EntityPath { get; }

        /// <summary>
        /// Gets the type of the Azure Service Bus entity on which the message was received.
        /// </summary>
        public ServiceBusEntityType EntityType { get; }

        /// <summary>
        /// Gets the contextual properties provided on the message provided by the Azure Service Bus runtime
        /// </summary>
        public AzureServiceBusSystemProperties SystemProperties { get; }

        /// <summary>
        /// Gets the token used to lock an individual message for processing
        /// </summary>
        public string LockToken { get; }

        /// <summary>
        /// Gets the amount of times a message was delivered
        /// </summary>
        /// <remarks>This increases when a message is abandoned and re-delivered for processing</remarks>
        public int DeliveryCount { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="AzureServiceBusMessageContext"/> based on the current Azure Service bus situation.
        /// </summary>
        /// <param name="jobId">The unique ID to identity the Azure Service bus message pump that is responsible for pumping messages from the <paramref name="receiver"/>.</param>
        /// <param name="entityType">The type of Azure Service bus entity that the <paramref name="receiver"/> receives from.</param>
        /// <param name="receiver">The Azure Service bus receiver that is responsible for receiving the <paramref name="message"/>.</param>
        /// <param name="message">The Azure Service bus message that is currently being processed.</param>
        /// <exception cref="ArgumentNullException">Thrown when one of the parameters is <c>null</c>.</exception>
        public static AzureServiceBusMessageContext Create(
            string jobId,
            ServiceBusEntityType entityType,
            ServiceBusReceiver receiver,
            ServiceBusReceivedMessage message)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Requires a non-blank job ID to identity an Azure Service bus message pump", nameof(jobId));
            }

            if (receiver is null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return new AzureServiceBusMessageContext(jobId, entityType, receiver, message);
        }

        /// <summary>
        /// Completes the Azure Service Bus message on Azure. This will delete the message from the service.
        /// </summary>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.</param>
        public async Task CompleteMessageAsync(CancellationToken cancellationToken = default)
        {
            await _receiver.CompleteMessageAsync(_message, cancellationToken);
        }

        /// <summary>
        /// Abandons an Azure Service bus on Azure.
        /// This will make the message available again for immediate processing as the lock on the message held by the receiver will be released.
        /// </summary>
        /// <param name="cancellationToken">An optional <see cref="T:System.Threading.CancellationToken" /> instance to signal the request to cancel the operation.</param>
        public async Task AbandonMessageAsync(CancellationToken cancellationToken = default)
        {
            await _receiver.AbandonMessageAsync(_message, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Abandons an Azure Service bus on Azure.
        /// This will make the message available again for immediate processing as the lock on the message held by the receiver will be released.
        /// </summary>
        /// <param name="propertiesToModify">The properties of the message to modify while abandoning the message.</param>
        /// <param name="cancellationToken">An optional <see cref="T:System.Threading.CancellationToken" /> instance to signal the request to cancel the operation.</param>
        public async Task AbandonMessageAsync(IDictionary<string, object> propertiesToModify, CancellationToken cancellationToken = default)
        {
            await _receiver.AbandonMessageAsync(_message, propertiesToModify, cancellationToken);
        }

        /// <summary>
        /// Dead letters the Azure Service bus message on Azure.
        /// </summary>
        /// <param name="deadLetterReason">The reason for dead-lettering the message.</param>
        /// <param name="deadLetterErrorDescription">The error description for dead-lettering the message.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.</param>
        public async Task DeadLetterMessageAsync(
            string deadLetterReason,
            string deadLetterErrorDescription,
            CancellationToken cancellationToken = default)
        {
            await _receiver.DeadLetterMessageAsync(_message, deadLetterReason, deadLetterErrorDescription, cancellationToken);
        }

        /// <summary>
        /// Dead letters the Azure Service bus message on Azure.
        /// </summary>
        /// <param name="deadLetterReason">The reason for dead-lettering the message.</param>
        /// <param name="deadLetterErrorDescription">The error description for dead-lettering the message.</param>
        /// <param name="propertiesToModify">The properties of the message to modify while moving to sub-queue.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.</param>
        public async Task DeadLetterMessageAsync(
            string deadLetterReason,
            string deadLetterErrorDescription,
            IDictionary<string, object> propertiesToModify,
            CancellationToken cancellationToken = default)
        {
            await _receiver.DeadLetterMessageAsync(_message, propertiesToModify, deadLetterReason, deadLetterErrorDescription, cancellationToken);
        }
    }
}