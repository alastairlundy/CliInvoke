using AlastairLundy.CliInvoke;
using AlastairLundy.CliInvoke.Core.Piping.Abstractions;
using AlastairLundy.CliInvoke.Piping;
using AlastairLundy.Resyslib.IO.Core.Files;
using AlastairLundy.Resyslib.IO.Files;

namespace CliInvoke.Benchmarking.Helpers;

internal class CliInvokeHelpers
{
    internal static ProcessFactory CreateProcessFactory()
    {
        IProcessPipeHandler processPipeHandler = new ProcessPipeHandler();
        return new ProcessFactory(new FilePathResolver(), processPipeHandler);
    }

    internal static ProcessInvoker CreateProcessInvoker()
    {
        IProcessPipeHandler processPipeHandler = new ProcessPipeHandler();
        return new ProcessInvoker(new FilePathResolver(), processPipeHandler);
    }
}