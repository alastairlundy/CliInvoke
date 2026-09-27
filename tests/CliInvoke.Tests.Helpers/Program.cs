using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

// Echo-args mode for escaping tests: print each subsequent argument to stdout,
// newline-delimited, then exit. This lets a test verify that the OS actually parsed
// the produced command line back into the original values.
if (args.Length >= 1 && args[0] == "echo-args")
{
    Stream stdout = Console.OpenStandardOutput();
    for (int i = 1; i < args.Length; i++)
    {
        byte[] argBytes = Encoding.UTF8.GetBytes(args[i]);
        stdout.Write(argBytes, 0, argBytes.Length);
        stdout.WriteByte((byte)'\n');
    }
    stdout.Flush();
    return 0;
}

// Echo-env mode for environment-variable delivery tests: print the value of the named
// environment variable to stdout, then exit. Exit code 1 signals that the variable was
// absent, letting a test distinguish "variable missing" from "variable delivered empty".
if (args.Length == 2 && args[0] == "echo-env")
{
    string? value = Environment.GetEnvironmentVariable(args[1]);
    Stream stdout = Console.OpenStandardOutput();
    byte[] valueBytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
    stdout.Write(valueBytes, 0, valueBytes.Length);
    stdout.Flush();
    return value is null ? 1 : 0;
}

string markerPath = args.Length >= 1 ? args[0] : throw new ArgumentException(
    "Expected at least 2 arguments: <markerFilePath> <sleepSeconds>");

if (!int.TryParse(args.Length >= 2 ? args[1] : null, NumberStyles.None,
        CultureInfo.InvariantCulture, out int sleepSeconds))
{
    throw new ArgumentException(
        $"Second argument must be an integer (sleep duration in seconds), got: '{args.ElementAtOrDefault(1)}'.");
}

string? parentDir = Path.GetDirectoryName(markerPath);
if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
{
    throw new DirectoryNotFoundException(
        $"Marker file parent directory does not exist: {parentDir}");
}

Action writeMarker = () =>
    File.WriteAllText(markerPath, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));

Console.CancelKeyPress += (sender, e) =>
{
    writeMarker();
    e.Cancel = false;
};

// On Unix, ProcessWrapper.SendInterruptSignalAsync sends SIGTERM first, not SIGINT.
// Register a POSIX handler so the marker is written regardless of signal type.
if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
{
    PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
    {
        writeMarker();
        context.Cancel = false;
    });
}

Thread.Sleep(TimeSpan.FromSeconds(sleepSeconds));

return 0;
