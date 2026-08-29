/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

namespace CliInvoke;

/// <summary>
///     Shared defaults for per-stream output truncation during buffered capture.
/// </summary>
/// <remarks>
///     The key constant lives in <c>CliInvoke</c> (not <c>CliInvoke.Extensions</c>) so that both the
///     invocation pipeline and the Extensions middleware can reference it without creating a circular
///     dependency. See DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#I001.
/// </remarks>
internal static class TruncationDefaults
{
    /// <summary>
    ///     The <c>MiddlewareItems</c> key under which a single per-stream byte cap (a
    ///     <see cref="long"/>) is stored. The pipeline applies the same value to both standard output
    ///     and standard error.
    /// </summary>
    public const string MaxBytesPerStreamKey = "CliInvoke.Truncation.MaxBytesPerStream";
}
