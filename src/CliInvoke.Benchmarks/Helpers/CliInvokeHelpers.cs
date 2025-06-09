using AlastairLundy.CliInvoke;

using AlastairLundy.CliInvoke.Piping;
using AlastairLundy.Resyslib.IO.Core.Files;
using AlastairLundy.Resyslib.IO.Files;

namespace CliInvoke.Benchmarking.Helpers;

internal class CliInvokeHelpers
{
    internal static ProcessFactory CreateProcessFactory()
    {
        return new ProcessFactory(new FilePathResolver(), new ProcessPipeHandler());
    }

    internal static ProcessInvoker CreateProcessInvoker()
    {
        return new ProcessInvoker(CreateProcessFactory());
    }
}