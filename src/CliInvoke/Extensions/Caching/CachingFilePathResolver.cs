/*
    CliInvoke.Extensions
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using Microsoft.Extensions.Caching.Memory;

namespace CliInvoke.Extensions.Caching;

/// <summary>
///     A decorator around <see cref="IFilePathResolver"/> that caches resolved absolute
///     <see cref="FileInfo"/> paths, keyed on the raw target, delegating to the inner resolver
///     (PATH-first per GLOSSARY DD1) on a cache miss.
/// </summary>
/// <remarks>
///     Lives in <c>CliInvoke.Extensions</c>; <c>CliInvoke.Core</c> remains free of caching concerns
///     (see DECISIONS-CliInvoke-middleware-truncation-caching-retry.md).
/// </remarks>
public sealed class CachingFilePathResolver : IFilePathResolver
{
    private readonly IFilePathResolver _inner;
    private readonly IMemoryCache _cache;
    private readonly CachingFilePathResolverOptions _options;

    /// <summary>
    ///     Initialises a new instance of the <see cref="CachingFilePathResolver"/> class.
    /// </summary>
    /// <param name="inner">The resolver delegated to on a cache miss. Must use PATH-first resolution.</param>
    /// <param name="cache">The shared memory cache (typically registered as a Singleton).</param>
    /// <param name="options">Optional caching options; defaults to <see cref="CachingFilePathResolverOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="inner"/> or <paramref name="cache"/> is <c>null</c>.</exception>
    public CachingFilePathResolver(
        IFilePathResolver inner,
        IMemoryCache cache,
        CachingFilePathResolverOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(cache);

        _inner = inner;
        _cache = cache;
        _options = options ?? CachingFilePathResolverOptions.Default;
    }

    /// <inheritdoc />
    public FileInfo ResolveFilePath(string filePathToResolve)
    {
        if (_cache.TryGetValue<FileInfo>(filePathToResolve, out FileInfo? cached) && cached is not null)
            return cached;

        FileInfo resolved = _inner.ResolveFilePath(filePathToResolve);

        Cache(filePathToResolve, resolved);

        return resolved;
    }

    /// <inheritdoc />
    public bool TryResolveFilePath(string filePathToResolve, out FileInfo? resolvedFilePath)
    {
        if (_cache.TryGetValue<FileInfo>(filePathToResolve, out FileInfo? cached) && cached is not null)
        {
            resolvedFilePath = cached;
            return true;
        }

        if (_inner.TryResolveFilePath(filePathToResolve, out FileInfo? innerResolved) && innerResolved is not null)
        {
            Cache(filePathToResolve, innerResolved);
            resolvedFilePath = innerResolved;
            return true;
        }

        resolvedFilePath = null;
        return false;
    }

    private void Cache(string key, FileInfo value)
    {
        MemoryCacheEntryOptions entryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _options.AbsoluteExpirationRelativeToNow,
            Size = 1
        };

        if (_options.PostEvictionCallback is not null)
            entryOptions.RegisterPostEvictionCallback(_options.PostEvictionCallback);

        _cache.Set(key, value, entryOptions);
    }
}