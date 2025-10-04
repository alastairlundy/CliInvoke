using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AlastairLundy.CliInvoke.Tests.Internal.Helpers;

public static class WindowsTestExecutables
{
    public static string WinCalcExePath
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return string.Format("{0}{1}calc.exe",
                    Environment.SystemDirectory,
                    Path.DirectorySeparatorChar);
            }
                
            throw new PlatformNotSupportedException();
        }
    }   
}