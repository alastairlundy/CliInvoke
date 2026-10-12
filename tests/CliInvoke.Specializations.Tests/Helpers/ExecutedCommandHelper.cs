using System.IO;
using System.Runtime.InteropServices;

namespace CliInvoke.Specializations.Tests.Helpers;

internal static class ExecutedCommandHelper
{
    internal static string WinCalcExePath
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return $"{Environment.SystemDirectory}{Path.DirectorySeparatorChar}calc.exe";

            throw new PlatformNotSupportedException();
        }
    }
}