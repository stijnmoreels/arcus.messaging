using System;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents a handler that provides a deserialization strategy for the incoming message during the message processing of message pump or router.
    /// </summary>
    /// <seealso cref="IMessageHandler{TMessage,TMessageContext}"/>
    [Obsolete("Will be removed in v4.0, please use the " + nameof(IMessageBodyDeserializer<MessageContext>) + " interface instead")]
    public interface IMessageBodySerializer
    {
        /// <summary>
        /// Tries to deserialize the incoming <paramref name="messageBody"/> to a message instance.
        /// </summary>
        /// <param name="messageBody">The incoming message body.</param>
        /// <returns>
        ///     A message result that either represents a successful or faulted deserialization result.
        /// </returns>
        Task<MessageResult> DeserializeMessageAsync(string messageBody);
    }
}

namespace Arcus.Messaging
{
    /// <summary>
    /// Represents a custom way to deserialize an incoming message for a specific <see cref="IMessageHandler{TMessage,TMessageContext}"/>.
    /// </summary>
    /// <typeparam name="TMessageContext">The custom type representing the context in which the message is deserialized.</typeparam>
    public interface IMessageBodyDeserializer<in TMessageContext> where TMessageContext : MessageContext
    {
        /// <summary>
        /// Tries to deserialize the incoming <paramref name="messageBody"/> to a message instance that a handler can process.
        /// </summary>
        /// <param name="messageBody">The incoming message body that needs to be deserialized to a concrete type.</param>
        /// <param name="messageContext">The instance representing the context in which the message is deserialized.</param>
        /// <returns>
        ///     A message result that either represents a successful or faulted deserialization result.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageBody"/> or <paramref name="messageContext"/> is <c>null</c>.</exception>
        Task<MessageBodyResult> DeserializeMessageAsync(BinaryData messageBody, TMessageContext messageContext);
    }

    /// <summary>
    /// Represents a type that's the result of a successful or faulted message deserialization of an <see cref="IMessageBodyDeserializer{TMessageContext}"/> instance.
    /// </summary>
    /// <seealso cref="IMessageBodySerializer"/>
    public class MessageBodyResult
    {
        private MessageBodyResult(object result)
        {
            ArgumentNullException.ThrowIfNull(result);

            IsSuccess = true;
            DeserializedMessage = result;
        }

        private MessageBodyResult(Exception exception) : this(exception.Message)
        {
            ArgumentNullException.ThrowIfNull(exception);
            Exception = exception;
        }

        private MessageBodyResult(string errorMessage)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

            IsSuccess = false;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Gets a flag indicating whether or not the message was successfully deserialized.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets the deserialized message instance after the <see cref="IMessageBodyDeserializer{TMessageContext}"/>.
        /// </summary>
        /// <remarks>
        ///     Only contains a value if the deserialization in the <see cref="IMessageBodyDeserializer{TMessageContext}"/> was successful (<see cref="IsSuccess"/> is <c>true</c>).
        /// </remarks>
        public object DeserializedMessage { get; }

        /// <summary>
        /// Gets the optional error message describing the failure during the deserialization in the <see cref="IMessageBodyDeserializer{TMessageContext}"/>.
        /// </summary>
        /// <remarks>
        ///     Only contains value if the deserialization in the <see cref="IMessageBodyDeserializer{TMessageContext}"/> was faulted (<see cref="IsSuccess"/> is <c>false</c>).
        /// </remarks>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets the optional exception thrown during the deserialization in the <see cref="IMessageBodyDeserializer{TMessageContext}"/>.
        /// </summary>
        /// <remarks>
        ///     Only contains a value if the deserialization in the <see cref="IMessageBodyDeserializer{TMessageContext}"/> was faulted (<see cref="IsSuccess"/> is <c>false</c>)
        ///     and there was an exception thrown during the deserialization.
        /// </remarks>
        public Exception Exception { get; }

        /// <summary>
        /// Creates a <see cref="MessageBodyResult"/> that represents a successful deserialization.
        /// </summary>
        /// <param name="message">The deserialized message instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        public static MessageBodyResult Success(object message)
        {
            return new MessageBodyResult(message);
        }

        /// <summary>
        /// Creates a <see cref="MessageBodyResult"/> that represents a faulted deserialization.
        /// </summary>
        /// <param name="errorMessage">The message describing the deserialization error.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="errorMessage"/> is blank.</exception>
        public static MessageBodyResult Failure(string errorMessage)
        {
            return new MessageBodyResult(errorMessage);
        }

        /// <summary>
        /// Creates a <see cref="MessageBodyResult"/> that represents a faulted deserialization.
        /// </summary>
        /// <param name="exception">The exception describing the deserialization failure.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="exception"/> is <c>null</c>.</exception>
        public static MessageBodyResult Failure(Exception exception)
        {
            return new MessageBodyResult(exception);
        }
    }
}
