/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Core.Middleware;

/// <summary>
///     Public builder for composing a sequence of <see cref="IProcessMiddleware"/> instances.
///     Supports direct middleware registration, type-based resolution, and conditional
///     sub-pipelines via <see cref="UseWhen(Func{InvocationContext, bool}, Action{IProcessMiddlewareBuilder})"/>.
/// </summary>
public sealed class ProcessMiddlewareBuilder : IProcessMiddlewareBuilder
{
    private readonly Func<Type, IProcessMiddleware> _resolver;
    private readonly List<IProcessMiddleware> _middleware = [];
    private readonly List<Type> _typedEntries = [];
    private readonly Dictionary<Type, IProcessMiddleware> _resolvedCache = [];

    /// <summary>
    ///     Initialises a new instance of the <see cref="ProcessMiddlewareBuilder"/> class
    ///     using an <see cref="IServiceProvider"/> to resolve middleware types.
    /// </summary>
    /// <param name="provider">The service provider used to resolve middleware types.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="provider"/> is <c>null</c>.</exception>
    public ProcessMiddlewareBuilder(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _resolver = type => (IProcessMiddleware)(provider.GetService(type)
            ?? throw new InvalidOperationException($"No service of type '{type.FullName}' has been registered."));
    }

    /// <summary>
    ///     Initialises a new instance of the <see cref="ProcessMiddlewareBuilder"/> class
    ///     using a custom resolver delegate.
    /// </summary>
    /// <param name="resolver">A delegate that resolves a <see cref="Type"/> to an <see cref="IProcessMiddleware"/> instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="resolver"/> is <c>null</c>.</exception>
    public ProcessMiddlewareBuilder(Func<Type, IProcessMiddleware> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolver = resolver;
    }

    /// <inheritdoc />
    public IProcessMiddlewareBuilder UseMiddleware(IProcessMiddleware middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        _middleware.Add(middleware);
        return this;
    }

    /// <inheritdoc />
    public IProcessMiddlewareBuilder UseMiddleware<T>() where T : IProcessMiddleware
    {
        _typedEntries.Add(typeof(T));
        return this;
    }

    /// <inheritdoc />
    public IProcessMiddlewareBuilder UseWhen(Func<InvocationContext, bool> predicate, Action<IProcessMiddlewareBuilder> configuration)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(configuration);

        return UseWhen(ctx => Task.FromResult(predicate(ctx)), configuration);
    }

    /// <inheritdoc />
    public IProcessMiddlewareBuilder UseWhen(Func<InvocationContext, Task<bool>> predicate, Action<IProcessMiddlewareBuilder> configuration)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(configuration);

        ProcessMiddlewareBuilder subBuilder = new ProcessMiddlewareBuilder(_resolver);
        configuration(subBuilder);

        IReadOnlyList<IProcessMiddleware> subPipeline = subBuilder.Build();

        return UseMiddleware(new ConditionalMiddleware(predicate, subPipeline));
    }

    /// <inheritdoc />
    public IReadOnlyList<IProcessMiddleware> Build()
    {
        List<IProcessMiddleware> result = new List<IProcessMiddleware>(_middleware.Count + _typedEntries.Count);
        result.AddRange(_middleware);

        foreach (Type type in _typedEntries)
        {
            if (!_resolvedCache.TryGetValue(type, out IProcessMiddleware? resolved))
            {
                resolved = _resolver(type) ?? throw new InvalidOperationException(
                    $"The resolver returned null for type '{type.FullName}'.");
                _resolvedCache[type] = resolved;
            }

            result.Add(resolved);
        }

        return result.AsReadOnly();
    }
}
