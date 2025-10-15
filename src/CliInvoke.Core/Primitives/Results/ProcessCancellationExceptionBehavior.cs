/*
    CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace AlastairLundy.CliInvoke.Core;

/// <summary>
/// 
/// </summary>
public enum ProcessCancellationExceptionBehavior
{
    /// <summary>
    /// No validation is performed.
    /// </summary>
    SuppressException = 0,
    /// <summary>
    /// Allow .NET to throw the exception if the cancellation succeeds.
    /// </summary>
    AllowException,
    /// <summary>
    /// Allows the exception if the cancellation is unexpected.
    /// </summary>
    AllowExceptionIfUnexpected,
}