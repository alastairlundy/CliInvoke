namespace CliInvoke.Benchmarking.Data;

public class DotnetCommandHelper
{
    public DotnetCommandHelper()
    {
        FileInfo fileResult = new FilePathResolver().ResolveFilePath(
            OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
        
        DotnetExecutableTargetFilePath = fileResult.FullName;
    }

    public string DotnetExecutableTargetFilePath { get; }

    public string Arguments => "--list-sdks";
}