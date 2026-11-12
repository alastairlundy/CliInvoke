using System.Diagnostics;
using System.Threading.Tasks;
using CliInvoke.Extensibility.Factories;
using CliInvoke.Extensions;
using CliInvoke.Specializations.Configurations;

namespace CliInvoke.Specializations.Tests.Configurations;

/// <summary>
///     Launches a real cmd.exe through the public configuration surface and checks
///     the observable outcome: what the process writes to stdout and which exit code
///     it returns. Covers a benign tokenised runner composition, a caller-owned raw
///     command line, and the API boundary that rejects a pre-tokenised token cmd.exe
///     parses itself.
/// </summary>
public class CmdProcessRuntimeTests
{
    private const string Marker = "cli-invoke-runtime-marker";

    private static readonly TimeSpan ExitTimeout = TimeSpan.FromSeconds(30);

    [Test, WindowsOnly]
    public async Task CreateRunnerConfiguration_BenignCmdComposition_RunsEchoAndExitsZero()
    {
        ProcessConfiguration runner = new("cmd.exe", "/c");
        ProcessConfiguration target = new("cmd.exe", $"/c echo {Marker}");

        ProcessConfiguration composed =
            new RunnerProcessFactory().CreateRunnerConfiguration(target, runner);

        ProcessStartInfo processStartInfo = composed.ToProcessStartInfo();
        processStartInfo.UseShellExecute = false;

        ProcessRun run = await RunToCompletionAsync(processStartInfo);

        await Assert.That(run.Exited).IsTrue();
        await Assert.That(run.ExitCode).IsEqualTo(0);
        await Assert.That(run.StandardOutput).Contains(Marker);
    }

    [Test, WindowsOnly]
    public async Task ToProcessStartInfo_RawCmdLine_RunsEchoAndExitsZero()
    {
        // The raw /c line is caller-owned and delivered verbatim: the tokenised
        // argument path of this configuration is never populated.
        CmdProcessConfiguration configuration = new($"echo {Marker}",
            redirectStandardInput: false,
            redirectStandardOutput: true,
            redirectStandardError: true);

        ProcessStartInfo processStartInfo = configuration.ToProcessStartInfo();
        processStartInfo.UseShellExecute = false;

        ProcessRun run = await RunToCompletionAsync(processStartInfo);

        await Assert.That(run.Exited).IsTrue();
        await Assert.That(run.ExitCode).IsEqualTo(0);
        await Assert.That(run.StandardOutput).Contains(Marker);
    }

    [Test, WindowsOnly]
    public async Task CreateRunnerConfiguration_TokenWithCmdControlCharacter_ThrowsBeforeLaunch()
    {
        ProcessConfiguration runner = new("cmd.exe", "/c");
        ProcessConfiguration target = new("cmd.exe", "/c echo alpha & echo beta");

        RunnerProcessFactory factory = new();

        ArgumentException? caught = null;

        try
        {
            factory.CreateRunnerConfiguration(target, runner);
        }
        catch (ArgumentException exception)
        {
            caught = exception;
        }

        // The composition is refused while it is being built, so no process is
        // ever started from the rejected configuration.
        await Assert.That(caught).IsNotNull();
        await Assert.That(caught!.ParamName).IsEqualTo(nameof(runner.Arguments));
        await Assert.That(caught.Message).Contains("&");
    }

    [Test, WindowsOnly]
    public async Task ToProcessStartInfo_TokenWithCmdControlCharacter_ThrowsBeforeLaunch()
    {
        ProcessConfiguration configuration = new("cmd.exe", "/c echo alpha & echo beta");

        ArgumentException? caught = null;

        try
        {
            configuration.ToProcessStartInfo();
        }
        catch (ArgumentException exception)
        {
            caught = exception;
        }

        // Conversion fails before any process could be started from the result.
        await Assert.That(caught).IsNotNull();
        await Assert.That(caught!.ParamName).IsEqualTo(nameof(ProcessConfiguration.ArgumentsList));
        await Assert.That(caught.Message).Contains("&");
    }

    /// <summary>
    ///     Starts the process, captures stdout/stderr, and waits for exit with a
    ///     bounded timeout; a process that outlives the timeout is killed so the
    ///     test fails instead of hanging.
    /// </summary>
    private static async Task<ProcessRun> RunToCompletionAsync(ProcessStartInfo processStartInfo)
    {
        processStartInfo.UseShellExecute = false;

        using Process process = new() { StartInfo = processStartInfo };

        await Assert.That(process.Start()).IsTrue();

        Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();

        bool exited = process.WaitForExit(ExitTimeout);

        if (!exited)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception exception)
                when (exception is InvalidOperationException or NotSupportedException)
            {
                // Best-effort cleanup; the exit assertion below still fails the test.
            }
        }

        string standardOutput = await standardOutputTask.WaitAsync(ExitTimeout);
        string standardError = await standardErrorTask.WaitAsync(ExitTimeout);

        return new ProcessRun(exited, exited ? process.ExitCode : -1, standardOutput, standardError);
    }

    private sealed record ProcessRun(bool Exited, int ExitCode, string StandardOutput, string StandardError);
}
