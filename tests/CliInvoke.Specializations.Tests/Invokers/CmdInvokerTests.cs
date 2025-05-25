
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AlastairLundy.CliInvoke;
using AlastairLundy.CliInvoke.Abstractions;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Builders.Abstractions;
using AlastairLundy.CliInvoke.Core.Primitives.Results;
using AlastairLundy.CliInvoke.Extensibility.Abstractions.Invokers;

using AlastairLundy.CliInvoke.Specializations.Invokers;

using CliInvoke.Specializations.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace CliInvoke.Specializations.Tests.Invokers
{
#if NET48_OR_GREATER
using OperatingSystem = Polyfills.OperatingSystemPolyfill;
#endif

    public class CmdInvokerTests : IClassFixture<TestFixture>
    {
        private readonly ISpecializedCliCommandInvoker _specializedCliCommandInvoker;
    
        public CmdInvokerTests(TestFixture testFixture)
        {
            ICliCommandInvoker cliInvoker = testFixture.ServiceProvider
                .GetRequiredService<ICliCommandInvoker>();
        
            _specializedCliCommandInvoker = new CmdCliCommandInvoker(cliInvoker);
        }
    
        /*public async Task Invoke_Calc_Open_With_CMD_Test()
        {
#if NET48_OR_GREATER || NET5_0_OR_GREATER
            if (OperatingSystem.IsWindows())
#else
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
            {
                ICliCommandConfigurationBuilder configurationBuilder = new CliCommandConfigurationBuilder
                        (ExecutedCommandHelper.WinCalcExePath)
                    .WithValidation(ProcessResultValidation.ExitCodeZero);
            
                CliCommandConfiguration commandConfiguration = configurationBuilder.Build();

                CliCommandConfiguration runnerCommand = _specializedCliCommandInvoker.CreateRunnerCommand(commandConfiguration);

                ProcessResult result = await _specializedCliCommandInvoker.ExecuteAsync(runnerCommand, CancellationToken.None);
                
                Assert.True(Process.GetProcessesByName("Calculator").Any() &&
                            result.WasSuccessful);
            }
        }*/
    }
}