using System;
using System.Diagnostics;

using AlastairLundy.CliInvoke.Core.Primitives;

#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

// ReSharper disable MemberCanBePrivate.Global

namespace AlastairLundy.CliInvoke.Core.Extensions
{
    public static class ProcessAddCredentialExtensions
    {

        /// <summary>
        /// Attempts to add the specified Credential to the current ProcessStartInfo object.
        /// </summary>
        /// <param name="processStartInfo">The current Process start info object.</param>
        /// <param name="credential">The credential to be added.</param>
        /// <returns>True if successfully applied; false otherwise.</returns>
        public static bool TryAddUserCredential(this ProcessStartInfo processStartInfo, UserCredential credential)
        {
            if (credential.IsSupportedOnCurrentOS())
            {
                try
                {
                    AddUserCredential(processStartInfo, credential);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        
        /// <summary>
        /// Attempts to add the specified Credential to the current Process object.
        /// </summary>
        /// <param name="process">The current Process object.</param>
        /// <param name="credential">The credential to be added.</param>
        /// <returns>True if successfully applied; false otherwise.</returns>
        public static bool TryAddUserCredential(this Process process, UserCredential credential)
        {
            if (credential.IsSupportedOnCurrentOS())
            {
                try
                {
                    AddUserCredential(process, credential);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    
        /// <summary>
        /// Adds the specified Credential to the current Process object.
        /// </summary>
        /// <param name="process">The current Process object.</param>
        /// <param name="credential">The credential to be added.</param>
        /// <exception cref="PlatformNotSupportedException">Thrown if not supported on the current operating system.</exception>
#if NET5_0_OR_GREATER
        [SupportedOSPlatform("windows")]
#endif
        public static void AddUserCredential(this Process process, UserCredential credential)
        {
#pragma warning disable CA1416
            if (credential.IsSupportedOnCurrentOS())
            {
                if (credential.Domain is not null)
                {
                    process.StartInfo.Domain = credential.Domain;
                }

                if (credential.UserName is not null)
                {
                    process.StartInfo.UserName = credential.UserName;
                }

                if (credential.Password is not null)
                {
                    process.StartInfo.Password = credential.Password;
                }

                if (credential.LoadUserProfile is not null)
                {
                    process.StartInfo.LoadUserProfile = (bool)credential.LoadUserProfile;
                }
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
#pragma warning restore CA1416
        }
    
        /// <summary>
        /// Adds the specified Credential to the current ProcessStartInfo object.
        /// </summary>
        /// <param name="processStartInfo">The current ProcessStartInfo object.</param>
        /// <param name="credential">The credential to be added.</param>
#if NET5_0_OR_GREATER
        [SupportedOSPlatform("windows")]
#endif
        public static void AddUserCredential(this ProcessStartInfo processStartInfo, UserCredential credential)
        {
#pragma warning disable CA1416
            if (credential.IsSupportedOnCurrentOS())
            {
                if (credential.Domain is not null)
                {
                    processStartInfo.Domain = credential.Domain;
                }

                if (credential.UserName is not null)
                {
                    processStartInfo.UserName = credential.UserName;
                }

                if (credential.Password is not null)
                {
                    processStartInfo.Password = credential.Password;
                }

                if (credential.LoadUserProfile is not null)
                {
                    processStartInfo.LoadUserProfile = (bool)credential.LoadUserProfile;
                }
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
#pragma warning restore CA1416
        }
    }
}