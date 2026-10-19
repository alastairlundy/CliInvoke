/*
    CliInvoke.Specializations.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using CliInvoke.Extensions;
using CliInvoke.Specializations.Middleware;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core.Exceptions;

namespace CliInvoke.Specializations.Tests.Middleware;

public class PowerShellMiddlewareIntegrationTests
{
    /// <summary>
    ///     Resolves the absolute path to a <c>pwsh</c> executable on the system PATH, or
    ///     <c>null</c> when PowerShell (Core) is not installed.
    /// </summary>
    private static string? ResolvePwshPath()
    {
        string fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "pwsh.exe"
            : "pwsh";

        string? pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (pathEnv is null)
            return null;

        foreach (string directory in pathEnv.Split(Path.PathSeparator))
        {
            string trimmed = directory.Trim();
            if (trimmed.Length == 0)
                continue;

            string candidate = Path.Combine(trimmed, fileName);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    /// <summary>
    ///     Builds a fresh DI provider with <c>UsePowerShell()</c> middleware and runs
    ///     <c>dotnet --version</c> through the invoker, so the middleware rewrites the
    ///     target to <c>pwsh -NoProfile -Command</c>.
    /// </summary>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    private static async Task<BufferedProcessResult> ExecuteDotnetVersionThroughPwshAsync()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddCliInvokeSpecializations()
            .AddCliInvoke(builder => builder.UsePowerShell())
            .BuildServiceProvider();

        IProcessInvoker invoker = provider.GetRequiredService<IProcessInvoker>();

        ProcessConfiguration config = new ProcessConfiguration("dotnet", "--version");

        BufferedProcessResult result = await invoker.ExecuteBufferedAsync(
            config,
            ProcessExitConfiguration.CreateGraceful());

        await provider.DisposeAsync().ConfigureAwait(false);

        return result;
    }

    [Test]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    public async Task UsePowerShell_RewritesConfigurationToTargetPwsh_ForRealInvocation()
    {
        string? pwshPath = ResolvePwshPath();

        if (pwshPath is null)
        {
            // Windows CI gap: PowerShell Core (pwsh) is not installed there.
            throw new SkipTestException(
                "PowerShell Core (pwsh) is not available on PATH; skipping PowerShell middleware integration test.");
        }

        // The original target is `dotnet`, but the PowerShellMiddleware registered via
        // `UsePowerShell()` rewrites the configuration to run the command inside
        // `pwsh -NoProfile -Command "..."`. We wire the middleware onto a normal
        // ProcessInvoker through the supported DI entry points; the dedicated
        // `PowershellProcessInvoker` type was removed in favour of this pipeline.
        BufferedProcessResult result = await ExecuteDotnetVersionThroughPwshAsync();

        // pwsh occasionally dies from a signal on hosted Linux runners (observed
        // SIGABRT/exit 134 on ubuntu-latest) before producing any output. That is an
        // environment-level process crash, not a middleware-rewrite failure, so retry
        // once; a genuine rewrite failure reproduces on the retry. Exit codes above 127
        // on Unix are reserved for signal deaths (128 + signal number).
        if (OperatingSystem.IsLinux() && result.ExitCode > 127)
        {
            result = await ExecuteDotnetVersionThroughPwshAsync();
        }

        // Fail with captured output first so a crash like the one above is diagnosable
        // from the test log instead of showing only the exit code.
        if (result.ExitCode != 0)
        {
            Assert.Fail(
                $"pwsh invocation exited with {result.ExitCode}. " +
                $"stdout: '{result.StandardOutput}'; stderr: '{result.StandardError}'");
        }

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.StandardOutput).IsNotNull();

        // The rewritten config's ExecutedFilePath should be pwsh (or pwsh.exe on Windows).
        string expectedPwshName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "pwsh.exe"
            : "pwsh";
        await Assert.That(result.ExecutedFilePath)
            .Contains(expectedPwshName, StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task ShellMiddlewareOptions_Default_HasExpectedValues()
    {
        ShellMiddlewareOptions options = ShellMiddlewareOptions.Default;

        await Assert.That(options.WindowCreation).IsFalse();
        await Assert.That(options.UseShellExecution).IsFalse();
    }

    [Test]
    public async Task ShellMiddlewareOptions_CanCustomiseProperties()
    {
        ShellMiddlewareOptions options = new ShellMiddlewareOptions
        {
            WindowCreation = true,
            UseShellExecution = true
        };

        await Assert.That(options.WindowCreation).IsTrue();
        await Assert.That(options.UseShellExecution).IsTrue();
    }
}
