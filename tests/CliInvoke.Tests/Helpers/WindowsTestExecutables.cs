using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AlastairLundy.CliInvoke.Tests.Helpers;

public class WindowsTestExecutables
{
    public static string WinCalcExePath
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Environment.SystemDirectory + Path.DirectorySeparatorChar
                                                   + "System32" + Path.DirectorySeparatorChar
                                                   + "calc.exe";
            }
                
            throw new PlatformNotSupportedException();
        }
    }   
}