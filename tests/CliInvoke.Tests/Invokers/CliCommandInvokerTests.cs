using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AlastairLundy.CliInvoke.Abstractions;

using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Builders.Abstractions;
using AlastairLundy.CliInvoke.Core.Primitives;
using AlastairLundy.CliInvoke.Core.Primitives.Results;
using AlastairLundy.CliInvoke.Legacy;
using AlastairLundy.CliInvoke.Legacy.Piping;
using AlastairLundy.CliInvoke.Legacy.Utilities;
using AlastairLundy.CliInvoke.Tests.Helpers;
using AlastairLundy.CliInvoke.Tests.TestData;

using AlastairLundy.Resyslib.IO.Files;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

#if NET48_OR_GREATER
using OperatingSystem = Polyfills.OperatingSystemPolyfill;
#elif NET472
using System.Runtime.InteropServices;
#endif

namespace AlastairLundy.CliInvoke.Tests.Invokers
{
    public class CliCommandInvokerTests : IClassFixture<TestFixture>
    {
        private readonly ICliCommandInvoker _cliCommandInvoker;
        
     //   private CrossPlatformTestExecutables _crossPlatformTestExecutables;
        
        public CliCommandInvokerTests()
        {
            /*
            IServiceProvider services = fixture.ServiceProvider;
            
            ICliCommandInvoker cliInvoker = services
                .GetRequiredService<ICliCommandInvoker>();
                */
            
            _cliCommandInvoker = new CliCommandInvoker(
                new PipedProcessRunner(new ProcessRunnerUtility(new FilePathResolver()), new ProcessPipeHandler()),
                new ProcessPipeHandler(), new CommandProcessFactory());
            
          //  _crossPlatformTestExecutables = new CrossPlatformTestExecutables(_cliCommandInvoker);
        }

        /*[Fact]
        public async Task Invoke_Shell_Echo()
        {
            ICliCommandConfigurationBuilder configurationBuilder = new
                    CliCommandConfigurationBuilder(CrossPlatformTestExecutables.CrossPlatformShellPath)
                .WithArguments("echo test")
                .WithValidation(ProcessResultValidation.ExitCodeZero);
            
            CliCommandConfiguration cliCommandConfiguration = configurationBuilder.Build();
            
            BufferedProcessResult bufferedProcess = await _cliCommandInvoker.ExecuteBufferedAsync(cliCommandConfiguration);
            
            Assert.Equal(0, bufferedProcess.ExitCode);
            Assert.Equal("test", bufferedProcess.StandardOutput);
        }*/
        
        [Fact]
        public async Task Invoke_Open_Windows_Calc_Test()
        {
#if NET48_OR_GREATER || NET5_0_OR_GREATER
            if (OperatingSystem.IsWindows())
#else
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
            {
                ICliCommandConfigurationBuilder configurationBuilder = new CliCommandConfigurationBuilder(
                        WindowsTestExecutables.WinCalcExePath)
                    .WithWorkingDirectory(Environment.SystemDirectory)
                    .WithValidation(ProcessResultValidation.ExitCodeZero);
            
                CliCommandConfiguration commandConfiguration = configurationBuilder.Build();
                
                ProcessResult result = await _cliCommandInvoker.ExecuteAsync(commandConfiguration, CancellationToken.None);
                
                Assert.True(Process.GetProcessesByName("Calculator").Any() &&
                            result.WasSuccessful);
            }
        }

        /*[Fact]
        public async Task Invoke_Dotnet_List_Sdk()
        {
            ICliCommandConfigurationBuilder configurationBuilder = new
                    CliCommandConfigurationBuilder(_crossPlatformTestExecutables.DotnetExePath)
                .WithArguments("--list-sdks")
                .WithValidation(ProcessResultValidation.ExitCodeZero);
            
            CliCommandConfiguration cliCommandConfiguration = configurationBuilder.Build();
            
            ProcessResult processResult = await _cliCommandInvoker.ExecuteAsync(cliCommandConfiguration);
            
            Assert.Equal(0, processResult.ExitCode);
        }*/
    }
}