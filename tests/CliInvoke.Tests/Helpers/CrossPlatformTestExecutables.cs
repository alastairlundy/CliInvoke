using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using AlastairLundy.CliInvoke.Abstractions;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Builders.Abstractions;

using AlastairLundy.CliInvoke.Core.Abstractions.Legacy;
using AlastairLundy.CliInvoke.Core.Abstractions.Legacy.Utilities;
using AlastairLundy.CliInvoke.Core.Abstractions.Piping;
using AlastairLundy.CliInvoke.Core.Primitives;
using AlastairLundy.CliInvoke.Core.Primitives.Results;

using AlastairLundy.CliInvoke.Legacy;
using AlastairLundy.CliInvoke.Legacy.Piping;
using AlastairLundy.CliInvoke.Legacy.Utilities;

using AlastairLundy.CliInvoke.Specializations.Configurations;

using AlastairLundy.Extensions.IO.Abstractions.Files;
using AlastairLundy.Extensions.IO.Files;

namespace AlastairLundy.CliInvoke.Tests.Helpers
{
    public class CrossPlatformTestExecutables
    {
        private static ICliCommandInvoker _cliInvoker;

        private static readonly string dotnetExePath;
        private static readonly string cmdExePath;
        
        static CrossPlatformTestExecutables()
        {
                IProcessPipeHandler processPipeHandler = new ProcessPipeHandler();
                IFilePathResolver filePathResolver = new FilePathResolver();
            
                IProcessRunnerUtility processRunnerUtility = new ProcessRunnerUtility(filePathResolver);
            
                IPipedProcessRunner pipedProcessRunner = new PipedProcessRunner(processRunnerUtility,
                    processPipeHandler);
            
                ICommandProcessFactory commandProcessFactory = new CommandProcessFactory();
            
                _cliInvoker = new CliCommandInvoker(pipedProcessRunner,
                    processPipeHandler, commandProcessFactory);

                ICliCommandConfigurationBuilder dotnetConfigurationBuilder;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    cmdExePath = new CmdCommandConfiguration().TargetFilePath;
                    dotnetConfigurationBuilder = new CliCommandConfigurationBuilder(cmdExePath)
                        .WithArguments("dotnet --list-sdks");
                }
                else
                {
                    dotnetConfigurationBuilder = new CliCommandConfigurationBuilder("/usr/bin/which")
                        .WithArguments("dotnet --list-sdks");
                }
                
                CliCommandConfiguration dotnetCommandConfiguration = dotnetConfigurationBuilder.Build();    
            
                Task<BufferedProcessResult> dotnetBufferredOutput = _cliInvoker.ExecuteBufferedAsync(dotnetCommandConfiguration);

                dotnetBufferredOutput.Start();
                
                dotnetBufferredOutput.Wait();

                string[] lines = dotnetBufferredOutput.Result.StandardOutput.Split(Environment.NewLine.First());

                if (lines.Any())
                {
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) == false)
                        {
                           dotnetExePath = line.Split(' ').Last()
                                .Replace("[", string.Empty).Replace("]", string.Empty);
                           break;
                        }
                    }
                }
        }

        public static string DotnetExePath => dotnetExePath;
           
        public static string CrossPlatformPowershellExePath =>
            new PowershellCommandConfiguration(_cliInvoker).TargetFilePath;

    }
}