/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Core.Factories;
using CliInvoke.Core.Middleware;
using CliInvoke.Extensions;
using CliInvoke.Specializations;
using Microsoft.Extensions.DependencyInjection;

namespace CliInvoke.Tests.Middleware;

/// <summary>
///     Covers the shell-wrapping contract of the platform middleware through the public
///     registration surface: every PowerShell wrap uses the shared safe switch set
///     (<c>-NoProfile -NonInteractive -Command</c>) no matter which middleware produced it,
///     and a registered <see cref="ShellMiddlewareOptions"/> instance is honoured.
/// </summary>
public class ShellMiddlewareWrappingTests
{
    /// <summary>
    ///     A shell detector that always reports a fixed shell, so <c>UseDefaultShell()</c>
    ///     wraps deterministically without probing the machine's real default shell.
    /// </summary>
    private sealed class FixedShellDetector : IShellDetector
    {
        private readonly ShellInformation _shell;

        public FixedShellDetector(string name, string targetFilePath)
        {
            _shell = new ShellInformation(name, new FileInfo(targetFilePath), new Version(7, 4, 0));
        }

        public Task<ShellInformation> ResolveDefaultShellAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_shell);
        }
    }

    /// <summary>
    ///     Runs one buffered invocation through a shell middleware chain whose terminal stage is a
    ///     stub process factory, and returns the configuration the factory received, i.e. the
    ///     configuration after the shell middleware rewrote it. No real process is ever started.
    /// </summary>
    private static async Task<ProcessConfiguration> RunThroughShellMiddlewareAsync(
        Action<IProcessMiddlewareBuilder> configure,
        ShellMiddlewareOptions? options = null,
        IShellDetector? shellDetector = null)
    {
        IServiceCollection services = new ServiceCollection();

        // The options instance must be registered before AddCliInvokeSpecializations(),
        // whose TryAdd registration then leaves the consumer instance in place.
        if (options is not null)
            services.AddSingleton(options);

        services.AddCliInvokeSpecializations();
        services.AddCliInvoke(configure);

        // Registered after AddCliInvoke so these consumer registrations win over its defaults
        // (the last registration for a service type is the one the container resolves).
        CountingExternalProcessFactory factory = new CountingExternalProcessFactory();
        services.AddSingleton<IExternalProcessFactory>(factory);

        if (shellDetector is not null)
            services.AddSingleton<IShellDetector>(shellDetector);

        IServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        IProcessInvoker invoker = scope.ServiceProvider.GetRequiredService<IProcessInvoker>();

        ProcessConfiguration config = new ProcessConfiguration("echo", "hello from shell wrapping");
        await invoker.ExecuteBufferedAsync(config, ProcessExitConfiguration.CreateGraceful());

        await Assert.That(factory.LastConfiguration).IsNotNull();
        return factory.LastConfiguration!;
    }

    [Test]
    public async Task UsePowerShell_WrapsWithNoProfileNonInteractiveCommandSwitches()
    {
        ProcessConfiguration wrapped = await RunThroughShellMiddlewareAsync(
            builder => builder.UsePowerShell());

        await Assert.That(wrapped.TargetFilePath)
            .IsEqualTo(OperatingSystem.IsWindows() ? "pwsh.exe" : "pwsh");
        await Assert.That(wrapped.ArgumentList.Count).IsEqualTo(4);
        await Assert.That(wrapped.ArgumentList[0]).IsEqualTo("-NoProfile");
        await Assert.That(wrapped.ArgumentList[1]).IsEqualTo("-NonInteractive");
        await Assert.That(wrapped.ArgumentList[2]).IsEqualTo("-Command");
        await Assert.That(wrapped.ArgumentList[3].Contains("hello", StringComparison.Ordinal))
            .IsTrue();
    }

    [Test]
    public async Task UseDefaultShell_DetectedPowerShell_UsesSameSafeSwitchesAsUsePowerShell()
    {
        string shellPath = Path.Combine(Path.GetTempPath(), "pwsh");

        ProcessConfiguration wrapped = await RunThroughShellMiddlewareAsync(
            builder => builder.UseDefaultShell(),
            shellDetector: new FixedShellDetector("pwsh", shellPath));

        await Assert.That(wrapped.TargetFilePath).IsEqualTo(new FileInfo(shellPath).FullName);
        await Assert.That(wrapped.ArgumentList.Count).IsEqualTo(4);
        await Assert.That(wrapped.ArgumentList[0]).IsEqualTo("-NoProfile");
        await Assert.That(wrapped.ArgumentList[1]).IsEqualTo("-NonInteractive");
        await Assert.That(wrapped.ArgumentList[2]).IsEqualTo("-Command");
        await Assert.That(wrapped.ArgumentList[3].Contains("hello", StringComparison.Ordinal))
            .IsTrue();
    }

    [Test]
    public async Task UseDefaultShell_DetectedPosixShell_WrapsWithDashC()
    {
        string shellPath = Path.Combine(Path.GetTempPath(), "sh");

        ProcessConfiguration wrapped = await RunThroughShellMiddlewareAsync(
            builder => builder.UseDefaultShell(),
            shellDetector: new FixedShellDetector("sh", shellPath));

        await Assert.That(wrapped.TargetFilePath).IsEqualTo(new FileInfo(shellPath).FullName);
        await Assert.That(wrapped.ArgumentList.Count).IsEqualTo(2);
        await Assert.That(wrapped.ArgumentList[0]).IsEqualTo("-c");
    }

    [Test]
    public async Task RegisteredShellMiddlewareOptionsInstance_IsHonouredByUsePowerShell()
    {
        ShellMiddlewareOptions options = new ShellMiddlewareOptions
        {
            WindowCreation = true,
            UseShellExecution = true
        };

        ProcessConfiguration wrapped = await RunThroughShellMiddlewareAsync(
            builder => builder.UsePowerShell(),
            options: options);

        await Assert.That(wrapped.WindowCreation).IsTrue();
        await Assert.That(wrapped.UseShellExecution).IsTrue();
    }
}
