using System.Runtime.InteropServices;
using CliWrap;
using CliWrap.Buffered;

namespace CliInvoke.Benchmarking.Data;

public class DotnetCommandHelper
{
    private readonly string _dotnetFilePath;
    
    public DotnetCommandHelper()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            CommandTask<BufferedCommandResult> task = Cli.Wrap("which")
                .WithArguments("dotnet")
                .ExecuteBufferedAsync();

            task.Task.Start();

            task.Task.Wait();
        
            _dotnetFilePath = task.Task.Result.StandardOutput.Split(Environment.NewLine).First();
        }
        else
        {
            _dotnetFilePath =
                $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}{Path.DirectorySeparatorChar}dotnet{Path.DirectorySeparatorChar}dotnet.exe";
        }
    }
    
    public string DotnetExecutableTargetFilePath => _dotnetFilePath;
}