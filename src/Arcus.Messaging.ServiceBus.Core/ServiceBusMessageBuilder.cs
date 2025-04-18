﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;

#pragma warning disable S1133 // Disable usage of deprecated functionality until v3.0 is released.

// ReSharper disable once CheckNamespace
namespace Azure.Messaging.ServiceBus
{
    /// <summary>
    /// Represents a builder instance to create <see cref="ServiceBusMessage"/> instances in different ways.
    /// </summary>
    [Obsolete("Will be removed in v3.0 as this builder is only used when Azure Service bus messages are send with the deprecated " + nameof(MessageCorrelationFormat.Hierarchical) + " correlation format")]
    public class ServiceBusMessageBuilder
    {
        private readonly object _messageBody;
        private readonly Encoding _encoding;
        private KeyValuePair<string, object> _transactionIdProperty, _operationParentIdProperty, _operationIdProperty;

        private ServiceBusMessageBuilder(object messageBody, Encoding encoding)
        {
            _messageBody = messageBody ?? throw new ArgumentNullException(nameof(messageBody));
            _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
        }

        /// <summary>
        /// Starts a new <see cref="ServiceBusMessageBuilder"/> to create a new <see cref="ServiceBusMessage"/> from a given <paramref name="messageBody"/>.
        /// </summary>
        /// <param name="messageBody">The message body that will be serialized as the body of the <see cref="ServiceBusMessage"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageBody"/> is <c>null</c>.</exception>
        public static ServiceBusMessageBuilder CreateForBody(object messageBody)
        {
            return new ServiceBusMessageBuilder(messageBody, Encoding.UTF8);
        }

        /// <summary>
        /// Starts a new <see cref="ServiceBusMessageBuilder"/> to create a new <see cref="ServiceBusMessage"/> from a given <paramref name="messageBody"/>.
        /// </summary>
        /// <param name="messageBody">The message body that will be serialized as the body of the <see cref="ServiceBusMessage"/>.</param>
        /// <param name="encoding">The encoding in which the <paramref name="messageBody"/> should be included in the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageBody"/> or the <paramref name="encoding"/> is <c>null</c>.</exception>
        public static ServiceBusMessageBuilder CreateForBody(object messageBody, Encoding encoding)
        {
            return new ServiceBusMessageBuilder(messageBody, encoding);
        }

        /// <summary>
        /// Adds an <paramref name="operationId"/> as the <see cref="ServiceBusMessage.CorrelationId"/> to the <see cref="ServiceBusMessage"/>.
        /// </summary>
        /// <param name="operationId">The unique identifier for this operation.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="operationId"/> is blank.</exception>
        public ServiceBusMessageBuilder WithOperationId(string operationId)
        {
            if (operationId is not null)
            {
                _operationIdProperty = new KeyValuePair<string, object>(null, operationId);
            }

            return this;
        }

        /// <summary>
        /// Adds an <paramref name="operationId"/> as an application property in the <see cref="ServiceBusMessage.ApplicationProperties"/>.
        /// </summary>
        /// <param name="operationId">The unique identifier for this operation.</param>
        /// <param name="operationIdPropertyName">The custom name that must be given to the application property that contains the <paramref name="operationId"/>.</param>
        /// <remarks>
        ///     <para>When no <paramref name="operationId"/> is specified, no operation ID will be set on the <see cref="ServiceBusMessage"/>;</para>
        ///     <para>when no <paramref name="operationIdPropertyName"/> is specified, the default location <see cref="ServiceBusMessage.CorrelationId"/> will be used to set the operation ID.</para>
        /// </remarks>
        public ServiceBusMessageBuilder WithOperationId(string operationId, string operationIdPropertyName)
        {
            if (operationId is not null)
            {
                _operationIdProperty = new KeyValuePair<string, object>(operationIdPropertyName, operationId);
            }

            return this;
        }

        /// <summary>
        /// Adds a <paramref name="transactionId"/> as an application property in <see cref="ServiceBusMessage.ApplicationProperties"/>
        /// with <see cref="PropertyNames.TransactionId"/> as key.
        /// </summary>
        /// <param name="transactionId">The unique identifier of the current transaction.</param>
        /// <remarks>When no <paramref name="transactionId"/> is specified, no transaction ID will be set on the <see cref="ServiceBusMessage"/>.</remarks>
        public ServiceBusMessageBuilder WithTransactionId(string transactionId)
        {
            return WithTransactionId(transactionId, PropertyNames.TransactionId);
        }

        /// <summary>
        /// Adds a <paramref name="transactionId"/> as an application property in <see cref="ServiceBusMessage.ApplicationProperties"/>
        /// with <paramref name="transactionIdPropertyName"/> as key.
        /// </summary>
        /// <param name="transactionId">The unique identifier of the current transaction.</param>
        /// <param name="transactionIdPropertyName">The custom name that must be given to the application property that contains the <paramref name="transactionId"/>.</param>
        /// <remarks>
        ///     <para>When no <paramref name="transactionId"/> is specified, no transaction ID will be set on the <see cref="ServiceBusMessage"/>;</para>
        ///     <para>when no <paramref name="transactionIdPropertyName"/> is specified, the default <see cref="PropertyNames.TransactionId"/> application property name will be used.</para>
        /// </remarks>
        public ServiceBusMessageBuilder WithTransactionId(string transactionId, string transactionIdPropertyName)
        {
            if (transactionId is not null)
            {
                _transactionIdProperty = new KeyValuePair<string, object>(
                    transactionIdPropertyName ?? PropertyNames.TransactionId,
                    transactionId);
            }

            return this;
        }

        /// <summary>
        /// Adds a <paramref name="operationParentId"/> as an application property in <see cref="ServiceBusMessage.ApplicationProperties"/>
        /// with <see cref="PropertyNames.OperationParentId"/> as key.
        /// </summary>
        /// <param name="operationParentId">The unique identifier of the current parent operation.</param>
        /// <remarks>When no <paramref name="operationParentId"/> is specified, no operation parent ID will be set on the <see cref="ServiceBusMessage"/>.</remarks>
        public ServiceBusMessageBuilder WithOperationParentId(string operationParentId)
        {
            return WithOperationParentId(operationParentId, PropertyNames.OperationParentId);
        }

        /// <summary>
        /// Adds a <paramref name="operationParentId"/> as an application property in <see cref="ServiceBusMessage.ApplicationProperties"/>
        /// with <paramref name="operationParentIdPropertyName"/> as key.
        /// </summary>
        /// <param name="operationParentId">The unique identifier of the current operation.</param>
        /// <param name="operationParentIdPropertyName">The custom name that must be given to the application property that contains the <paramref name="operationParentId"/>.</param>
        /// <remarks>
        ///     <para>When no <paramref name="operationParentId"/> is specified, no operation parent ID will be set on the <see cref="ServiceBusMessage"/>;</para>
        ///     <para>when no <paramref name="operationParentIdPropertyName"/> is specified, the default <see cref="PropertyNames.OperationParentId"/> application property name will be used.</para>
        /// </remarks>
        public ServiceBusMessageBuilder WithOperationParentId(
            string operationParentId,
            string operationParentIdPropertyName)
        {
            if (operationParentId is not null)
            {
                _operationParentIdProperty = new KeyValuePair<string, object>(
                    operationParentIdPropertyName ?? PropertyNames.OperationParentId,
                    operationParentId);
            }

            return this;
        }

        /// <summary>
        /// Creates an <see cref="ServiceBusMessage"/> instance based on the configured settings.
        /// </summary>
        public ServiceBusMessage Build()
        {
            string json = JsonSerializer.Serialize(_messageBody);
            byte[] raw = _encoding.GetBytes(json);
            var message = new ServiceBusMessage(raw)
            {
                ApplicationProperties =
                {
                    { PropertyNames.ContentType, "application/json" },
                    { PropertyNames.Encoding, _encoding.WebName }
                }
            };

            if (_operationIdProperty.Key is null && _operationIdProperty.Value is not null)
            {
                message.CorrelationId = _operationIdProperty.Value?.ToString();
            }
            else if (_operationIdProperty.Value is not null)
            {
                message.ApplicationProperties.Add(_operationIdProperty);
            }

            if (_transactionIdProperty.Key is not null)
            {
                message.ApplicationProperties.Add(_transactionIdProperty);
            }

            if (_operationParentIdProperty.Key is not null)
            {
                message.ApplicationProperties.Add(_operationParentIdProperty);
            }

            return message;
        }
    }
}
