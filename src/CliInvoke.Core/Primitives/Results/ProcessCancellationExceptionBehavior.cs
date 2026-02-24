/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Core;

/// <summary>
/// Specifies the behaviour for handling exceptions when a process cancellation occurs.
/// </summary>
// TODO: Rename type to ProcessCancellationHandlingMode for v3
public enum ProcessCancellationExceptionBehavior
{
    /// <summary>
    /// Suppresses thrown exceptions.
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
