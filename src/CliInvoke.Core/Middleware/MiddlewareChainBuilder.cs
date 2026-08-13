/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Core.Middleware;

/// <summary>
///     Default implementation of <see cref="IProcessMiddlewareBuilder"/>.
///     Appends middleware in registration order and returns them as a read-only list.
/// </summary>
internal sealed class MiddlewareChainBuilder : IProcessMiddlewareBuilder
{
    private readonly List<IProcessMiddleware> _middleware = [];

    /// <inheritdoc />
    public IProcessMiddlewareBuilder Use(IProcessMiddleware middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        _middleware.Add(middleware);
        return this;
    }

    /// <inheritdoc />
    public IReadOnlyList<IProcessMiddleware> Build() => _middleware.AsReadOnly();
}
