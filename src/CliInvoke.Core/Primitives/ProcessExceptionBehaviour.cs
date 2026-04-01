/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Core;

/// <summary>
///     Specifies the behaviour for handling exceptions.
/// </summary>
public enum ProcessExceptionBehaviour
{
    /// <summary>
    ///     Suppresses thrown exceptions.
    /// </summary>
    SuppressExceptions = 0,

    /// <summary>
    ///     Allow .NET to throw the exception if expected.
    /// </summary>
    AllowExceptions,

    /// <summary>
    ///     Allows the exception to be thrown if it is unexpected.
    /// </summary>
    AllowExceptionsIfUnexpected
}