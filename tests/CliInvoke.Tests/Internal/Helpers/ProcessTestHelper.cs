using CliInvoke.Processes.Internal;
using TUnit.Core.Exceptions;

namespace CliInvoke.Tests.Internal.Helpers;

internal class ProcessTestHelper
{
    internal static string GetTargetFilePath()
    {
        string filePath;
        if (OperatingSystem.IsWindows())
            filePath = "cmd";
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD() || OperatingSystem.IsAndroid())
            filePath = "echo";
        else if (OperatingSystem.IsMacOS())
            filePath = "echo";
        else
            throw new PlatformNotSupportedException();

        return filePath;
    }

    internal static ProcessWrapper CreateProcess(string targetFilePath, string arguments)
    {
        ProcessConfiguration configuration = new ProcessConfiguration(targetFilePath,arguments);

        FilePathResolver resolver = new();
        FileInfo resolvedPath = resolver.ResolveFilePath(targetFilePath);
        ProcessWrapper process = new ProcessWrapper(configuration, resolvedPath);

        return process;
    }

    /// <summary>
    ///     Arguments that make <see cref="GetTargetFilePath"/>'s target exit immediately
    ///     instead of waiting at an interactive REPL. A bare cmd on Windows reads stdin
    ///     until end-of-file, so its lifetime depends on whichever stdin the host process
    ///     happens to inherit: tests that need a short-lived child must supply a
    ///     terminating command rather than empty arguments, or they stall (or leak the
    ///     child) whenever the host's stdin stays open.
    /// </summary>
    internal static string GetExitingArguments()
        => OperatingSystem.IsWindows() ? "/c exit 0" : string.Empty;

    /// <summary>
    ///     Resolves <paramref name="fileName"/> against every PATH entry, mirroring
    ///     the resolve convention used by <c>RawModeRedirectionNormalizationTests</c>
    ///     and <c>StdinPipingClaimTests</c>.
    /// </summary>
    /// <param name="fileName">The executable file name to locate.</param>
    /// <returns>The full path to the executable, or <c>null</c> when not found.</returns>
    internal static string? ResolveOnPath(string fileName)
    {
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

    /// <summary>
    ///     Returns the per-OS quiet child that writes nothing to stdout and exits 0.
    ///     Skips the test when the child executable is unavailable on PATH.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     Raw, unredirected spawns inherit the test host's console, so every byte
    ///     the child writes lands in test output; in an interactive terminal an
    ///     output-producing child also disturbs the test runner's live console
    ///     redraws (that is why the <c>dotnet --version</c> banner, spawned raw by
    ///     the CliRun surface tests, appeared in <c>dotnet test</c> output).
    ///     </para>
    ///     <para>
    ///     On Unix this cannot fall back to <see cref="GetTargetFilePath"/> +
    ///     <see cref="GetExitingArguments"/>, because the dual of <c>cmd /c exit 0</c>
    ///     there (<c>echo</c> with empty arguments) prints a blank line — quiet only
    ///     on Windows, by design.
    ///     </para>
    /// </remarks>
    internal static (string TargetFilePath, string Arguments) GetQuietChildCommand()
    {
        if (OperatingSystem.IsWindows())
            return (GetTargetFilePath(), GetExitingArguments());

        string? sh = ResolveOnPath("sh");
        if (sh is null)
            throw new SkipTestException("A POSIX shell (sh) is not available on PATH.");

        return (sh, "-c \"exit 0\"");
    }

    /// <summary>
    ///     Resolves the absolute path to the built signal-trapping helper binary.
    /// </summary>
    internal static string GetSignalTrappingHelperPath()
    {
        // Walk up from the test assembly directory to the repository root
        string? dir = AppContext.BaseDirectory;

        while (dir != null &&
               !File.Exists(Path.Combine(dir, "src", "CliInvoke.sln")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        if (dir == null)
        {
            throw new FileNotFoundException(
                "Could not locate repository root from test assembly path.");
        }

        string binaryName = OperatingSystem.IsWindows()
            ? "CliInvoke.Tests.Helpers.exe"
            : "CliInvoke.Tests.Helpers";

        string config = AppContext.BaseDirectory.Contains("Release", StringComparison.OrdinalIgnoreCase)
            ? "Release"
            : "Debug";

        string binaryPath = Path.Combine(
            dir, "tests", "CliInvoke.Tests.Helpers", "bin",
            config, "net10.0", binaryName);

        if (!File.Exists(binaryPath))
        {
            throw new FileNotFoundException(
                $"Signal-trapping helper binary not found at: {binaryPath}. " +
                "Ensure the CliInvoke.Tests.Helpers project has been built.");
        }

        return binaryPath;
    }

    /// <summary>
    ///     Delivers <see cref="PosixSignal.SIGTERM"/> to the process with the given id.
    /// </summary>
    /// <remarks>
    ///     <see cref="System.Diagnostics.Process.Kill()"/> terminates a Unix process with
    ///     <c>SIGKILL</c> (exit code 137), not <c>SIGTERM</c> (exit code 143), so it cannot be
    ///     used to exercise the signal-trapping helper. This sends the expected signal directly.
    /// </remarks>
    internal static void SendTerminationSignal(int processId)
    {
        if (OperatingSystem.IsWindows())
            return;

        const int Sigterm = 15;
        _ = kill(processId, Sigterm);
    }

    [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
    private static extern int kill(int pid, int sig);

    /// <summary>
    ///     Creates a <see cref="ProcessWrapper"/> configured to launch the signal-trapping helper
    ///     binary with the specified marker file path and natural sleep duration.
    /// </summary>
    internal static ProcessWrapper CreateSignalTrappingProcess(string markerPath, int sleepSeconds)
    {
        string helperPath = GetSignalTrappingHelperPath();
        return CreateProcess(helperPath, $"\"{markerPath}\" {sleepSeconds}");
    }

    /// <summary>
    ///     Polls a condition until it is true or the timeout expires, replacing fixed
    ///     <c>Task.Delay</c> sleeps with a reliable wait mechanism.
    /// </summary>
    internal static async Task WaitForConditionAsync(
        Func<bool> condition,
        TimeSpan timeout,
        int pollIntervalMs = 50,
        string? failureMessage = null)
    {
        Stopwatch sw = Stopwatch.StartNew();
        while (!condition())
        {
            if (sw.Elapsed >= timeout)
            {
                Assert.Fail(failureMessage ?? $"Condition not met within {timeout.TotalSeconds}s");
                return;
            }

            await Task.Delay(pollIntervalMs).ConfigureAwait(true);
        }
    }
}