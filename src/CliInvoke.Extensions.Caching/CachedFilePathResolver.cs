using System;

using AlastairLundy.CliInvoke;

using Microsoft.Extensions.Caching.Memory;

namespace CliInvoke.Extensions.Caching;

/// <summary>
/// 
/// </summary>
public class CachedFilePathResolver : FilePathResolver
{
    private readonly IMemoryCache _cache;

    private const string PathExtName = "PathExtData";
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

    public new string ResolveFilePath(string filePathToResolve)
    {
       
        
    }

}