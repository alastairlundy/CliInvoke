/*
    CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.IO;
using System.Runtime.Versioning;
using System.Collections.Generic;
using System.Linq;

namespace CliInvoke.Extensions.Caching;

/// <summary>
/// Provides a mechanism for resolving file paths with caching support to optimize repeated file searches.
/// Extends the functionality of <see cref="FilePathResolver"/> by adding caching layers for performance improvements.
/// </summary>
public class CachedFilePathResolver : FilePathResolver
{
    private readonly IMemoryCache _cache;

    private const string FilePathCachePrefix = "FilePath_";
    private const string PathExtCacheName = "PathExtData";
    private const string PathCacheName = "PathCacheData";
    
    private TimeSpan PathExtCacheLifespan { get; } = TimeSpan.FromHours(1.0);
    private TimeSpan PathCacheLifespan { get; } = TimeSpan.FromMinutes(3.0); 
    
    private TimeSpan FilePathCacheLifespan { get; } = TimeSpan.FromMinutes(10.0);

    /// <summary>
    /// Provides a mechanism for resolving file paths with enhanced performance by using
    /// an in-memory cache. This class extends the functionality of <see cref="FilePathResolver"/>
    /// and allows customizable cache lifespans for PATH contents and PATH environment variable extensions.
    /// </summary>
    public CachedFilePathResolver(IMemoryCache cache, TimeSpan? filePathCacheLifespan = null, TimeSpan? pathCacheLifespan = null,
        TimeSpan? pathExtCacheLifespan = null)
    {
        _cache = cache;

        if(filePathCacheLifespan.HasValue)
            FilePathCacheLifespan = filePathCacheLifespan.Value;
        
        if(pathExtCacheLifespan.HasValue)
            PathExtCacheLifespan = pathExtCacheLifespan.Value;
        
        if(pathCacheLifespan.HasValue)
            PathCacheLifespan = pathCacheLifespan.Value;
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
        ArgumentException.ThrowIfNullOrEmpty(filePathToResolve);
        
        if (Path.IsPathRooted(filePathToResolve))
            return filePathToResolve;
        
        bool isCached = _cache.TryGetValue($"{FilePathCachePrefix}{filePathToResolve}", out string? path);

        if (isCached && path is not null && File.Exists(path))
            return path;
        
        bool resolveFromPath =
            ResolveFromPathEnvironmentVariable(filePathToResolve, out FileInfo? filePath);

        string output;

        if (resolveFromPath && filePath is not null)
        {
            output = filePath.FullName;
        }
        else
        {
            output = LocateFileFromDirectory(filePathToResolve).FullName;
        }
        
        _cache.Set($"{FilePathCachePrefix}{filePathToResolve}", output, FilePathCacheLifespan);    
        
        return output;
    }
    
    protected new string[] GetPathExtensionsInfo()
    {
        bool extensionsCached = _cache.TryGetValue<string[]>(PathExtCacheName, out string[]? extensions);

        if (extensionsCached && extensions is not null)
            return extensions;

        string[] output = base.GetPathExtensionsInfo();
    
        _cache.Set(PathExtCacheName, output, PathExtCacheLifespan);

        return output;
    }

    protected new IEnumerable<string>? GetPathInfo()
    {
        bool pathCached = _cache.TryGetValue<string[]>(PathCacheName, out string[]? path);

        if (pathCached)
            return path;
        
        string[]? output = base.GetPathInfo()?.ToArray();
        
        _cache.Set(PathCacheName, output, PathCacheLifespan);

        return output;
    }
}