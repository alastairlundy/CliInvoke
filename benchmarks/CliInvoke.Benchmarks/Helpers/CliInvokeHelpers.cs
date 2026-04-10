namespace CliInvoke.Benchmarking.Helpers;

internal static class CliInvokeHelpers
{
    private static readonly ProcessInvoker _processInvoker;

    static CliInvokeHelpers()
    {
        _processInvoker = new ProcessInvoker(FilePathResolver.Shared);
    }

    internal static ProcessInvoker CreateProcessInvoker()
    {
        return _processInvoker;
    }
}