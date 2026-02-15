using CliInvoke.Core;

namespace CliInvoke.Benchmarking.Data;

public class DotnetCommandHelper
{
    public DotnetCommandHelper()
    {
        IFilePathResolver filePathResolver = new FilePathResolver();
        
        DotnetExecutableTargetFilePath = filePathResolver.ResolveFilePath(OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
    }

    public string DotnetExecutableTargetFilePath { get; }

    public string Arguments => "--list-sdks";
}
