using AlastairLundy.CliInvoke;
using AlastairLundy.CliInvoke.Core.Abstractions;
using AlastairLundy.CliInvoke.Core.Abstractions.Legacy;
using AlastairLundy.CliInvoke.Core.Abstractions.Legacy.Utilities;
using AlastairLundy.CliInvoke.Core.Abstractions.Piping;
using AlastairLundy.CliInvoke.Legacy;
using AlastairLundy.CliInvoke.Legacy.Utilities;
using AlastairLundy.CliInvoke.Piping;
using AlastairLundy.Resyslib.IO.Core.Files;

namespace CliInvoke.Benchmarking.Helpers;

internal class CliInvokeHelpers
{
    internal static ProcessFactory CreateProcessFactory()
    {
        return new ProcessFactory(new FilePathResolver());
    }

    internal static CliCommandInvoker CreateCliCommandInvoker()
    {
        IFilePathResolver filePathResolver = new FilePathResolver();
        IProcessRunnerUtility processRunnerUtility = new ProcessRunnerUtility(filePathResolver);
        IProcessPipeHandler processPipeHandler = new ProcessPipeHandler();
        
        IPipedProcessRunner pipedProcessRunner = new PipedProcessRunner(processRunnerUtility, processPipeHandler);

        ICommandProcessFactory commandProcessFactory = new CommandProcessFactory();
        
        return new CliCommandInvoker(pipedProcessRunner, processPipeHandler, commandProcessFactory);
    }
}