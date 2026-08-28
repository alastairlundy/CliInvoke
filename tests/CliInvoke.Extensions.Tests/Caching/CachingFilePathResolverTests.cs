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
        public int ResolveCount;

        public int TryResolveCount;

        public FileInfo ResolveFilePath(string filePathToResolve)
        {
            ResolveCount++;
            return new FileInfo(filePathToResolve);
        }

        public bool TryResolveFilePath(string filePathToResolve, out FileInfo? resolvedFilePath)
        {
            TryResolveCount++;
            resolvedFilePath = new FileInfo(filePathToResolve);
            return true;
        }
    }

    [Test]
    public async Task ResolveFilePath_CachesAndDelegatesOnlyOnMiss()
    {
        var inner = new FakeResolver();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var resolver = new CachingFilePathResolver(inner, cache);

        FileInfo first = resolver.ResolveFilePath("tool");
        FileInfo second = resolver.ResolveFilePath("tool");

        await Assert.That(inner.ResolveCount).IsEqualTo(1);
        await Assert.That(first).IsSameReferenceAs(second);
    }

    [Test]
    public async Task TryResolveFilePath_CachesAndDelegatesOnlyOnMiss()
    {
        var inner = new FakeResolver();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var resolver = new CachingFilePathResolver(inner, cache);

        bool firstOk = resolver.TryResolveFilePath("tool", out FileInfo? first);
        bool secondOk = resolver.TryResolveFilePath("tool", out FileInfo? second);

        await Assert.That(firstOk).IsTrue();
        await Assert.That(secondOk).IsTrue();
        await Assert.That(inner.TryResolveCount).IsEqualTo(1);
        await Assert.That(first).IsSameReferenceAs(second);
    }

    [Test]
    public async Task ResolveFilePath_MissDelegatesToInner()
    {
        var inner = new FakeResolver();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var resolver = new CachingFilePathResolver(inner, cache);

        FileInfo result = resolver.ResolveFilePath("tool");

        await Assert.That(inner.ResolveCount).IsEqualTo(1);
        await Assert.That(result.Name).IsEqualTo("tool");
    }
}
