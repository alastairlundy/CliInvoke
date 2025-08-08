using System;

using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AlastairLundy.CliInvoke.Builders;

using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.CliInvoke.Core.Piping;
using AlastairLundy.CliInvoke.Core.Primitives;

using AlastairLundy.CliInvoke.Piping;
using AlastairLundy.CliInvoke.Specializations.Configurations;

namespace AlastairLundy.CliInvoke.Tests.Helpers
{
    public class CrossPlatformTestExecutables
    {
        private static IProcessInvoker processInvoker;

        private static readonly string dotnetExePath;
        private static readonly string cmdExePath;
        
        static CrossPlatformTestExecutables()
        {
                IProcessPipeHandler processPipeHandler = new ProcessPipeHandler();
                IFilePathResolver filePathResolver = new FilePathResolver();

                processInvoker = new ProcessInvoker(filePathResolver,
                    processPipeHandler);

                IProcessConfigurationBuilder dotnetConfigurationBuilder;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    cmdExePath = new CmdProcessConfiguration().TargetFilePath;
                    dotnetConfigurationBuilder = new ProcessConfigurationBuilder(cmdExePath)
                        .WithArguments("dotnet --list-sdks");
                }
                else
                {
                    dotnetConfigurationBuilder = new ProcessConfigurationBuilder("/usr/bin/which")
                        .WithArguments("dotnet --list-sdks");
                }
                
                ProcessConfiguration dotnetCommandConfiguration = dotnetConfigurationBuilder.Build();    
            
                Task<BufferedProcessResult> dotnetBufferredOutput = processInvoker.ExecuteBufferedAsync(dotnetCommandConfiguration);

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
                                .Replace("[",
                                    string.Empty).Replace("]",
                                    string.Empty);
                           break;
                        }
                    }
                }
        }

        public static string DotnetExePath => dotnetExePath;
           
        public static string CrossPlatformPowershellExePath =>
            new PowershellProcessConfiguration(processInvoker).TargetFilePath;

    }
}