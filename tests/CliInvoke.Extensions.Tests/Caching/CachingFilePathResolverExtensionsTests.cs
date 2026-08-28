/*
    CliInvoke.Extensions.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Core;
using CliInvoke.Extensions.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CliInvoke.Extensions.Tests.Caching;

/// <summary>
///     Tests for <see cref="CachingFilePathResolverExtensions.UseCachingFilePathResolver"/>: the
///     <see cref="IMemoryCache"/> Singleton registration and the decorator swap of the existing
///     <see cref="IFilePathResolver"/> (without a circular dependency).
/// </summary>
public class CachingFilePathResolverExtensionsTests
{
    private sealed class FakeResolver : IFilePathResolver
    {
        public int ResolveCount;

        public FileInfo ResolveFilePath(string filePathToResolve)
        {
            ResolveCount++;
            return new FileInfo(filePathToResolve);
        }

        public bool TryResolveFilePath(string filePathToResolve, out FileInfo? resolvedFilePath)
        {
            ResolveCount++;
            resolvedFilePath = new FileInfo(filePathToResolve);
            return true;
        }
    }

    [Test]
    public async Task UseCachingFilePathResolver_RegistersSingletonMemoryCache()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddCliInvoke();
        services.UseCachingFilePathResolver();

        IServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope1 = provider.CreateScope();
        using IServiceScope scope2 = provider.CreateScope();

        IMemoryCache cache1 = scope1.ServiceProvider.GetRequiredService<IMemoryCache>();
        IMemoryCache cache2 = scope2.ServiceProvider.GetRequiredService<IMemoryCache>();

        // The cache store is shared across scopes (D008).
        await Assert.That(cache1).IsSameReferenceAs(cache2);
    }

    [Test]
    public async Task UseCachingFilePathResolver_DecoratesExistingResolver_AndCaches()
    {
        IServiceCollection services = new ServiceCollection();
        var fake = new FakeResolver();
        services.AddCliInvoke();
        services.RemoveAll<IFilePathResolver>();
        services.AddSingleton<IFilePathResolver>(fake);
        services.UseCachingFilePathResolver();

        IServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        IFilePathResolver resolver = scope.ServiceProvider.GetRequiredService<IFilePathResolver>();

        await Assert.That(resolver).IsTypeOf<CachingFilePathResolver>();

        resolver.ResolveFilePath("tool");
        resolver.ResolveFilePath("tool");

        // Second resolve hits the cache, so the inner is only invoked once.
        await Assert.That(fake.ResolveCount).IsEqualTo(1);
    }

    [Test]
    public async Task UseCachingFilePathResolver_ResolvesWithoutCircularDependency()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddCliInvoke();
        services.UseCachingFilePathResolver();

        IServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();

        // Resolving IFilePathResolver must succeed: the inner is captured from the prior registration,
        // not via IFilePathResolver, so there is no circular dependency.
        IFilePathResolver resolver = scope.ServiceProvider.GetRequiredService<IFilePathResolver>();

        await Assert.That(resolver).IsTypeOf<CachingFilePathResolver>();
    }

    [Test]
    public async Task UseCachingFilePathResolver_WithoutExistingResolver_Throws()
    {
        IServiceCollection services = new ServiceCollection();

        await Assert.That(() => services.UseCachingFilePathResolver()).Throws<InvalidOperationException>();
    }
}
