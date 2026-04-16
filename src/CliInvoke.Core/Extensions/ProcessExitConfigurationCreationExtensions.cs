/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace CliInvoke.Core;

/// <summary>
/// 
/// </summary>
public static class ProcessExitConfigurationCreationExtensions
{
    extension(ProcessExitConfiguration)
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeoutPolicy"></param>
        /// <param name="cancellationExitBehaviour"></param>
        /// <param name="suppressExceptions"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
        public static ProcessExitConfiguration CreateGraceful()
            => ProcessExitConfiguration.CreateGraceful(ProcessTimeoutPolicy.Default);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeoutPolicy"></param>
        /// <returns></returns>
        public static ProcessExitConfiguration CreateGraceful(ProcessTimeoutPolicy timeoutPolicy)
            => ProcessExitConfiguration.Create(timeoutPolicy,  ProcessExitBehaviour.GracefulExit,
                true);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static ProcessExitConfiguration CreateForceful()
            => ProcessExitConfiguration.CreateForceful(ProcessTimeoutPolicy.Default);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeoutPolicy"></param>
        /// <returns></returns>
        public static ProcessExitConfiguration CreateForceful(ProcessTimeoutPolicy timeoutPolicy)
            => ProcessExitConfiguration.Create(new ProcessTimeoutPolicy(
                timeoutPolicy.TimeoutThreshold, timeoutPolicy.Enabled,
                ProcessExitBehaviour.ForcefulExit));
    }
}