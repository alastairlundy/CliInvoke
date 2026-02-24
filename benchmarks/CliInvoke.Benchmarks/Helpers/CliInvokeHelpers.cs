using CliInvoke.Core.Piping;
using CliInvoke.Piping;
using WhatExec.Lib;
using WhatExec.Lib.Abstractions;
using WhatExec.Lib.Detectors;

namespace CliInvoke.Benchmarking.Helpers;

internal static class CliInvokeHelpers
{
    internal static ProcessInvoker CreateProcessInvoker()
    {
        IProcessPipeHandler processPipeHandler = new ProcessPipeHandler();
        return new ProcessInvoker(CreateFileResolver(), processPipeHandler);
    }

    internal static IExecutableFileResolver CreateFileResolver()
    {
        var executableFileDetector = new ExecutableFileDetector();

        return new ExecutableFileResolver(executableFileDetector,
            new PathEnvironmentVariableResolver(new PathEnvironmentVariableDetector(), executableFileDetector));
    }
}
