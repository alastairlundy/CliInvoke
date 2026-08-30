/*
    CliInvoke.Extensions.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Extensions.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace CliInvoke.Extensions.Tests.Caching;

/// <summary>
///     Tests for <see cref="CachingFilePathResolver"/>: cache-by-raw-target behaviour, PATH-first
///     miss delegation, and that repeated resolves hit the cache.
/// </summary>
public class CachingFilePathResolverTests
{
    private sealed class FakeResolver : IFilePathResolver
    {
        private readonly string _resolvedPath;

        public FakeResolver(string resolvedPath)
        {
            _resolvedPath = resolvedPath;
        }

        public int ResolveCount;

        public int TryResolveCount;

        public FileInfo ResolveFilePath(string filePathToResolve)
        {
            ResolveCount++;
            return new FileInfo(_resolvedPath);
        }

        public bool TryResolveFilePath(string filePathToResolve, out FileInfo? resolvedFilePath)
        {
            TryResolveCount++;
            resolvedFilePath = new FileInfo(_resolvedPath);
            return true;
        }
    }

    [Test]
    public async Task ResolveFilePath_CachesAndDelegatesOnlyOnMiss()
    {
        // Use a real, existing file so the cache's existence re-verification passes on the hit.
        string existing = typeof(CachingFilePathResolverTests).Assembly.Location;
        var inner = new FakeResolver(existing);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var resolver = new CachingFilePathResolver(inner, cache);

        FileInfo first = resolver.ResolveFilePath("tool");
        FileInfo second = resolver.ResolveFilePath("tool");

        // The inner resolver runs once; the second call is served from cache (re-verified via File.Exists).
        await Assert.That(inner.ResolveCount).IsEqualTo(1);
        await Assert.That(first.FullName).IsEqualTo(second.FullName);
        await Assert.That(first.Exists).IsTrue();
    }

    [Test]
    public async Task TryResolveFilePath_CachesAndDelegatesOnlyOnMiss()
    {
        string existing = typeof(CachingFilePathResolverTests).Assembly.Location;
        var inner = new FakeResolver(existing);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var resolver = new CachingFilePathResolver(inner, cache);

        bool firstOk = resolver.TryResolveFilePath("tool", out FileInfo? first);
        bool secondOk = resolver.TryResolveFilePath("tool", out FileInfo? second);

        await Assert.That(firstOk).IsTrue();
        await Assert.That(secondOk).IsTrue();
        await Assert.That(inner.TryResolveCount).IsEqualTo(1);
        await Assert.That(first!.FullName).IsEqualTo(second!.FullName);
    }

    [Test]
    public async Task ResolveFilePath_MissDelegatesToInner()
    {
        string existing = typeof(CachingFilePathResolverTests).Assembly.Location;
        var inner = new FakeResolver(existing);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var resolver = new CachingFilePathResolver(inner, cache);

        FileInfo result = resolver.ResolveFilePath("tool");

        await Assert.That(inner.ResolveCount).IsEqualTo(1);
        await Assert.That(result.FullName).IsEqualTo(existing);
    }
}
