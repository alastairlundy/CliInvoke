using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

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
