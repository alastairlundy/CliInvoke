using WhatExec.Lib.Abstractions.Resolvers;
using WhatExec.Lib.Detectors;
using WhatExec.Lib.Resolvers;

namespace CliInvoke.Benchmarking.Helpers;

internal static class CliInvokeHelpers
{
    private static readonly ProcessInvoker _processInvoker;
    private static readonly IExecutableFileResolver executableFileResolver;

    static CliInvokeHelpers()
    {
        executableFileResolver = new ExecutableFileResolver(new ExecutableFileDetector(),
            new PathEnvironmentVariableResolver(
                new PathEnvironmentVariableDetector(), new ExecutableFileDetector()));

        _processInvoker = new ProcessInvoker(executableFileResolver);
    }

    internal static ProcessInvoker CreateProcessInvoker()
    {
        return _processInvoker;
    }

    internal static IExecutableFileResolver CreateExecutableFileResolver()
    {
        return executableFileResolver;
    }
}