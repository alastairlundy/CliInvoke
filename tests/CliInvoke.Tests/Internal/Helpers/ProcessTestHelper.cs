using System;
using System.Diagnostics;
using CliInvoke.Tests.Internal.Constants;

namespace CliInvoke.Tests.Internal.Helpers;

public class ProcessTestHelper
{
    public static string GetTargetFilePath()
    {
        string filePath;
        if (OperatingSystem.IsWindows())
        {
            filePath = TargetFilePaths.CmdFilePath;
        }
        else if(OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD() || OperatingSystem.IsAndroid())
        {
            filePath = TargetFilePaths.LinuxEchoFilePath;
        }
        else if (OperatingSystem.IsMacOS())
        {
            filePath = "echo";
        }
        else
        {
            throw new PlatformNotSupportedException();
        }
        
        return filePath;
    }

    public static Process CreateProcess(string targetFilePath, string arguments)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = targetFilePath,
            Arguments = arguments,
            RedirectStandardInput = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        
        Process process = new Process()
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };

        return process;
    }
}