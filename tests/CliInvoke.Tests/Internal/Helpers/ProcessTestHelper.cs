using CliInvoke.Helpers;
using CliInvoke.Tests.Internal.Constants;

namespace CliInvoke.Tests.Internal.Helpers;

internal class ProcessTestHelper
{
    internal static string GetTargetFilePath()
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

    internal static ProcessWrapper CreateProcess(string targetFilePath, string arguments)
    {
        ProcessConfiguration configuration = new ProcessConfiguration(targetFilePath, false, true,
            true, arguments, windowCreation: false);

        ProcessWrapper process = new ProcessWrapper(configuration, configuration.ResourcePolicy);

        return process;
    }
}