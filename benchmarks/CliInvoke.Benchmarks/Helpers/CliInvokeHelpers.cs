using CliInvoke.Core.Factories;
using CliInvoke.Factories;

namespace CliInvoke.Benchmarking.Helpers;

internal static class CliInvokeHelpers
{
    private static readonly ProcessInvoker _processInvoker;
    
    private static readonly IExternalProcessFactory _externalProcessFactory;

    static CliInvokeHelpers()
    {
        _externalProcessFactory = new ExternalProcessFactory();
        _processInvoker = new ProcessInvoker(_externalProcessFactory);
    }

    internal static ProcessInvoker CreateProcessInvoker()
    {
        return _processInvoker;
    }
    
    internal static IExternalProcessFactory CreateExternalProcessFactory()
    {
        return _externalProcessFactory;
    }
}