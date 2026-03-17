using CliInvoke.Benchmarking.Helpers;
using CliInvoke.Core;
using WhatExec.Lib.Abstractions.Resolvers;
using WhatExec.Lib.Detectors;
using WhatExec.Lib.Resolvers;

namespace CliInvoke.Benchmarking.Data;

public class DotnetCommandHelper
{
    public DotnetCommandHelper()
    {
        IExecutableFileResolver executableFileResolver = CliInvokeHelpers.CreateExecutableFileResolver();
        
        Task<FileInfo> resultTask = executableFileResolver.LocateExecutableAsync(OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet", SearchOption.AllDirectories,
            CancellationToken.None);

        resultTask.Wait();

        DotnetExecutableTargetFilePath = resultTask.Result.FullName;
    }

    public string DotnetExecutableTargetFilePath { get; }

    public string Arguments => "--list-sdks";
}
