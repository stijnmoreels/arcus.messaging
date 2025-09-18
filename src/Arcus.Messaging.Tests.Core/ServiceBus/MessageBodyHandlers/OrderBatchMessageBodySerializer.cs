using System;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace Arcus.Messaging.Tests.Workers.MessageBodyHandlers
{
    public class OrderBatchMessageBodySerializer : IServiceBusMessageBodyDeserializer
    {
        private readonly ILogger<OrderBatchMessageBodySerializer> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderBatchMessageBodySerializer"/> class.
        /// </summary>
        public OrderBatchMessageBodySerializer(ILogger<OrderBatchMessageBodySerializer> logger)
        {
            _logger = logger ?? NullLogger<OrderBatchMessageBodySerializer>.Instance;
        }

        /// <summary>
        /// Tries to deserialize the incoming <paramref name="messageBody"/> to a message instance that a handler can process.
        /// </summary>
        /// <param name="messageBody">The incoming message body that needs to be deserialized to a concrete type.</param>
        /// <param name="messageContext">The instance representing the context in which the message is deserialized.</param>
        /// <returns>
        ///     A message result that either represents a successful or faulted deserialization result.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageBody"/> or <paramref name="messageContext"/> is <c>null</c>.</exception>
        public Task<MessageBodyResult> DeserializeMessageAsync(BinaryData messageBody, AzureServiceBusMessageContext messageContext)
        {
            var order = JsonConvert.DeserializeObject<Order>(messageBody.ToString());

            if (order is null)
            {
                return Task.FromResult(MessageBodyResult.Failure("Cannot deserialize incoming message to an 'Order', so can't use 'Order'"));
            }

            return Task.FromResult(MessageBodyResult.Success(new OrderBatch { Orders = new[] { order } }));
        }
    }
}
