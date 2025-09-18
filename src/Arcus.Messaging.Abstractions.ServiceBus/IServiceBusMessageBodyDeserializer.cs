using Arcus.Messaging.Abstractions.ServiceBus;

namespace Arcus.Messaging
{
    /// <summary>
    /// Represents a custom way to deserialize an incoming message for a specific Azure Service Bus message handler
    /// </summary>
    public interface IServiceBusMessageBodyDeserializer : IMessageBodyDeserializer<AzureServiceBusMessageContext>
    {
    }
}
