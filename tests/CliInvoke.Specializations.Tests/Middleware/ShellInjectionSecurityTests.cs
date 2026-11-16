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
using TUnit.Core.Exceptions;

namespace CliInvoke.Specializations.Tests.Middleware;

/// <summary>
///     Regression tests for shell command composition. The PowerShell middleware must
///     use the shell-aware <c>ShellRewriter</c> output and preserve caller-supplied
///     argument lists; the cmd middleware additionally rejects wrapped values
///     containing characters cmd.exe parses itself, the same as the runner factory's
///     cmd branch.
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
        // Each argument-list entry is quoted as its own token in the composed script
        // (per-entry QuotePathForPowerShell), so a semicolon stays inside the entry's
        // quoted region rather than being re-tokenised.
        await Assert.That(rewritten.ArgumentList[3])
            .IsEqualTo("& \"prog.exe\" \"first\" \"second;evil.exe\"");
    }

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task Cmd_Wrapper_RejectsMetacharactersInTargetPath()
    {
        if (!OperatingSystem.IsWindows())
            throw new SkipTestException(
                "cmd is Windows-only; CmdMiddleware rejects non-Windows by design.");

        // cmd.exe parses the whole /c tail the middleware composes, so a target path
        // containing characters cmd.exe interprets itself is rejected before anything
        // is composed or launched, the same as the runner factory's cmd branch.
        ProcessConfiguration original = MakeConfig("prog\" & evil.exe & \"", "a b c");

        CapturingNext next = new();
        CmdMiddleware middleware = new();

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await middleware.InvokeAsync(
                new InvocationContext(original, ProcessExitConfiguration.CreateGraceful(),
                    InvocationMode.Buffered),
                next.Invoke));

        await Assert.That(exception.ParamName).IsEqualTo("TargetFilePath");
        await Assert.That(exception.Message).Contains("prog\" & evil.exe & \"");
        await Assert.That(next.Captured).IsNull();
    }

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task Cmd_Wrapper_RejectsCommandSeparatorInArguments()
    {
        if (!OperatingSystem.IsWindows())
            throw new SkipTestException(
                "cmd is Windows-only; CmdMiddleware rejects non-Windows by design.");

        // The legacy Arguments string flows into the composed /c tail as one whole
        // span, so a separator character anywhere in it is rejected as well.
        ProcessConfiguration original = MakeConfig("prog.exe", "a & b | c");

        CapturingNext next = new();
        CmdMiddleware middleware = new();

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await middleware.InvokeAsync(
                new InvocationContext(original, ProcessExitConfiguration.CreateGraceful(),
                    InvocationMode.Buffered),
                next.Invoke));

        await Assert.That(exception.ParamName).IsEqualTo("Arguments");
        await Assert.That(exception.Message).Contains("a & b | c");
        await Assert.That(exception.Message).Contains("&");
        await Assert.That(next.Captured).IsNull();
    }

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task Cmd_Wrapper_PreservesArgumentListAndIgnoresArguments()
    {
        ProcessConfiguration original = new("prog.exe", "ignored")
        {
            ArgumentList = ["first", "second app", "third arg"]
        };

        CapturingNext next = new();
        CmdMiddleware middleware = new();

        await middleware.InvokeAsync(
            new InvocationContext(original, ProcessExitConfiguration.CreateGraceful(),
                InvocationMode.Buffered),
            next.Invoke);

        ProcessConfiguration rewritten = next.Captured!.Configuration;

        await Assert.That(rewritten.ArgumentList.Count).IsEqualTo(0);

        // The outer quoting pair is mandatory: cmd.exe strips one leading and one trailing
        // quote before reparsing the remainder. Quoting each entry inside it is what keeps
        // argument boundaries intact, so 'second app' stays one argument for the child
        // instead of collapsing into two.
        await Assert.That(rewritten.Arguments)
            .IsEqualTo("/c \"\"prog.exe\" \"first\" \"second app\" \"third arg\"\"");
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
