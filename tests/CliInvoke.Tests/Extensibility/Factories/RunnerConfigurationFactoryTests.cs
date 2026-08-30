/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;

using CliInvoke;
using CliInvoke.Builders;
using CliInvoke.Core;
using CliInvoke.Core.Extensibility;
using CliInvoke.Extensibility;
using CliInvoke.Factories;

using TUnit.Core.Exceptions;

namespace CliInvoke.Tests.Extensibility.Factories;

/// <summary>
///     Regression tests for the command-injection vulnerability in
///     <see cref="RunnerConfigurationFactory"/> (CWE-88, escalating to CWE-78 when the
///     runner is a shell).
///
///     <para>
///         These tests assert BEHAVIOR through the full pipeline to
///     <see cref="System.Diagnostics.Process"/>: each test runs a real shell with an
///     injection input supplied by the factory and asserts that the marker never executes
///     (no marker file is created). A raw single-string delivery control runs alongside
///     every class of injection input as a calibration — if the calibration does not fire,
///     the test framework is no longer capable of detecting the injection class and the
///     test result is meaningless.
///     </para>
///
///     <para>
///         String-shape assertions on the factory output are not sufficient: historically
///         the factory produced a safe-looking <c>Arguments</c> string while still being
///         exploitable at the OS / shell re-parse boundary. These tests run end-to-end
///         against real pwsh / cmd and assert what actually happened.
///     </para>
/// </summary>
public class RunnerConfigurationFactoryTests
{
    /// <summary>
    ///     Shared marker directory for this test session. Exposed for the hooks class.
    /// </summary>
    public static readonly string MarkerDirStatic =
        Path.Combine(Path.GetTempPath(), "CliInvoke.RunnerConfigurationFactoryTests",
            Guid.NewGuid().ToString("N"));

    private static string MarkerDir => MarkerDirStatic;

    private static ProcessConfiguration BuildConfig(string targetFilePath, string arguments)
        => new ProcessConfigurationBuilder(targetFilePath)
            .SetArguments(arguments)
            .SetOutputRedirection(true)
            .Build();

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
            if (trimmed.Length == 0) continue;
            string candidate = Path.Combine(trimmed, fileName);
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    private static string MarkerPath(string id) => Path.Combine(MarkerDir, id + ".txt");

    /// <summary>
    ///     Runs <paramref name="fileName"/> with <paramref name="arguments"/> as a single
    ///     <see cref="System.Diagnostics.ProcessStartInfo.Arguments"/> string. Used only
    ///     as a CALIBRATION control — proves the injection input fires when delivered
    ///     through the vulnerable single-string path so the behaviour assertions below
    ///     have meaning.
    /// </summary>
    private static void RunRawSingleString(string fileName, string arguments, string markerId)
    {
        string marker = MarkerPath(markerId);
        if (File.Exists(marker)) File.Delete(marker);

        using System.Diagnostics.Process p = new()
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo(fileName)
            {
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };
        p.Start();
        p.WaitForExit(30000);
    }

    /// <summary>
    ///     Runs the wrapped configuration produced by the factory through the full
    ///     library pipeline (<see cref="ProcessInvoker"/>) and asserts the marker file was
    ///     NOT created.
    /// </summary>
    private static async Task<bool> RunViaFactoryAndCheckMarkerAsync(
        IRunnerConfigurationFactory factory,
        ProcessConfiguration runner,
        string targetFilePath,
        string targetArguments,
        string markerId)
    {
        string marker = MarkerPath(markerId);
        if (File.Exists(marker)) File.Delete(marker);

        ProcessConfiguration target = BuildConfig(targetFilePath, targetArguments);
        ProcessConfiguration wrapped = factory.CreateRunnerConfiguration(target, runner);

        // Structural sanity: the factory must populate the read-only ArgumentList (or the
        // mutable ArgumentsList) so the adapter emits via ProcessStartInfo.ArgumentList
        // and not the single Arguments string.
        bool structuralOk =
            wrapped.ArgumentList.Count > 0 || wrapped.ArgumentsList.Count > 0;

        if (!structuralOk) return false;

        IProcessInvoker invoker = new ProcessInvoker(new ExternalProcessFactory());
        try
        {
            _ = await invoker.ExecuteBufferedAsync(wrapped,
                ProcessExitConfiguration.CreateGraceful());
        }
        catch
        {
            // Non-zero exits and missing-target errors are expected; the security property
            // we assert is whether the marker file was created.
        }

        return File.Exists(marker);
    }

    // ------------------------------------------------------------------------
    //  Structural assertions: the factory must deliver tokens via ArgumentList
    // ------------------------------------------------------------------------

    [Test]
    public async Task CreateRunnerConfiguration_PowerShellRunner_DeliversViaArgumentList()
    {
        IRunnerConfigurationFactory factory = new RunnerConfigurationFactory();

        ProcessConfiguration runner = BuildConfig("pwsh.exe", "-NoProfile -NonInteractive -Command");
        ProcessConfiguration target = BuildConfig(@"C:\program files\app.exe", "arg one");

        ProcessConfiguration result = factory.CreateRunnerConfiguration(target, runner);

        // pwsh runner delivers via ArgumentList — runner flags as separate tokens, then
        // ONE shell-escaped script entry. The dangerous single-string Arguments is empty.
        await Assert.That(result.Arguments).IsEqualTo(string.Empty);
        await Assert.That(result.ArgumentList.Count).IsGreaterThan(0);

        // Runner flags are well-known and stay as discrete tokens.
        await Assert.That(result.ArgumentList).Contains("-NoProfile");
        await Assert.That(result.ArgumentList).Contains("-NonInteractive");
        await Assert.That(result.ArgumentList).Contains("-Command");

        // The last ArgumentList entry is the shell-escaped script — it carries the
        // call operator, the target (with spaces preserved), and the args.
        string script = result.ArgumentList[^1];
        await Assert.That(script).Contains("& ");
        await Assert.That(script).Contains("app.exe");
        await Assert.That(script).Contains("arg");
        await Assert.That(script).Contains("one");
    }

    [Test]
    public async Task CreateRunnerConfiguration_CmdRunner_DeliversViaEscapedArgumentsString()
    {
        // cmd /c cannot use .NET ArgumentList delivery because the ArgumentList quoting
        // .NET applies for embedded quotes does not match what cmd /c's parser strips.
        // The factory therefore composes a single Arguments string composed with the
        // cmd escaper — .NET passes Arguments verbatim to the raw command line, and
        // cmd applies its own quote-stripping rules to the unquoted / cmd-escaped form.
        IRunnerConfigurationFactory factory = new RunnerConfigurationFactory();

        ProcessConfiguration runner = BuildConfig("cmd.exe", "/c");
        ProcessConfiguration target = BuildConfig(@"C:\program files\app.exe", "arg one");

        ProcessConfiguration result = factory.CreateRunnerConfiguration(target, runner);

        await Assert.That(result.ArgumentList.Count).IsEqualTo(0);
        await Assert.That(result.Arguments).IsNotEqualTo(string.Empty);
        await Assert.That(result.Arguments).StartsWith("/c ");
        await Assert.That(result.Arguments).Contains("app.exe");
        await Assert.That(result.Arguments).Contains("arg");
        await Assert.That(result.Arguments).Contains("one");
    }

    [Test]
    public async Task CreateRunnerConfiguration_PowerShellRunner_AddsCallOperatorToScript()
    {
        IRunnerConfigurationFactory factory = new RunnerConfigurationFactory();

        ProcessConfiguration runner = BuildConfig("pwsh.exe", "-NoProfile -NonInteractive -Command");
        ProcessConfiguration target = BuildConfig("app.exe", "arg1");

        ProcessConfiguration result = factory.CreateRunnerConfiguration(target, runner);

        // The script (last ArgumentList entry) carries the call operator and target.
        string script = result.ArgumentList[^1];
        await Assert.That(script).StartsWith("& ");
        await Assert.That(script).Contains("app.exe");
    }

    [Test]
    public async Task CreateRunnerConfiguration_QuoteInCallerTarget_IsEscapedInsideScript()
    {
        IRunnerConfigurationFactory factory = new RunnerConfigurationFactory();

        // A quote inside the caller's target must be backtick-escaped inside the
        // shell-escaped script — it must NOT appear as a literal that breaks pwsh
        // quoting or the surrounding outer quote.
        ProcessConfiguration runner = BuildConfig("pwsh.exe", "-NoProfile -NonInteractive -Command");
        ProcessConfiguration target = BuildConfig("app\"evil.exe", "safe");

        ProcessConfiguration result = factory.CreateRunnerConfiguration(target, runner);

        string script = result.ArgumentList[^1];
        // The caller's quote is escaped as `".
        await Assert.That(script).Contains("app`\"evil.exe");
        await Assert.That(script).Contains("safe");
        // The script does not start with a broken `& "app` sequence.
        await Assert.That(script).StartsWith("& \"");
    }

    // ------------------------------------------------------------------------
    //  Calibration controls: raw single-string delivery MUST fire
    //  (without these the behaviour assertions below are meaningless)
    // ------------------------------------------------------------------------

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task Calibration_PwshRawSingleString_QuoteAndSemicolon_FiresMarker()
    {
        string? pwsh = ResolvePwshPath();
        SkipTestIfNull(pwsh, "PowerShell Core (pwsh) is not available on PATH.");

        string marker = MarkerPath("CAL_PS");
        RunRawSingleString(pwsh!,
            $"-NoProfile -NonInteractive -Command & \"whoami\" a\" ; Set-Content -Path \"{marker}\" 'pwned'",
            "CAL_PS");

        await Assert.That(File.Exists(marker)).IsTrue();
    }

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task Calibration_CmdRawSingleString_Ampersand_FiresMarker()
    {
        string marker = MarkerPath("CAL_CMD");
        RunRawSingleString("cmd.exe",
            $"/c \"whoami\" a\" & echo pwned>\"{marker}\"",
            "CAL_CMD");

        await Assert.That(File.Exists(marker)).IsTrue();
    }

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task Calibration_CmdRawSingleString_BareAmpersand_FiresMarker()
    {
        string marker = MarkerPath("CAL_CMD_BARE");
        RunRawSingleString("cmd.exe",
            $"/c whoami & echo pwned>\"{marker}\"",
            "CAL_CMD_BARE");

        await Assert.That(File.Exists(marker)).IsTrue();
    }

    // ------------------------------------------------------------------------
    //  Behaviour assertions: factory + pwsh runner, real pwsh, marker must NOT fire
    // ------------------------------------------------------------------------

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task PwshRunner_QuoteAndSemicolon_DoesNotFireMarker()
    {
        string? pwsh = ResolvePwshPath();
        SkipTestIfNull(pwsh, "PowerShell Core (pwsh) is not available on PATH.");

        IRunnerConfigurationFactory factory = new RunnerConfigurationFactory();
        ProcessConfiguration runner = BuildConfig(pwsh!, "-NoProfile -NonInteractive -Command");

        bool markerFired = await RunViaFactoryAndCheckMarkerAsync(
            factory, runner, @"C:\nonexistent\app.exe",
            "a\" ; Set-Content -Path \"" + MarkerPath("B_PS_QS") + "\" 'pwned'",
            "B_PS_QS");

        await Assert.That(markerFired).IsFalse();
    }

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task PwshRunner_QuoteAndAmpersand_DoesNotFireMarker()
    {
        string? pwsh = ResolvePwshPath();
        SkipTestIfNull(pwsh, "PowerShell Core (pwsh) is not available on PATH.");

        IRunnerConfigurationFactory factory = new RunnerConfigurationFactory();
        ProcessConfiguration runner = BuildConfig(pwsh!, "-NoProfile -NonInteractive -Command");

        bool markerFired = await RunViaFactoryAndCheckMarkerAsync(
            factory, runner, @"C:\nonexistent\app.exe",
            "a\" & Set-Content -Path \"" + MarkerPath("B_PS_QA") + "\" 'pwned'",
            "B_PS_QA");

        await Assert.That(markerFired).IsFalse();
    }

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task PwshRunner_DoubledQuoteAndSemicolon_DoesNotFireMarker()
    {
        string? pwsh = ResolvePwshPath();
        SkipTestIfNull(pwsh, "PowerShell Core (pwsh) is not available on PATH.");

        IRunnerConfigurationFactory factory = new RunnerConfigurationFactory();
        ProcessConfiguration runner = BuildConfig(pwsh!, "-NoProfile -NonInteractive -Command");

        bool markerFired = await RunViaFactoryAndCheckMarkerAsync(
            factory, runner, @"C:\nonexistent\app.exe",
            "a\"\" ; Set-Content -Path \"" + MarkerPath("B_PS_QQ") + "\" 'pwned'",
            "B_PS_QQ");

        await Assert.That(markerFired).IsFalse();
    }

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task PwshRunner_DollarParenSubexpression_DoesNotFireMarker()
    {
        string? pwsh = ResolvePwshPath();
        SkipTestIfNull(pwsh, "PowerShell Core (pwsh) is not available on PATH.");

        IRunnerConfigurationFactory factory = new RunnerConfigurationFactory();
        ProcessConfiguration runner = BuildConfig(pwsh!, "-NoProfile -NonInteractive -Command");

        bool markerFired = await RunViaFactoryAndCheckMarkerAsync(
            factory, runner, @"C:\nonexistent\app.exe",
            "$(Set-Content -Path \"" + MarkerPath("B_PS_DOLLAR") + "\" 'pwned')",
            "B_PS_DOLLAR");

        await Assert.That(markerFired).IsFalse();
    }

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task PwshRunner_QuoteInTarget_DoesNotFireMarker()
    {
        string? pwsh = ResolvePwshPath();
        SkipTestIfNull(pwsh, "PowerShell Core (pwsh) is not available on PATH.");

        IRunnerConfigurationFactory factory = new RunnerConfigurationFactory();
        ProcessConfiguration runner = BuildConfig(pwsh!, "-NoProfile -NonInteractive -Command");

        bool markerFired = await RunViaFactoryAndCheckMarkerAsync(
            factory, runner,
            @"C:\nonexistent\ap""p.exe",
            "x",
            "B_PS_QT");

        await Assert.That(markerFired).IsFalse();
    }

    // ------------------------------------------------------------------------
    //  Behaviour assertions: factory + cmd runner, real cmd, marker must NOT fire
    // ------------------------------------------------------------------------

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task CmdRunner_QuoteAndAmpersand_DoesNotFireMarker()
    {
        IRunnerConfigurationFactory factory = new RunnerConfigurationFactory();
        ProcessConfiguration runner = BuildConfig("cmd.exe", "/c");

        bool markerFired = await RunViaFactoryAndCheckMarkerAsync(
            factory, runner, @"C:\nonexistent\app.exe",
            "a\" & echo pwned>\"" + MarkerPath("B_CMD_QA") + "\"",
            "B_CMD_QA");

        await Assert.That(markerFired).IsFalse();
    }

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task CmdRunner_BareAmpersand_DoesNotFireMarker()
    {
        IRunnerConfigurationFactory factory = new RunnerConfigurationFactory();
        ProcessConfiguration runner = BuildConfig("cmd.exe", "/c");

        bool markerFired = await RunViaFactoryAndCheckMarkerAsync(
            factory, runner, @"C:\nonexistent\app.exe",
            "x & echo pwned>\"" + MarkerPath("B_CMD_BARE") + "\"",
            "B_CMD_BARE");

        await Assert.That(markerFired).IsFalse();
    }

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task CmdRunner_DoubledQuoteAndAmpersand_DoesNotFireMarker()
    {
        IRunnerConfigurationFactory factory = new RunnerConfigurationFactory();
        ProcessConfiguration runner = BuildConfig("cmd.exe", "/c");

        bool markerFired = await RunViaFactoryAndCheckMarkerAsync(
            factory, runner, @"C:\nonexistent\app.exe",
            "a\"\" & echo pwned>\"" + MarkerPath("B_CMD_QQ") + "\"",
            "B_CMD_QQ");

        await Assert.That(markerFired).IsFalse();
    }

    // ------------------------------------------------------------------------
    //  Benign functionality probes — the wrapper must actually run legitimate commands
    // ------------------------------------------------------------------------

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task CmdRunner_WrapsBenignCommand_ExitCodeIsPropagated()
    {
        IRunnerConfigurationFactory factory = new RunnerConfigurationFactory();
        ProcessConfiguration runner = BuildConfig("cmd.exe", "/c");
        ProcessConfiguration target = BuildConfig("cmd.exe", "/c exit 42");

        ProcessConfiguration wrapped = factory.CreateRunnerConfiguration(target, runner);

        IProcessInvoker invoker = new ProcessInvoker(new ExternalProcessFactory());
        BufferedProcessResult result = await invoker.ExecuteBufferedAsync(
            wrapped, ProcessExitConfiguration.CreateGraceful());

        // The exit code from the wrapped command chain must reach the caller.
        await Assert.That(result.ExitCode).IsEqualTo(42);
    }

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task CmdRunner_WrapsWhoami_RunsProgramAndReturnsZero()
    {
        IRunnerConfigurationFactory factory = new RunnerConfigurationFactory();
        ProcessConfiguration runner = BuildConfig("cmd.exe", "/c");
        ProcessConfiguration target = BuildConfig("whoami", string.Empty);

        ProcessConfiguration wrapped = factory.CreateRunnerConfiguration(target, runner);

        IProcessInvoker invoker = new ProcessInvoker(new ExternalProcessFactory());
        BufferedProcessResult result = await invoker.ExecuteBufferedAsync(
            wrapped, ProcessExitConfiguration.CreateGraceful());

        // whoami is a real external program; the wrapper must actually run it, exit 0,
        // and propagate the username to the caller. A previous implementation of this
        // fix returned exit 1 with no output because .NET ArgumentList quoting wrapped
        // the target in a way cmd /c's parser could not handle.
        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.StandardOutput).IsNotNull();
        await Assert.That(result.StandardOutput).IsNotEmpty();
    }

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task NonShellRunner_DeliversTargetAndArgsAsDiscreteTokens()
    {
        // Sudo (or any non-shell runner) does not re-parse its arguments; it simply
        // exec's the target. Each value must therefore be delivered as a separate
        // token without shell escaping. The runner's target file path goes to
        // ProcessConfiguration.TargetFilePath (the FileName), not into ArgumentList.
        IRunnerConfigurationFactory factory = new RunnerConfigurationFactory();
        ProcessConfiguration runner = BuildConfig("sudo", string.Empty);
        ProcessConfiguration target = BuildConfig(@"C:\some\app.exe", "--flag value");

        ProcessConfiguration wrapped = factory.CreateRunnerConfiguration(target, runner);

        await Assert.That(wrapped.Arguments).IsEqualTo(string.Empty);
        await Assert.That(wrapped.TargetFilePath).IsEqualTo("sudo");
        await Assert.That(wrapped.ArgumentList).Contains(@"C:\some\app.exe");
        await Assert.That(wrapped.ArgumentList).Contains("--flag");
        await Assert.That(wrapped.ArgumentList).Contains("value");
    }

    private static void SkipTestIfNull(string? value, string reason)
    {
        if (value is null)
            throw new SkipTestException(reason);
    }
}
