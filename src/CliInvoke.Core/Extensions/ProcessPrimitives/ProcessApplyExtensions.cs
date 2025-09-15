/*
    AlastairLundy.DotPrimitives 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */


using System;
using System.Diagnostics;

using AlastairLundy.CliInvoke.Core.Primitives;

using System.Runtime.Versioning;

// ReSharper disable MemberCanBePrivate.Global

namespace AlastairLundy.CliInvoke.Core;

/// <summary>
/// 
/// </summary>
public static class ProcessApplyExtensions
{
    /// <summary>
    /// Adds the specified Credential to the current Process object.
    /// </summary>
    /// <param name="process">The current Process object.</param>
    /// <param name="credential">The credential to be added.</param>
    /// <exception cref="PlatformNotSupportedException">Thrown if not supported on the current operating system.</exception>
    [SupportedOSPlatform("windows")]
    public static void ApplyUserCredential(this Process process, UserCredential credential)
    {
#pragma warning disable CA1416
        if (credential.IsSupportedOnCurrentOS())
        {
            if (credential.Domain is not null) 
                process.StartInfo.Domain = credential.Domain;

            if (credential.UserName is not null) 
                process.StartInfo.UserName = credential.UserName;

            if (credential.Password is not null) 
                process.StartInfo.Password = credential.Password;

            if (credential.LoadUserProfile is not null) 
                process.StartInfo.LoadUserProfile = (bool)credential.LoadUserProfile;
        }
        else
        {
            throw new PlatformNotSupportedException();
        }
#pragma warning restore CA1416
    }
}