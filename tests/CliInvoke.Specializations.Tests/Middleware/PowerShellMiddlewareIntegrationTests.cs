/*
    CliInvoke.Specializations.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CliInvoke.Factories;
using CliInvoke.Specializations.Middleware;
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
            string candidate = Path.Combine(directory.Trim(), fileName);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    [Test]
    public async Task UsePowerShell_RewritesConfigurationToTargetPwsh_ForRealInvocation()
    {
        string? pwshPath = ResolvePwshPath();

        if (pwshPath is null)
        {
            // Windows CI gap: PowerShell Core (pwsh) is not installed there.
            throw new SkipTestException(
                "PowerShell Core (pwsh) is not available on PATH; skipping PowerShell middleware integration test.");
        }

        ProcessInvoker invoker = new ProcessInvoker(new ExternalProcessFactory()).UsePowerShell();

        // The original target is `dotnet`, but PowerShellMiddleware rewrites the configuration
        // to run the command inside `pwsh -NoProfile -Command "..."`.
        using ProcessConfiguration config = ProcessConfigurationFactory.Create("dotnet", "--version");

        // PowerShellMiddleware rewrites the config so the real target becomes `pwsh`.
        // Verify the rewrite by confirming the process executed through pwsh (exit 0 with output).
        BufferedProcessResult result = await invoker.ExecuteBufferedAsync(
            config,
            ProcessExitConfiguration.CreateGraceful());

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.StandardOutput).IsNotNull();

        // The rewritten config's ExecutedFilePath should be pwsh (or pwsh.exe on Windows).
        string expectedPwshName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "pwsh.exe"
            : "pwsh";
        await Assert.That(result.ExecutedFilePath)
            .Contains(expectedPwshName, StringComparison.OrdinalIgnoreCase);
    }
}
