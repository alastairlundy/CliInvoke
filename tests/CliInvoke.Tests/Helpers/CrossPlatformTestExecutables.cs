using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AlastairLundy.CliInvoke.Core.Piping.Abstractions;
using AlastairLundy.CliInvoke.Piping;
using AlastairLundy.CliInvoke.Specializations.Configurations;

using AlastairLundy.Resyslib.IO.Core.Files;
using AlastairLundy.Resyslib.IO.Files;

namespace AlastairLundy.CliInvoke.Tests.Helpers
{
    public class CrossPlatformTestExecutables
    {
   //     private static ICliCommandInvoker _cliInvoker;

        private static readonly string dotnetExePath;
        private static readonly string cmdExePath;
        
        static CrossPlatformTestExecutables()
        {
                IProcessPipeHandler processPipeHandler = new ProcessPipeHandler();
                IFilePathResolver filePathResolver = new FilePathResolver();

          //      _cliInvoker = new CliCommandInvoker(pipedProcessRunner,
          //             processPipeHandler, commandProcessFactory);

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