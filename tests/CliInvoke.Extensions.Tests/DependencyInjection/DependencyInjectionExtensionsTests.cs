/*
    CliInvoke.Extensions.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Runtime.InteropServices;
using CliInvoke.Core.Middleware;
using CliInvoke.Extensions;
using CliInvoke.Extensions.Middleware;
using CliInvoke.Extensions.Middleware.Validation;
using CliInvoke.Factories;
using CliInvoke.Specializations.Middleware;
using CliInvoke.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace CliInvoke.Extensions.Tests.DependencyInjection;

public class DependencyInjectionExtensionsTests
{
    private static (string FilePath, string Arguments) ResolveEchoCommand()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return ("cmd.exe", "/C echo Hello from CliInvoke");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            return ("/bin/echo", "Hello from CliInvoke");

        throw new PlatformNotSupportedException("Unsupported OS for the echo integration test.");
    }

    [Test]
    public async Task AddCliInvoke_WithoutConfigure_RegistersDefaultInvoker()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddCliInvoke();
        IServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        IProcessInvoker? invoker = scope.ServiceProvider.GetService<IProcessInvoker>();

        await Assert.That(invoker).IsNotNull();
        await Assert.That(invoker).IsTypeOf<ProcessInvoker>();
    }

    [Test]
    public async Task AddCliInvoke_WithConfigure_RegistersConfiguredInvoker()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddCliInvoke(builder => builder.UseLogging());
        IServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        IProcessInvoker? invoker = scope.ServiceProvider.GetService<IProcessInvoker>();

        await Assert.That(invoker).IsNotNull();
        await Assert.That(invoker).IsTypeOf<ProcessInvoker>();
    }

    [Test]
    public async Task AddCliInvoke_WithConfigure_NullConfigure_ThrowsArgumentNullException()
    {
        IServiceCollection services = new ServiceCollection();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
        {
            services.AddCliInvoke(configure: null!);
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task AddCliInvoke_WithConfigure_Singleton_RegistersAsSingleton()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddCliInvoke(builder => builder.UseLogging(), ServiceLifetime.Singleton);
        IServiceProvider provider = services.BuildServiceProvider();

        IProcessInvoker? invoker1 = provider.GetService<IProcessInvoker>();
        IProcessInvoker? invoker2 = provider.GetService<IProcessInvoker>();

        await Assert.That(invoker1).IsNotNull();
        await Assert.That(invoker2).IsNotNull();
        await Assert.That(invoker1).IsSameReferenceAs(invoker2);
    }

    [Test]
    public async Task AddCliInvoke_WithConfigure_Scoped_RegistersAsScoped()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddCliInvoke(builder => builder.UseLogging(), ServiceLifetime.Scoped);
        IServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope1 = provider.CreateScope();
        using IServiceScope scope2 = provider.CreateScope();

        IProcessInvoker? invoker1a = scope1.ServiceProvider.GetService<IProcessInvoker>();
        IProcessInvoker? invoker1b = scope1.ServiceProvider.GetService<IProcessInvoker>();
        IProcessInvoker? invoker2 = scope2.ServiceProvider.GetService<IProcessInvoker>();

        await Assert.That(invoker1a).IsNotNull();
        await Assert.That(invoker1b).IsNotNull();
        await Assert.That(invoker2).IsNotNull();
        await Assert.That(invoker1a).IsSameReferenceAs(invoker1b);
        await Assert.That(invoker1a).IsNotSameReferenceAs(invoker2);
    }

    [Test]
    public async Task AddCliInvoke_WithConfigure_Transient_RegistersAsTransient()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddCliInvoke(builder => builder.UseLogging(), ServiceLifetime.Transient);
        IServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        IProcessInvoker? invoker1 = scope.ServiceProvider.GetService<IProcessInvoker>();
        IProcessInvoker? invoker2 = scope.ServiceProvider.GetService<IProcessInvoker>();

        await Assert.That(invoker1).IsNotNull();
        await Assert.That(invoker2).IsNotNull();
        await Assert.That(invoker1).IsNotSameReferenceAs(invoker2);
    }

    [Test]
    public async Task AddCliInvoke_WithConfigure_ChainedMiddleware_AllApplied()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddCliInvoke(builder =>
        {
            builder.UseLogging();
            builder.UsePostExitValidation(PostExitValidation.ExitCodeIsZero());
        });
        IServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        IProcessInvoker? invoker = scope.ServiceProvider.GetService<IProcessInvoker>();

        await Assert.That(invoker).IsNotNull();
        await Assert.That(invoker).IsTypeOf<ProcessInvoker>();
    }

    [Test]
    public async Task AddCliInvoke_WithConfigure_MiddlewareRunsDuringExecution()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddCliInvoke(builder => builder.UseLogging());
        IServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        IProcessInvoker invoker = scope.ServiceProvider.GetRequiredService<IProcessInvoker>();

        (string filePath, string arguments) = ResolveEchoCommand();
        ProcessConfiguration config = ProcessConfigurationFactory.Create(filePath, arguments);

        BufferedProcessResult result = await invoker.ExecuteBufferedAsync(
            config,
            ProcessExitConfiguration.CreateGraceful());

        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task AddCliInvoke_WithConfigure_UsePowerShell_RegistersConfiguredInvoker()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddCliInvoke(builder => builder.UsePowerShell());
        IServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        IProcessInvoker? invoker = scope.ServiceProvider.GetService<IProcessInvoker>();

        await Assert.That(invoker).IsNotNull();
        await Assert.That(invoker).IsTypeOf<ProcessInvoker>();
    }

    [Test]
    public async Task AddCliInvoke_WithConfigure_UseCmd_RegistersConfiguredInvoker()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddCliInvoke(builder => builder.UseCmd());
        IServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        IProcessInvoker? invoker = scope.ServiceProvider.GetService<IProcessInvoker>();

        await Assert.That(invoker).IsNotNull();
        await Assert.That(invoker).IsTypeOf<ProcessInvoker>();
    }

    [Test]
    public async Task AddCliInvoke_WithConfigure_AllBuiltInMiddleware_RegistersConfiguredInvoker()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddCliInvoke(builder =>
        {
            builder.UseLogging();
            builder.UsePostExitValidation(PostExitValidation.ExitCodeIsZero());
            builder.UsePowerShell();
            builder.UseCmd();
        });
        IServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        IProcessInvoker? invoker = scope.ServiceProvider.GetService<IProcessInvoker>();

        await Assert.That(invoker).IsNotNull();
        await Assert.That(invoker).IsTypeOf<ProcessInvoker>();
    }
}
