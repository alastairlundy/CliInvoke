﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Core.Piping;
using AlastairLundy.CliInvoke.Piping;

namespace AlastairLundy.CliInvoke.Specializations.Tests.Helpers;

public static class ExecutedCommandHelper
{
        
    static ExecutedCommandHelper()
    {
        IProcessPipeHandler processPipeHandler = new ProcessPipeHandler();
        IFilePathResolver filePathResolver = new FilePathResolver();
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