using System;
using System.IO;
using CliInvoke.Processes.Internal;
using CliInvoke.Tests.Internal.Constants;

namespace CliInvoke.Tests.Internal.Helpers;

internal class ProcessTestHelper
{
    internal static string GetTargetFilePath()
    {
        string filePath;
        if (OperatingSystem.IsWindows())
            filePath = TargetFilePaths.CmdFilePath;
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD() || OperatingSystem.IsAndroid())
            filePath = TargetFilePaths.LinuxEchoFilePath;
        else if (OperatingSystem.IsMacOS())
            filePath = "echo";
        else
            throw new PlatformNotSupportedException();

        return filePath;
    }

    internal static ProcessWrapper CreateProcess(string targetFilePath, string arguments)
    {
        ProcessConfiguration configuration = new ProcessConfiguration(targetFilePath,arguments);

        ProcessWrapper process = new ProcessWrapper(configuration, configuration.ResourcePolicy);

        return process;
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

        string binaryPath = Path.Combine(
            dir, "tests", "CliInvoke.Tests.Helpers", "bin",
            "Debug", "net10.0", binaryName);

        if (!File.Exists(binaryPath))
        {
            throw new FileNotFoundException(
                $"Signal-trapping helper binary not found at: {binaryPath}. " +
                "Ensure the CliInvoke.Tests.Helpers project has been built.");
        }

        return binaryPath;
    }

    /// <summary>
    ///     Creates a <see cref="ProcessWrapper"/> configured to launch the signal-trapping helper
    ///     binary with the specified marker file path and natural sleep duration.
    /// </summary>
    internal static ProcessWrapper CreateSignalTrappingProcess(string markerPath, int sleepSeconds)
    {
        string helperPath = GetSignalTrappingHelperPath();
        return CreateProcess(helperPath, $"\"{markerPath}\" {sleepSeconds}");
    }
}