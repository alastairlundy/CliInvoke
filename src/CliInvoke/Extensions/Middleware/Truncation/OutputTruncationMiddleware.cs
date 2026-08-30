/*
    CliInvoke.Extensions
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Core.Middleware;
using CliInvoke.Internal.Extensions;

namespace CliInvoke.Extensions.Middleware;

/// <summary>
///     Opt-in middleware that publishes a per-stream output-truncation cap into
///     <see cref="MiddlewareItems"/> before the remainder of the pipeline runs.
/// </summary>
/// <remarks>
///     The cap is written under <see cref="TruncationDefaults.MaxBytesPerStreamKey"/> so that the
///     buffered-capture path (which runs downstream of this link) can truncate each stream as it is
///     read. This middleware only writes the cap; it does not perform truncation itself. It is ordered
///     upstream of <c>LoggingMiddleware</c> so logs reflect already-capped output
///     (see DECISIONS-CliInvoke-middleware-truncation-caching-retry.md). Does not apply to
///     <c>IExternalProcess</c> (middleware does not flow there).
/// </remarks>
internal sealed class OutputTruncationMiddleware : IProcessMiddleware
{
    private readonly TruncationOptions _options;

    /// <summary>
    ///     Initialises a new instance of the <see cref="OutputTruncationMiddleware"/> class.
    /// </summary>
    /// <param name="options">The truncation options carrying the per-stream cap.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <c>null</c>.</exception>
    public OutputTruncationMiddleware(TruncationOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task InvokeAsync(InvocationContext context, Func<InvocationContext, Task> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        // Publish the cap before the terminal pipeline reads/captures output. The walkers assign
        // context.Middleware before invoking the first link, but guard defensively in case it is null.
        context.Middleware?.Items.Set<long>(TruncationDefaults.MaxBytesPerStreamKey, _options.MaxSize);

        await next(context);
    }
}
