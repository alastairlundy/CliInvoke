/*
    CliInvoke.Specializations.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Threading.Tasks;
using CliInvoke.Extensions;
using CliInvoke.Specializations.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace CliInvoke.Specializations.Tests.DependencyInjection;

public class DependencyInjectionExtensionsTests
{
    [Test]
    public async Task AddCliInvokeSpecializations_WithUsePowerShell_ResolvesConfiguredInvoker()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddCliInvokeSpecializations();
        services.AddCliInvoke(builder => builder.UsePowerShell());
        using ServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        IProcessInvoker? invoker = scope.ServiceProvider.GetService<IProcessInvoker>();

        await Assert.That(invoker).IsNotNull();
        await Assert.That(invoker).IsTypeOf<ProcessInvoker>();
    }

    [Test]
    public async Task AddCliInvokeSpecializations_SingletonLifetime_ResolvesMiddlewareFromRootProvider()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddCliInvoke(ServiceLifetime.Singleton);
        services.AddCliInvokeSpecializations(ServiceLifetime.Singleton);
        using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });

        // The Scoped default would fail this resolution with scope validation enabled;
        // Singleton registrations resolve from the root (non-scoped) provider.
        IProcessInvoker? invoker = provider.GetService<IProcessInvoker>();
        PowerShellMiddleware? middleware = provider.GetService<PowerShellMiddleware>();

        await Assert.That(invoker).IsNotNull();
        await Assert.That(middleware).IsNotNull();
    }

    [Test]
    public async Task AddCliInvokeSpecializations_ConsumerRegisteredOptions_WinsOverDefault()
    {
        ShellMiddlewareOptions customOptions = new ShellMiddlewareOptions
        {
            WindowCreation = true
        };

        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(customOptions);
        services.AddCliInvokeSpecializations();
        services.AddCliInvoke(builder => builder.UsePowerShell());
        using ServiceProvider provider = services.BuildServiceProvider();

        ShellMiddlewareOptions? resolved = provider.GetService<ShellMiddlewareOptions>();

        await Assert.That(resolved).IsNotNull();
        await Assert.That(resolved).IsSameReferenceAs(customOptions);
        await Assert.That(resolved!.WindowCreation).IsTrue();
    }
}
