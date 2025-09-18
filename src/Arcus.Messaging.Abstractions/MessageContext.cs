using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arcus.Messaging.Abstractions
{
    /// <summary>
    /// Represents the contextual information concerning a message that will be processed by a message pump.
    /// </summary>
    public abstract class MessageContext
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageContext"/> class.
        /// </summary>
        /// <param name="messageId">The unique identifier of the message.</param>
        /// <param name="jobId">The unique identifier of the message pump that processes the message.</param>
        /// <param name="properties">The contextual properties provided on the message.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="messageId"/> or the <paramref name="jobId"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="properties"/> is <c>null</c>.</exception>
        protected MessageContext(string messageId, string jobId, IDictionary<string, object> properties)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
            ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
            ArgumentNullException.ThrowIfNull(properties);

            MessageId = messageId;
            JobId = jobId;
            Properties = properties;
        }

        /// <summary>
        /// Gets unique identifier of the message.
        /// </summary>
        public string MessageId { get; }

        /// <summary>
        /// Gets the unique ID on which message pump this message was processed.
        /// </summary>
        public string JobId { get; }

        /// <summary>
        /// Gets the contextual properties provided on the message.
        /// </summary>
        public IDictionary<string, object> Properties { get; }

        /// <summary>
        /// Gets the configured encoding in the <see cref="Properties"/> with the <see cref="PropertyNames.Encoding"/>,
        /// or fall back on a default (UTF-8).
        /// </summary>
        public Encoding GetEncodingOrDefault()
        {
            Encoding fallbackEncoding = Encoding.UTF8;

            if (Properties.TryGetValue(PropertyNames.Encoding, out object encodingNameObj)
                && encodingNameObj is string encodingName
                && !string.IsNullOrWhiteSpace(encodingName))
            {
                EncodingInfo foundEncoding =
                    Encoding.GetEncodings()
                            .FirstOrDefault(e => e.Name.Equals(encodingName, StringComparison.OrdinalIgnoreCase));

                return foundEncoding?.GetEncoding() ?? fallbackEncoding;
            }

            return fallbackEncoding;
        }
    }
}