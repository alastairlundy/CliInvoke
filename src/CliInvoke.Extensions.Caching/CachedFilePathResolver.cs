/*
    AlastairLundy.CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;
using System.IO;
using System.Runtime.Versioning;

using AlastairLundy.CliInvoke;

using Microsoft.Extensions.Caching.Memory;

namespace CliInvoke.Extensions.Caching;

/// <summary>
/// Provides a mechanism for resolving file paths with caching support to optimize repeated file searches.
/// Extends the functionality of <see cref="FilePathResolver"/> by adding caching layers for performance improvements.
/// </summary>
public class CachedFilePathResolver : FilePathResolver
{
    private readonly IMemoryCache _cache;

    private const string PathExtCacheName = "PathExtData";
    private const string PathCacheName = "PathCacheData";
    
    private TimeSpan PathExtCacheLifespan { get; set; } = TimeSpan.FromHours(1.0);
    private TimeSpan PathCacheLifespan { get; set; } = TimeSpan.FromMinutes(3.0);

    /// <summary>
    /// Provides a mechanism for resolving file paths with enhanced performance by using
    /// an in-memory cache. This class extends the functionality of <see cref="FilePathResolver"/>
    /// and allows customizable cache lifespans for PATH contents and PATH environment variable extensions.
    /// </summary>
    public CachedFilePathResolver(IMemoryCache cache, TimeSpan? defaultPathCacheLifespan = null,
        TimeSpan? defaultPathExtCacheLifespan = null)
    {
        _cache = cache;

        if(defaultPathExtCacheLifespan.HasValue)
            PathExtCacheLifespan = defaultPathExtCacheLifespan.Value;
        
        if(defaultPathCacheLifespan.HasValue)
            PathCacheLifespan = defaultPathCacheLifespan.Value;
    }

    /// <summary>
    /// Resolves the full file path for a given file name or relative path. If the file path is not
    /// already absolute, it attempts to resolve it using a cached PATH environment variable
    /// lookup for improved performance.
    /// </summary>
    /// <param name="filePathToResolve">
    /// The name or relative path of the file to resolve. Must not be null or empty.
    /// </param>
    /// <returns>
    /// The fully resolved absolute path to the file if it can be located.
    /// </returns>
    /// <exception cref="FileNotFoundException">
    /// Thrown if the file cannot be located using the available directories or PATH environment variable.
    /// </exception>
    /// <exception cref="PlatformNotSupportedException">
    /// Thrown if the method is invoked on an unsupported platform.
    /// </exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public new string ResolveFilePath(string filePathToResolve)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrEmpty(filePathToResolve,  nameof(filePathToResolve));
#endif

        if (Path.IsPathRooted(filePathToResolve))
            return filePathToResolve;

        GetCombinedPathInfo(out string[]? pathContents, out string[] pathExtensions);
        
        bool resolveFromPath =
            ResolveFromPathEnvironmentVariable(filePathToResolve, pathContents, pathExtensions,
                out string? filePath);

        if (resolveFromPath && filePath is not null)
            return filePath;

        return LocateFileFromDirectory(filePathToResolve);
    }

    protected new bool GetCombinedPathInfo(out string[]? pathContents, out string[] pathExtensions)
    {
        bool foundPathExtensions = GetPathExtensionsInfo(out pathExtensions);
        bool foundPath = GetPathInfo(out pathContents);

        return foundPath && foundPathExtensions;
    }
    
    protected new bool GetPathExtensionsInfo(out string[] pathExtensions)
    {
        bool extensionsCached =
            _cache.TryGetValue<string[]>(PathExtCacheName, out string[]? extensions);

        if (extensionsCached && extensions is not null)
        {
            pathExtensions = extensions;
            return true;
        }

        bool result = base.GetPathExtensionsInfo(out pathExtensions);
    
        if(result)
            _cache.Set(PathExtCacheName, pathExtensions, PathExtCacheLifespan);

        return result;
    }

    protected new bool GetPathInfo(out string[]? pathContents)
    {
        bool pathCached =
            _cache.TryGetValue<string[]>(PathCacheName, out string[]? path);

        if (pathCached)
        {
            pathContents = path;
            return true;
        }
        
        bool result = base.GetPathInfo(out pathContents);
        
        if(result)
            _cache.Set(PathCacheName, pathContents, PathCacheLifespan);

        return result;
    }
}