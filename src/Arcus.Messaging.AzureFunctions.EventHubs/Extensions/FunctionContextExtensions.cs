using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;

// ReSharper disable once CheckNamespace
namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Extensions on the <see cref="FunctionContext"/> related to message correlation.
    /// </summary>
    public static class FunctionContextExtensions
    {
        /// <summary>
        /// Determine W3C message correlation from incoming execution <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The execution context of the isolated Azure Functions.</param>
        /// <param name="applicationProperties">The passed along application properties for the received event on Azure EventHubs.</param>
        /// <returns>An disposable message correlation that acts as a request scope for the remaining execution of the function.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="context"/> or the <paramref name="applicationProperties"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        public static MessageCorrelationResult GetCorrelationInfo(
            this FunctionContext context,
            Dictionary<string, JsonElement> applicationProperties)
        {
            return GetCorrelationInfo(context, applicationProperties, MessageCorrelationFormat.W3C);
        }

        /// <summary>
        /// Determine message correlation from incoming execution <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The execution context of the isolated Azure Functions.</param>
        /// <param name="applicationProperties">The passed along application properties for the received event on Azure EventHubs.</param>
        /// <param name="correlationFormat">The format of the message correlation that should be used.</param>
        /// <returns>An disposable message correlation that acts as a request scope for the remaining execution of the function.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="context"/> or the <paramref name="applicationProperties"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        public static MessageCorrelationResult GetCorrelationInfo(
            this FunctionContext context,
            Dictionary<string, JsonElement> applicationProperties,
            MessageCorrelationFormat correlationFormat)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (applicationProperties is null)
            {
                throw new ArgumentNullException(nameof(applicationProperties));
            }

            if (correlationFormat is MessageCorrelationFormat.W3C)
            {
                DetermineTraceParent(applicationProperties);
            }

            if (correlationFormat is MessageCorrelationFormat.Hierarchical)
            {
                string transactionId = DetermineTransactionId(applicationProperties, PropertyNames.TransactionId);
                string operationId = DetermineOperationId(applicationProperties);
                string operationParentId = GetOptionalUserProperty(applicationProperties, PropertyNames.OperationParentId);

            }

            throw new InvalidOperationException(
                "Cannot determine message correlation format, either choose between W3C or Hierarchical");
        }

        private static (string transactionId, string operationParentId) DetermineTraceParent(Dictionary<string, JsonElement> applicationProperties)
        {
            IDictionary<string, object> castProperties =
                applicationProperties.ToDictionary(
                    item => item.Key,
                    item => (object) item.Value.GetString());

            return castProperties.GetTraceParent();
        }

        private static string DetermineTransactionId(Dictionary<string, JsonElement> properties, string transactionIdPropertyName)
        {
            string transactionId = GetOptionalUserProperty(properties, transactionIdPropertyName);
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                string generatedTransactionId = Guid.NewGuid().ToString();
                return generatedTransactionId;
            }

            return transactionId;
        }

        private static string GetOptionalUserProperty(Dictionary<string, JsonElement> properties, string propertyName)
        {
            if (properties.TryGetValue(propertyName, out JsonElement propertyValue))
            {
                return propertyValue.ToString();
            }

            return null;
        }

        private static string DetermineOperationId(Dictionary<string, JsonElement> properties)
        {
            if (!properties.TryGetValue("CorrelationId", out JsonElement messageCorrelationId)
                || string.IsNullOrWhiteSpace(messageCorrelationId.ToString()))
            {
                var generatedOperationId = Guid.NewGuid().ToString();
                return generatedOperationId;
            }

            return messageCorrelationId.ToString();
        }
    }
}
