using System.IO;
using System.Runtime.InteropServices;
using CliInvoke.Core.Piping;
using CliInvoke.Piping;

namespace CliInvoke.Specializations.Tests.Helpers;

public static class ExecutedCommandHelper
{
    static ExecutedCommandHelper()
    {
        IProcessPipeHandler processPipeHandler = new ProcessPipeHandler();
    }
        
    public static string WinCalcExePath
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return $"{Environment.SystemDirectory}{Path.DirectorySeparatorChar}calc.exe";
            }
                
            throw new PlatformNotSupportedException();
        }
    }

}