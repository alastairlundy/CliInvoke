/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CliInvoke.Core.Middleware;
using CliInvoke.Extensions;
using CliInvoke.Extensions.Middleware;
using CliInvoke.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace CliInvoke.Tests.DependencyInjection;

public class DependencyInjectionExtensionTests
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
        services.AddCliInvoke(invoker => invoker.UseLogging());
        IServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        IProcessInvoker? invoker = scope.ServiceProvider.GetService<IProcessInvoker>();

        await Assert.That(invoker).IsNotNull();
        await Assert.That(invoker).IsTypeOf<ProcessInvoker>();

        ProcessInvoker concreteInvoker = (ProcessInvoker)invoker;
        await Assert.That(concreteInvoker.Middlewares.Count).IsEqualTo(1);
        await Assert.That(concreteInvoker.Middlewares[0]).IsTypeOf<LoggingMiddleware>();
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
        services.AddCliInvoke(invoker => invoker.UseLogging(), ServiceLifetime.Singleton);
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
        services.AddCliInvoke(invoker => invoker.UseLogging(), ServiceLifetime.Scoped);
        IServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope1 = provider.CreateScope();
        using IServiceScope scope2 = provider.CreateScope();

        IProcessInvoker? invoker1 = scope1.ServiceProvider.GetService<IProcessInvoker>();
        IProcessInvoker? invoker2 = scope2.ServiceProvider.GetService<IProcessInvoker>();

        await Assert.That(invoker1).IsNotNull();
        await Assert.That(invoker2).IsNotNull();
        await Assert.That(invoker1).IsNotSameReferenceAs(invoker2);
    }

    [Test]
    public async Task AddCliInvoke_WithConfigure_Transient_RegistersAsTransient()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddCliInvoke(invoker => invoker.UseLogging(), ServiceLifetime.Transient);
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
        services.AddCliInvoke(invoker =>
        {
            ProcessInvoker result = invoker.UseLogging();
            return result;
        });
        IServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        IProcessInvoker? invoker = scope.ServiceProvider.GetService<IProcessInvoker>();

        await Assert.That(invoker).IsNotNull();

        ProcessInvoker concreteInvoker = (ProcessInvoker)invoker;
        await Assert.That(concreteInvoker.Middlewares.Count).IsEqualTo(1);
        await Assert.That(concreteInvoker.Middlewares[0]).IsTypeOf<LoggingMiddleware>();
    }

    [Test]
    public async Task AddCliInvoke_WithConfigure_MiddlewareRunsDuringExecution()
    {
        // Arrange
        CapturingLogger logger = new CapturingLogger();

        MiddlewareItems items = new MiddlewareItems();
        items.Set(LoggingMiddleware.LoggerKey, (Microsoft.Extensions.Logging.ILogger)logger);

        ProcessInvoker configuredInvoker = new ProcessInvoker(new ExternalProcessFactory(), items).UseLogging();

        (string filePath, string arguments) = ResolveEchoCommand();
        using ProcessConfiguration config = ProcessConfigurationFactory.Create(filePath, arguments);

        // Act
        BufferedProcessResult result = await configuredInvoker.ExecuteBufferedAsync(
            config,
            ProcessExitConfiguration.CreateGraceful());

        // Assert
        await Assert.That(result.ExitCode).IsEqualTo(0);

        bool hasEntryLog = logger.Entries.Any(e =>
            e.Level == Microsoft.Extensions.Logging.LogLevel.Information && e.Message.Contains(filePath));
        bool hasExitLog = logger.Entries.Any(e =>
            e.Level == Microsoft.Extensions.Logging.LogLevel.Information && e.Message.Contains("exited with code"));

        await Assert.That(hasEntryLog).IsTrue();
        await Assert.That(hasExitLog).IsTrue();
    }
}

/// <summary>
///     A minimal <see cref="Microsoft.Extensions.Logging.ILogger"/> implementation that captures log entries for assertions.
/// </summary>
internal sealed class CapturingLogger : Microsoft.Extensions.Logging.ILogger
{
    private readonly object _lock = new();
    private readonly List<(Microsoft.Extensions.Logging.LogLevel Level, string Message)> _entries = [];

    public IReadOnlyList<(Microsoft.Extensions.Logging.LogLevel Level, string Message)> Entries
    {
        get { lock (_lock) { return _entries.ToArray(); } }
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
        => NullScope.Instance;

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

    public void Log<TState>(
        Microsoft.Extensions.Logging.LogLevel logLevel,
        Microsoft.Extensions.Logging.EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        lock (_lock)
        {
            _entries.Add((logLevel, formatter(state, exception)));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
