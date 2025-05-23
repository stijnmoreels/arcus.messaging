﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Arcus.Messaging.Pumps.Abstractions
{
    /// <summary>
    /// Represents the handler to control the lifetime of a certain message pump.
    /// </summary>
    [Obsolete("Will be removed in v3.0 since the circuit breaker functionality handles start/pause automatically now")]
    public interface IMessagePumpLifetime
    {
        /// <summary>
        /// Starts a message pump with the given <paramref name="jobId"/>.
        /// </summary>
        /// <param name="jobId">The uniquely defined identifier of the registered message pump.</param>
        /// <param name="cancellationToken">The token to indicate that the start process has been aborted.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        Task StartProcessingMessagesAsync(string jobId, CancellationToken cancellationToken);

        /// <summary>
        /// Pauses a message pump with the given <paramref name="jobId"/> for a specified <paramref name="duration"/>.
        /// </summary>
        /// <param name="jobId">The uniquely defined identifier of the registered message pump.</param>
        /// <param name="duration">The time duration in which the message pump should be stopped.</param>
        /// <param name="cancellationToken">The token to indicate that the shutdown and start process should no longer be graceful.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="duration"/> is a negative time range.</exception>
        Task PauseProcessingMessagesAsync(string jobId, TimeSpan duration, CancellationToken cancellationToken);

        /// <summary>
        /// Stops a message pump with a given <paramref name="jobId"/>.
        /// </summary>
        /// <param name="jobId">The uniquely defined identifier of the registered message pump.</param>
        /// <param name="cancellationToken">The token to indicate that the shutdown process should no longer be graceful.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        Task StopProcessingMessagesAsync(string jobId, CancellationToken cancellationToken);
    }
}
