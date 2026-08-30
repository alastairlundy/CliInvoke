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
///     Configuration options for the cached file-path resolver.
/// </summary>
/// <remarks>
///     Register an instance of this type in the dependency injection container to customise caching
///     behaviour. When no instance is registered, <see cref="Default"/> is used.
/// </remarks>
public sealed class CachingFilePathResolverOptions
{
    /// <summary>
    ///     Gets the default options instance (SizeLimit = 512, AbsoluteExpirationRelativeToNow = 5 minutes).
    /// </summary>
    public static CachingFilePathResolverOptions Default { get; } = new();

    /// <summary>
    ///     Gets or sets the suggested maximum number of entries for the shared cache
    ///     (applied to <c>MemoryCacheOptions.SizeLimit</c>).
    /// </summary>
    public int SizeLimit { get; set; } = 512;

    /// <summary>
    ///     Gets or sets the absolute expiration applied to cached resolved paths relative to now.
    /// </summary>
    public TimeSpan AbsoluteExpirationRelativeToNow { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    ///     Gets or sets an optional callback invoked when a cached entry is evicted, used as the
    ///     invalidation hook.
    /// </summary>
    /// <remarks>
    ///     Matches the delegate accepted by <c>MemoryCacheEntryOptions.RegisterPostEvictionCallback</c>,
    ///     so the resolver can forward it directly to the cache.
    /// </remarks>
    public PostEvictionDelegate? PostEvictionCallback { get; set; }
}
