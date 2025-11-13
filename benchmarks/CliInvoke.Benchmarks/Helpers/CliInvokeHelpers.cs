using AlastairLundy.CliInvoke;
using AlastairLundy.CliInvoke.Core.Piping;
using AlastairLundy.CliInvoke.Piping;

namespace CliInvoke.Benchmarking.Helpers;

internal class CliInvokeHelpers
{
    internal static ProcessInvoker CreateProcessInvoker()
    {
        IProcessPipeHandler processPipeHandler = new ProcessPipeHandler();
        return new ProcessInvoker(new FilePathResolver(), processPipeHandler);
    }
}
