using System.IO;

namespace CliInvoke.Specializations.Tests.Helpers;

internal static class ExecutedCommandHelper
{
    internal static string WinCalcExePath
    {
        get
        {
            if (OperatingSystem.IsWindows())
                return $"{Environment.SystemDirectory}{Path.DirectorySeparatorChar}calc.exe";

            throw new PlatformNotSupportedException();
        }
    }
}