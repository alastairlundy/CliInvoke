using System.Runtime.InteropServices;
using AlastairLundy.CliInvoke.Core;
using CliInvoke.Benchmarking.Helpers;

namespace CliInvoke.Benchmarking.Data;

public class DotnetCommandHelper
{
    private readonly string _dotnetFilePath;
    
    public DotnetCommandHelper()
    {
        IProcessInvoker processConfigurationInvoker = CliInvokeHelpers.CreateProcessInvoker();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
        {
            ProcessConfiguration processConfiguration = new ProcessConfiguration("/usr/bin/which",
                false, true, true,
                "dotnet");

           Task<BufferedProcessResult> task = processConfigurationInvoker.ExecuteBufferedAsync(processConfiguration);
           
           task.Wait();
        
            _dotnetFilePath = task.Result.StandardOutput.Split(Environment.NewLine).First();
        }
        else
        {
            _dotnetFilePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}{Path.DirectorySeparatorChar}dotnet{Path.DirectorySeparatorChar}dotnet.exe";
        }
    }
    
    public string DotnetExecutableTargetFilePath => _dotnetFilePath;

    public string Arguments => "--list-sdks";
}