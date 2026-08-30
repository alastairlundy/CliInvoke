/*
    CliInvoke.Extensions
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

namespace CliInvoke.Extensions.Middleware.Truncation;

/// <summary>
///     Configuration options for <see cref="OutputTruncationMiddleware"/>.
/// </summary>
/// <remarks>
///     Unlike the built-in middleware options, this type is not registered in the dependency
///     injection container by default — <see cref="OutputTruncationMiddleware"/> is opt-in, so the
///     options instance is supplied directly when configuring the pipeline via the
///     <c>UseOutputTruncation</c> extension method on <see cref="OutputTruncationMiddlewareExtensions"/>.
/// </remarks>
public sealed class TruncationOptions
{
    /// <summary>
    ///     Gets the default options instance with a 1 MB per-stream cap.
    /// </summary>
    public static TruncationOptions Default { get; } = new();

    /// <summary>
    ///     Gets or sets the maximum number of bytes retained per output stream
    ///     (standard output and standard error) before capture-time truncation occurs.
    /// </summary>
    /// <remarks>
    ///     A single value bounds both streams. The default of 1 MB bounds memory growth out of the
    ///     box while remaining overridable (see DECISIONS-CliInvoke-middleware-truncation-caching-retry.md).
    /// </remarks>
    public long MaxSize { get; set; } = 1_048_576;
}
