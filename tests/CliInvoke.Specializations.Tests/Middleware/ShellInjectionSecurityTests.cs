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
using CliInvoke.Specializations.Middleware;

namespace CliInvoke.Specializations.Tests.Middleware;

/// <summary>
///     Regression tests for shell command composition. The middleware must use the
///     shell-aware <c>ShellRewriter</c> output, preserve caller-supplied argument lists,
///     and prevent shell metacharacters from becoming additional commands.
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
        => new ProcessConfiguration(target, args);

    [Test]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    public async Task PowerShell_Wrapper_PreventsCommandInjectionFromTargetAndArguments()
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
        // The target is quoted; an embedded quote stays backtick-escaped and the
        // ampersands remain literal data inside the quoted string, so no raw break
        // or second command can materialise.
        await Assert.That(command).Contains("`\"", StringComparison.Ordinal);
        await Assert.That(command).Contains("& evil.exe", StringComparison.Ordinal);
        await Assert.That(command.Contains("`& evil.exe", StringComparison.Ordinal)).IsFalse();
        await Assert.That(command).Contains("a `& b `| c", StringComparison.Ordinal);
    }

    [Test]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    public async Task PowerShell_Wrapper_PreservesArgumentListAndIgnoresArguments()
    {
        ProcessConfiguration original = new("prog.exe", "ignored")
        {
            ArgumentList = ["first", "second;evil.exe"]
        };

        CapturingNext next = new();
        PowerShellMiddleware middleware = new();

        await middleware.InvokeAsync(
            new InvocationContext(original, ProcessExitConfiguration.CreateGraceful(),
                InvocationMode.Buffered),
            next.Invoke);

        ProcessConfiguration rewritten = next.Captured!.Configuration;

        await Assert.That(rewritten.Arguments).IsEqualTo(string.Empty);
        await Assert.That(rewritten.ArgumentList.Count).IsEqualTo(4);
        await Assert.That(rewritten.ArgumentList[3])
            .IsEqualTo("& \"prog.exe\" first second`;evil.exe");
    }

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task Cmd_Wrapper_PreventsCommandInjectionFromTargetAndArguments()
    {
        ProcessConfiguration original = MakeConfig("prog\" & evil.exe & \"", "a & b | c");

        CapturingNext next = new();
        CmdMiddleware middleware = new();

        await middleware.InvokeAsync(
            new InvocationContext(original, ProcessExitConfiguration.CreateGraceful(),
                InvocationMode.Buffered),
            next.Invoke);

        ProcessConfiguration rewritten = next.Captured!.Configuration;

        // cmd.exe applies its own quote-stripping rules, so it receives a single
        // escaped Arguments string rather than a ProcessStartInfo.ArgumentList.
        await Assert.That(rewritten.ArgumentList.Count).IsEqualTo(0);
        await Assert.That(rewritten.Arguments).StartsWith("/c \"");
        await Assert.That(rewritten.Arguments).Contains("^&", StringComparison.Ordinal);
        await Assert.That(rewritten.Arguments).Contains("^|", StringComparison.Ordinal);
    }

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task Cmd_Wrapper_PreservesArgumentListAndIgnoresArguments()
    {
        ProcessConfiguration original = new("prog.exe", "ignored")
        {
            ArgumentList = ["first", "second&evil.exe"]
        };

        CapturingNext next = new();
        CmdMiddleware middleware = new();

        await middleware.InvokeAsync(
            new InvocationContext(original, ProcessExitConfiguration.CreateGraceful(),
                InvocationMode.Buffered),
            next.Invoke);

        ProcessConfiguration rewritten = next.Captured!.Configuration;

        await Assert.That(rewritten.ArgumentList.Count).IsEqualTo(0);
        await Assert.That(rewritten.Arguments)
            .IsEqualTo("/c \"prog.exe\" first second^&evil.exe");
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
