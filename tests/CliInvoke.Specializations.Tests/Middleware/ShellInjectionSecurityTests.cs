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
using CliInvoke.Core;
using CliInvoke.Core.Factories;
using CliInvoke.Core.Middleware;
using CliInvoke.Factories;
using CliInvoke.Specializations;
using CliInvoke.Specializations.Middleware;
using TUnit.Core.Exceptions;

namespace CliInvoke.Specializations.Tests.Middleware;

/// <summary>
///     Regression tests for the command-injection (RCE) vulnerability in the PowerShell/cmd
///     wrappers. Historically the shell command was emitted as a single re-tokenized
///     <see cref="System.Diagnostics.ProcessStartInfo.Arguments"/> string, so a <c>"</c> in a
///     user-supplied target/argument broke the OS-level quoting and let the wrapped shell
///     reassemble a second command. The wrappers now deliver the command via
///     <see cref="System.Diagnostics.ProcessStartInfo.ArgumentList"/>, so metacharacters must be
///     treated as literal data.
/// </summary>
public class ShellInjectionSecurityTests
{
    /// <summary>
    ///     Captures the rewritten context produced by a middleware without actually starting a process.
    /// </summary>
    private sealed class CapturingNext
    {
        public InvocationContext? Captured { get; private set; }

        public Task Invoke(InvocationContext context)
        {
            Captured = context;
            return Task.CompletedTask;
        }
    }

    private static ProcessConfiguration MakeConfig(string target, string args)
        => ProcessConfigurationFactory.Create(target, args);

    [Test]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    public async Task PowerShell_Wrapper_EmitsVerbatimArgumentList_NotSingleArgumentsString()
    {
        // A target containing a double-quote and ampersand: under the old re-tokenized-Arguments
        // bug this would break OS-level quoting and inject a second command.
        ProcessConfiguration original = MakeConfig("prog\" & evil.exe & \"", "a & b | c");

        CapturingNext next = new();
        var middleware = new PowerShellMiddleware();

        await middleware.InvokeAsync(
            new InvocationContext(original, ProcessExitConfiguration.CreateGraceful(),
                InvocationMode.Buffered),
            next.Invoke);

        ProcessConfiguration rewritten = next.Captured!.Configuration;

        // The fix: the dangerous single-string path is empty and the wrapper is a verbatim list.
        await Assert.That(rewritten.Arguments).IsEqualTo(string.Empty);
        await Assert.That(rewritten.ArgumentList.Count).IsEqualTo(4);
        await Assert.That(rewritten.ArgumentList[0]).IsEqualTo("-NoProfile");
        await Assert.That(rewritten.ArgumentList[1]).IsEqualTo("-NonInteractive");
        await Assert.That(rewritten.ArgumentList[2]).IsEqualTo("-Command");

        // The -Command value is a single element, so the OS passes it to pwsh unmodified.
        string command = rewritten.ArgumentList[3];
        await Assert.That(command).StartsWith("& \"");
        // Metacharacters are neutralised by the shell-layer escaper, so no raw break remains.
        await Assert.That(command).Contains("`\"", StringComparison.Ordinal);
        await Assert.That(command).Contains("`&", StringComparison.Ordinal);
    }

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task Cmd_Wrapper_EmitsVerbatimArgumentList_NotSingleArgumentsString()
    {
        ProcessConfiguration original = MakeConfig("prog\" & evil.exe & \"", "a & b | c");

        CapturingNext next = new();
        var middleware = new CmdMiddleware();

        await middleware.InvokeAsync(
            new InvocationContext(original, ProcessExitConfiguration.CreateGraceful(),
                InvocationMode.Buffered),
            next.Invoke);

        ProcessConfiguration rewritten = next.Captured!.Configuration;

        await Assert.That(rewritten.Arguments).IsEqualTo(string.Empty);
        await Assert.That(rewritten.ArgumentList.Count).IsEqualTo(2);
        await Assert.That(rewritten.ArgumentList[0]).IsEqualTo("/c");

        string command = rewritten.ArgumentList[1];
        await Assert.That(command).StartsWith("\"");
        await Assert.That(command).Contains("^&", StringComparison.Ordinal);
    }

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
}
