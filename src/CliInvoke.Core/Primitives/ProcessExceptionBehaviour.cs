/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.ComponentModel;

namespace CliInvoke.Core;

/// <summary>
///     Specifies the behaviour for handling exceptions.
/// </summary>
[DefaultValue(AllowExceptionsIfUnexpected)]
public enum ProcessExceptionBehaviour
{
    /// <summary>
    ///     Suppresses thrown exceptions.
    /// </summary>
    SuppressExceptions = 0,

    /// <summary>
    ///     Rethrows the exception raised by the cancellation machinery whatever the
    ///     cancellation reason: a requested cancellation, a timeout, or an unknown reason.
    /// </summary>
    AllowExceptions,

    /// <summary>
    ///     Rethrows the exception raised by the cancellation machinery only when the
    ///     cancellation outcome was unexpected: a timeout or unknown reason whose
    ///     cancellation resolved more than one second before or after the exit time
    ///     predicted by the configured timeout. A requested cancellation, or a timeout
    ///     that resolves within that window, is swallowed; the process exits with a
    ///     result whose <c>Canceled</c> flag is set instead.
    /// </summary>
    AllowExceptionsIfUnexpected
}