/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace CliInvoke.Core;

/// <summary>
/// Provides extension methods to configure process exit scenarios for applications
/// using the CliInvoke.
/// </summary>
public static class ProcessExitConfigurationCreationExtensions
{
    extension(ProcessExitConfiguration)
    {
        /// <summary>
        /// Provides a default instance of <see cref="ProcessExitConfiguration"/>
        /// that is configured to perform a graceful exit with a standard timeout policy.
        /// </summary>
        public static ProcessExitConfiguration Default
            => ProcessExitConfiguration.CreateGraceful();

        /// <summary>
        /// Creates a new instance of <see cref="ProcessExitConfiguration"/> with the specified timeout policy, cancellation exit behaviour, and exception handling settings.
        /// </summary>
        /// <param name="timeoutPolicy">The timeout policy that determines the allowed execution duration for the process.</param>
        /// <param name="cancellationExitBehaviour">The behaviour to apply when the process is cancelled. Defaults to <see cref="ProcessExitBehaviour.GracefulExit"/>.</param>
        /// <param name="suppressExceptions">Indicates whether exceptions should be suppressed during the process exit. Defaults to false.</param>
        /// <returns>A configured <see cref="ProcessExitConfiguration"/> instance with the specified settings.</returns>
        public static ProcessExitConfiguration Create(ProcessTimeoutPolicy timeoutPolicy,
            ProcessExitBehaviour cancellationExitBehaviour = ProcessExitBehaviour.GracefulExit,
            bool suppressExceptions = false)
        {
            return new ProcessExitConfiguration(timeoutPolicy,
                cancellationExitBehaviour,
                suppressExceptions ? ProcessExceptionBehaviour.SuppressExceptions : 
                    ProcessExceptionBehaviour.AllowExceptionsIfUnexpected,
                !suppressExceptions);
        }

        /// <summary>
        /// Creates a default graceful <see cref="ProcessExitConfiguration"/> instance using a standard timeout policy.
        /// </summary>
        /// <returns>A <see cref="ProcessExitConfiguration"/> object configured to perform a graceful exit with the default timeout policy.</returns>
        public static ProcessExitConfiguration CreateGraceful()
            => ProcessExitConfiguration.CreateGraceful(ProcessTimeoutPolicy.Default);

        /// <summary>
        /// Creates a default graceful <see cref="ProcessExitConfiguration"/> instance using the specified timeout policy.
        /// </summary>
        /// <param name="timeoutPolicy">The <see cref="ProcessTimeoutPolicy"/> instance to define the timeout settings for the process exit.</param>
        /// <returns>A <see cref="ProcessExitConfiguration"/> object configured to perform a graceful exit using the specified timeout policy.</returns>
        public static ProcessExitConfiguration CreateGraceful(ProcessTimeoutPolicy timeoutPolicy)
            => ProcessExitConfiguration.Create(timeoutPolicy,  ProcessExitBehaviour.GracefulExit,
                true);

        /// <summary>
        /// Creates a default forceful <see cref="ProcessExitConfiguration"/> instance using a standard timeout policy.
        /// </summary>
        /// <returns>A <see cref="ProcessExitConfiguration"/> object configured to perform a forceful exit with the default timeout policy.</returns>
        public static ProcessExitConfiguration CreateForceful()
            => ProcessExitConfiguration.CreateForceful(ProcessTimeoutPolicy.Default);

        /// <summary>
        /// Creates a forceful <see cref="ProcessExitConfiguration"/> instance using the default timeout policy.
        /// A forceful exit ensures that the process is terminated without waiting for any ongoing operations or clean-up.
        /// </summary>
        /// <returns>A <see cref="ProcessExitConfiguration"/> object configured for a forceful exit with the default timeout policy.</returns>
        public static ProcessExitConfiguration CreateForceful(ProcessTimeoutPolicy timeoutPolicy)
            => ProcessExitConfiguration.Create(new ProcessTimeoutPolicy(
                timeoutPolicy.TimeoutThreshold, timeoutPolicy.Enabled,
                ProcessExitBehaviour.ForcefulExit));
    }
}