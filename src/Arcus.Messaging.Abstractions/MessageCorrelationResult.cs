using System;

namespace Arcus.Messaging.Abstractions
{
    /// <summary>
    /// Represents the correlation result of a received Azure Service Bus message.
    /// This result will act as the scope of the request telemetry.
    /// </summary>
    public sealed class MessageCorrelationResult : IDisposable
    {
        private readonly Action<bool> _onRequestCompleted;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="correlationInfo"></param>
        /// <param name="onRequestCompleted"></param>
        public MessageCorrelationResult(
            MessageCorrelationInfo correlationInfo,
            Action<bool> onRequestCompleted)
        {
            _onRequestCompleted = onRequestCompleted;

            CorrelationInfo = correlationInfo;
        }

        /// <summary>
        /// Gets the correlation information of the current received Azure Service Bus message.
        /// </summary>
        public MessageCorrelationInfo CorrelationInfo { get; }

        /// <summary>
        /// Gets or sets whether the received Azure Service bus message was successfully handled.
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _onRequestCompleted(IsSuccessful);
        }
    }
}
