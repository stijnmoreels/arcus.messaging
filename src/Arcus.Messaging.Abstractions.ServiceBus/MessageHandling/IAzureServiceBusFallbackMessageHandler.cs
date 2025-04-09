using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Azure.Messaging.ServiceBus;

namespace Arcus.Messaging.Abstractions.ServiceBus.MessageHandling
{
    /// <summary>
    /// Fallback version of the <see cref="IAzureServiceBusMessageHandler{TMessage}"/> to have a safety net when no handlers are found could process the message.
    /// </summary>
    [Obsolete("Will be removed v3.0, as the 'fallback' functionality is being removed in favor of a simpler message routing system")]
    public interface IAzureServiceBusFallbackMessageHandler : IFallbackMessageHandler<ServiceBusReceivedMessage, AzureServiceBusMessageContext>
    {
    }
}
