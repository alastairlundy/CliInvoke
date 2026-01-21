using System.Runtime.InteropServices;
using CliInvoke.Benchmarking.Helpers;
using CliInvoke.Core;

namespace CliInvoke.Benchmarking.Data;

public class DotnetCommandHelper
{
    private readonly string _dotnetFilePath;

    public DotnetCommandHelper()
    {
        IFilePathResolver filePathResolver = new FilePathResolver();
        
        _dotnetFilePath = filePathResolver.ResolveFilePath(OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
    }

    public string DotnetExecutableTargetFilePath => _dotnetFilePath;

    public string Arguments => "--list-sdks";
}
