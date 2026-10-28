namespace CliInvoke.Tests.Internal.Helpers;

public static class WindowsTestExecutables
{
    public static string WinCalcExePath
    {
        get
        {
            if (OperatingSystem.IsWindows())
                return string.Format("{0}{1}calc.exe",
                    Environment.SystemDirectory,
                    Path.DirectorySeparatorChar);

            throw new PlatformNotSupportedException();
        }
    }
}