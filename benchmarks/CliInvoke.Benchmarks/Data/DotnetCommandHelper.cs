using System.Runtime.InteropServices;
using AlastairLundy.WhatExecLib;
using AlastairLundy.WhatExecLib.Detectors;
using CliInvoke.Benchmarking.Helpers;
using CliInvoke.Core;

namespace CliInvoke.Benchmarking.Data;

public class DotnetCommandHelper
{
    private readonly string _dotnetFilePath;

    public DotnetCommandHelper()
    {
        PathExecutableResolver filePathResolver = new(new ExecutableFileDetector());

        _dotnetFilePath = filePathResolver.ResolvePathEnvironmentExecutableFile("dotnet").FullName;
    }

    public string DotnetExecutableTargetFilePath => _dotnetFilePath;

    public string Arguments => "--list-sdks";
}
