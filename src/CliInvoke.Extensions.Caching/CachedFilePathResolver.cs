using System;
using System.IO;

using AlastairLundy.CliInvoke;

using Microsoft.Extensions.Caching.Memory;

namespace CliInvoke.Extensions.Caching;

/// <summary>
/// 
/// </summary>
public class CachedFilePathResolver : FilePathResolver
{
    private readonly IMemoryCache _cache;

    private const string PathExtCacheName = "PathExtData";
    private const string PathCacheName = "PathCacheData";
    
    private TimeSpan PathExtCacheLifespan { get; set; } = TimeSpan.FromHours(1.0);
    private TimeSpan PathCacheLifespan { get; set; } = TimeSpan.FromMinutes(3.0);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cache"></param>
    /// <param name="defaultPathExtCacheLifespan"></param>
    /// <param name="defaultPathCacheLifespan"></param>
    public CachedFilePathResolver(IMemoryCache cache, TimeSpan? defaultPathExtCacheLifespan = null,
        TimeSpan? defaultPathCacheLifespan = null)
    {
        _cache = cache;
        
        if(defaultPathExtCacheLifespan.HasValue)
            PathExtCacheLifespan = defaultPathExtCacheLifespan.Value;
        
        if(defaultPathCacheLifespan.HasValue)
            PathCacheLifespan = defaultPathCacheLifespan.Value;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePathToResolve"></param>
    /// <returns></returns>
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