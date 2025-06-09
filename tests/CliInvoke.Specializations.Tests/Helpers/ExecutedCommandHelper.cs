

using System;
using System.IO;
using System.Runtime.InteropServices;

using AlastairLundy.CliInvoke;
using AlastairLundy.CliInvoke.Abstractions;

using AlastairLundy.CliInvoke.Core.Abstractions.Legacy;
using AlastairLundy.CliInvoke.Core.Abstractions.Legacy.Utilities;
using AlastairLundy.CliInvoke.Core.Abstractions.Piping;
using AlastairLundy.CliInvoke.Core;

using AlastairLundy.CliInvoke.Legacy;
using AlastairLundy.CliInvoke.Legacy.Utilities;

using AlastairLundy.CliInvoke.Specializations.Configurations;

using AlastairLundy.Resyslib.IO.Core.Files;
using AlastairLundy.Resyslib.IO.Files;


namespace CliInvoke.Specializations.Tests.Helpers
{
    public static class ExecutedCommandHelper
    {
        private static ICliCommandInvoker _cliInvoker;
        
        static ExecutedCommandHelper()
        {
            IProcessPipeHandler processPipeHandler = new ProcessPipeHandler();
            IFilePathResolver filePathResolver = new FilePathResolver();
            
            IProcessRunnerUtility processRunnerUtility = new ProcessRunnerUtility(filePathResolver);
            
            IPipedProcessRunner pipedProcessRunner = new PipedProcessRunner(processRunnerUtility,
                processPipeHandler);
            
            ICommandProcessFactory commandProcessFactory = new CommandProcessFactory();
            
            _cliInvoker = new CliCommandInvoker(pipedProcessRunner,
                processPipeHandler, commandProcessFactory);
        }
        
        public static string WinCalcExePath
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return $"{Environment.SystemDirectory}{Path.DirectorySeparatorChar}calc.exe";
                }
                
                throw new PlatformNotSupportedException();
            }
        }

        public static string CrossPlatformPowershellExePath => new PowershellCommandConfiguration(_cliInvoker).TargetFilePath;
    }
}