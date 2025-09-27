/*
    AlastairLundy.CliInvoke.Core 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */


using System;
using System.Diagnostics;
using AlastairLundy.CliInvoke.Core.Internal;

#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

#if NETSTANDARD2_0
using OperatingSystem = Polyfills.OperatingSystemPolyfill;
#endif

using AlastairLundy.CliInvoke.Core.Primitives;
// ReSharper disable MemberCanBePrivate.Global

namespace AlastairLundy.CliInvoke.Core.Extensions.Processes;

public static class ProcessApplyExtensions
{
    /// <summary>
    /// Attempts to add the specified Credential to the current Process object.
    /// </summary>
    /// <param name="process">The current Process object.</param>
    /// <param name="credential">The credential to be added.</param>
    /// <returns>True if successfully applied; false otherwise.</returns>
    [Obsolete(DeprecationMessages.ClassDeprecationV2)]
    public static bool TryApplyUserCredential(this Process process, UserCredential credential)
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
#pragma warning disable CA1416
                ApplyUserCredential(process, credential);
#pragma warning restore CA1416
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
    public static void ApplyUserCredential(this Process process, UserCredential credential)
    {
#pragma warning disable CA1416
        if (OperatingSystem.IsWindows())
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
}