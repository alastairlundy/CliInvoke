using CliInvoke.Core.Piping;
using CliInvoke.Piping;

namespace CliInvoke.Benchmarking.Helpers;

internal static class CliInvokeHelpers
{
    internal static ProcessInvoker CreateProcessInvoker()
    {
        IProcessPipeHandler processPipeHandler = new ProcessPipeHandler();
        return new ProcessInvoker(new FilePathResolver(), processPipeHandler);
    }
}
