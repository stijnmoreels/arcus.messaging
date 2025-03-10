﻿using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ServiceBus;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    /// Represents the contract on how to authenticate with the Azure Service Bus.
    /// </summary>
    [Obsolete("Will be removed in v3.0 as the pump project will solely focus on providing message routing functionality")]
    public interface IAzureServiceBusManagementAuthentication
    {
        /// <summary>
        /// Authenticates with to the previously specified Azure Service Bus resource.
        /// </summary>
        /// <returns>
        ///     An <see cref="IServiceBusManagementClient"/> instance that manages the previously specified Azure Service Bus resource.
        /// </returns>
        Task<IServiceBusManagementClient> AuthenticateAsync();
    }
}
