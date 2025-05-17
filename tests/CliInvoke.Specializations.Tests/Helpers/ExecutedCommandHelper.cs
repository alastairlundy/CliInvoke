using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CliInvoke.Specializations.Tests.Helpers
{
    public static class ExecutedCommandHelper
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
}