

namespace CliInvoke.Tests.Internal.Constants;

public static class TargetFilePaths
{
    private static readonly IFilePathResolver _filePathResolver = new FilePathResolver();

    public static readonly string CmdFilePath = Environment.SystemDirectory + Path.DirectorySeparatorChar + "cmd.exe";

    public static string DotnetFilePath =>
        _filePathResolver.ResolveFilePath(
            OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet").FullName;
}