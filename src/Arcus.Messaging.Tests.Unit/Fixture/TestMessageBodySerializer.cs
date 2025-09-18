using System;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.Fixture
{
    /// <summary>
    /// Test <see cref="IMessageBodySerializer"/> implementation to return a static <see cref="TestMessage"/> instance.
    /// </summary>
    public class TestMessageBodySerializer : IServiceBusMessageBodyDeserializer, IMessageBodySerializer
    {
        private readonly string _expectedExpectedBody;
        private readonly object _message;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestMessageBodySerializer"/> class.
        /// </summary>
        public TestMessageBodySerializer() : this(null, new TestMessage())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestMessageBodySerializer"/> class.
        /// </summary>
        public TestMessageBodySerializer(string expectedBody) : this(expectedBody, new TestMessage())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestMessageBodySerializer"/> class.
        /// </summary>
        public TestMessageBodySerializer(string expectedBody, object message)
        {
            ArgumentNullException.ThrowIfNull(message);

            _expectedExpectedBody = expectedBody;
            _message = message;
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
            Assert.Equal(_expectedExpectedBody, messageBody.ToString());
            return Task.FromResult(MessageBodyResult.Success(_message));
        }

        public Task<MessageResult> DeserializeMessageAsync(string messageBody)
        {
            Assert.Equal(_expectedExpectedBody, messageBody);
            return Task.FromResult(MessageResult.Success(_message));
        }
    }
}
