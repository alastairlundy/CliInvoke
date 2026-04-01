using System.Threading;
using WhatExec.Lib.Abstractions.Resolvers;
using WhatExec.Lib.Detectors;
using WhatExec.Lib.Resolvers;

namespace CliInvoke.Tests.Internal.Constants;

public static class TargetFilePaths
{
    private static readonly IExecutableFileResolver _filePathResolver = new ExecutableFileResolver(
        new ExecutableFileDetector(),
        new PathEnvironmentVariableResolver(new PathEnvironmentVariableDetector(), new ExecutableFileDetector()));

    public static readonly string CmdFilePath = Environment.SystemDirectory + Path.DirectorySeparatorChar + "cmd.exe";

    public static readonly string LinuxEchoFilePath = "/usr/bin/echo";

    public static string DotnetFilePath
    {
        get
        {
            Task<FileInfo> resultTask = _filePathResolver.LocateExecutableAsync(
                OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet", SearchOption.AllDirectories,
                CancellationToken.None);

            resultTask.Wait();

            return resultTask.Result.FullName;
        }
    }
}