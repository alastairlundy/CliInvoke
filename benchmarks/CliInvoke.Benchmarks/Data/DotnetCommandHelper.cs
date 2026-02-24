using CliInvoke.Benchmarking.Helpers;
using WhatExec.Lib.Abstractions;

namespace CliInvoke.Benchmarking.Data;

public class DotnetCommandHelper
{
    public DotnetCommandHelper()
    {
        IExecutableFileResolver filePathResolver = CliInvokeHelpers.CreateFileResolver();
        
        DotnetExecutableTargetFilePath = filePathResolver.LocateExecutableAsync(OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet",
                SearchOption.AllDirectories, CancellationToken.None)
            .Result.FullName;
    }

    public string DotnetExecutableTargetFilePath { get; }

    public string Arguments => "--list-sdks";
}
