using AlastairLundy.CliInvoke;
using AlastairLundy.CliInvoke.Core.Piping;

using AlastairLundy.CliInvoke.Piping;

namespace CliInvoke.Benchmarking.Helpers;

internal class CliInvokeHelpers
{
    internal static ProcessFactory CreateProcessFactory()
    {
        IProcessPipeHandler processPipeHandler = new ProcessPipeHandler();
        return new ProcessFactory(new FilePathResolver(),
            processPipeHandler);
    }

    internal static ProcessConfigurationInvoker CreateProcessInvoker()
    {
        IProcessPipeHandler processPipeHandler = new ProcessPipeHandler();
        return new ProcessConfigurationInvoker(new FilePathResolver(),
            processPipeHandler);
    }
}