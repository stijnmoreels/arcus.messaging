using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Messaging;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Azure.Messaging.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Represents the available options when registering an <see cref="IAzureServiceBusMessageHandler{TMessage}"/>.
    /// </summary>
    /// <typeparam name="TMessage">The custom message type to handler.</typeparam>
    public class ServiceBusMessageHandlerOptions<TMessage>
    {
        private readonly Collection<Func<TMessage, bool>> _messageBodyFilters = [];
        private readonly Collection<Func<AzureServiceBusMessageContext, bool>> _messageContextFilters = [];

        internal Func<IServiceProvider, IServiceBusMessageBodyDeserializer> MessageBodySerializerImplementationFactory { get; private set; }
        internal Func<TMessage, bool> MessageBodyFilter => _messageBodyFilters.Count is 0 ? null : msg => _messageBodyFilters.All(filter => filter(msg));
        internal Func<AzureServiceBusMessageContext, bool> MessageContextFilter => _messageContextFilters.Count is 0 ? null : ctx => _messageContextFilters.All(filter => filter(ctx));

        /// <summary>
        /// Adds a custom serializer instance that deserializes the incoming <see cref="ServiceBusReceivedMessage.Body"/>.
        /// </summary>
        /// <typeparam name="TDeserializer">The custom <see cref="IServiceBusMessageBodyDeserializer"/> type load from the application services.</typeparam>
        public ServiceBusMessageHandlerOptions<TMessage> UseMessageBodyDeserializer<TDeserializer>()
            where TDeserializer : IServiceBusMessageBodyDeserializer
        {
            return UseMessageBodyDeserializer(serviceProvider => serviceProvider.GetRequiredService<TDeserializer>());
        }

        /// <summary>
        /// Adds a custom serializer instance that deserializes the incoming <see cref="ServiceBusReceivedMessage.Body"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serializer"/> is <c>null</c>.</exception>
        public ServiceBusMessageHandlerOptions<TMessage> UseMessageBodyDeserializer(IServiceBusMessageBodyDeserializer serializer)
        {
            ArgumentNullException.ThrowIfNull(serializer);
            return UseMessageBodyDeserializer(_ => serializer);
        }

        /// <summary>
        /// Adds a custom serializer instance that deserializes the incoming <see cref="ServiceBusReceivedMessage.Body"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public ServiceBusMessageHandlerOptions<TMessage> UseMessageBodyDeserializer(Func<IServiceProvider, IServiceBusMessageBodyDeserializer> implementationFactory)
        {
            ArgumentNullException.ThrowIfNull(implementationFactory);
            MessageBodySerializerImplementationFactory = implementationFactory;

            return this;
        }

        /// <summary>
        /// Adds a custom <paramref name="bodyFilter"/> to only select a subset of messages, based on its body, that the registered message handler can handle.
        /// </summary>
        public ServiceBusMessageHandlerOptions<TMessage> AddMessageBodyFilter(Func<TMessage, bool> bodyFilter)
        {
            ArgumentNullException.ThrowIfNull(bodyFilter);
            _messageBodyFilters.Add(bodyFilter);

            return this;
        }

        /// <summary>
        /// Adds a custom <paramref name="contextFilter"/> to only select a subset of messages, based on its context, that the registered message handler can handle.
        /// </summary>
        public ServiceBusMessageHandlerOptions<TMessage> AddMessageContextFilter(Func<AzureServiceBusMessageContext, bool> contextFilter)
        {
            ArgumentNullException.ThrowIfNull(contextFilter);
            _messageContextFilters.Add(contextFilter);

            return this;
        }

        /// <summary>
        /// Adds a custom serializer instance that deserializes the incoming <see cref="ServiceBusReceivedMessage.Body"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serializer"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v4.0, please use " + nameof(UseMessageBodyDeserializer) + " which provides the exact same functionality")]
        public ServiceBusMessageHandlerOptions<TMessage> AddMessageBodySerializer(IMessageBodySerializer serializer)
        {
            return UseMessageBodyDeserializer(new DeprecatedMessageBodySerializerAdapter(serializer));
        }

        /// <summary>
        /// Adds a custom serializer instance that deserializes the incoming <see cref="ServiceBusReceivedMessage.Body"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v4.0, please use " + nameof(UseMessageBodyDeserializer) + " which provides the exact same functionality")]
        public ServiceBusMessageHandlerOptions<TMessage> AddMessageBodySerializer(Func<IServiceProvider, IMessageBodySerializer> implementationFactory)
        {
            return UseMessageBodyDeserializer(serviceProvider =>
            {
                var deprecated = implementationFactory(serviceProvider);
                return new DeprecatedMessageBodySerializerAdapter(deprecated);
            });
        }

        [Obsolete("Will be removed in v4.0")]
        private sealed class DeprecatedMessageBodySerializerAdapter : IServiceBusMessageBodyDeserializer
        {
            private readonly IMessageBodySerializer _deprecated;

            internal DeprecatedMessageBodySerializerAdapter(IMessageBodySerializer deprecated)
            {
                ArgumentNullException.ThrowIfNull(deprecated);
                _deprecated = deprecated;
            }

            public async Task<MessageBodyResult> DeserializeMessageAsync(BinaryData messageBody, AzureServiceBusMessageContext messageContext)
            {
                string messageBodyTxt = messageBody.IsEmpty
                    ? string.Empty
                    : messageContext.GetEncodingOrDefault().GetString(messageBody);

                MessageResult result = await _deprecated.DeserializeMessageAsync(messageBodyTxt);

                if (result.IsSuccess)
                {
                    return MessageBodyResult.Success(result.DeserializedMessage);
                }

                return result.Exception is null
                    ? MessageBodyResult.Failure(result.ErrorMessage)
                    : MessageBodyResult.Failure(result.Exception);
            }
        }
    }
}