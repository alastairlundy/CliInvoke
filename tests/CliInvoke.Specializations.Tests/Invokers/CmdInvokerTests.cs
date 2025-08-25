
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core.Builders;

using AlastairLundy.CliInvoke.Core.Primitives;

using CliInvoke.Specializations.Tests.Helpers;

namespace CliInvoke.Specializations.Tests.Invokers;

public class CmdInvokerTests : IClassFixture<TestFixture>
{
    
    public CmdInvokerTests(TestFixture testFixture)
    {
        //        CliCommandInvoker cliInvoker = new CliCommandInvoker(new PipedProcessRunner(new ProcessRunnerUtility(new FilePathResolver()),
        //           new ProcessPipeHandler()), new ProcessPipeHandler(), new CommandProcessFactory());
            
        //  ICliCommandInvoker cliInvoker = testFixture.ServiceProvider
        //    .GetRequiredService<ICliCommandInvoker>();
        
        //        _specializedCliCommandInvoker = new CmdCliCommandInvoker(cliInvoker);
    }
    
    [Fact]
    public async Task Invoke_Calc_Open_With_CMD_Test()
    {
        if (OperatingSystem.IsWindows())
        {
            IProcessConfigurationBuilder configurationBuilder = new ProcessConfigurationBuilder
                    (ExecutedCommandHelper.WinCalcExePath)
                .WithWorkingDirectory(ExecutedCommandHelper.WinCalcExePath.Replace("calc.exe",
                    string.Empty));
            
            ProcessConfiguration commandConfiguration = configurationBuilder.Build();

            //        ProcessConfiguration runnerCommand = CreateRunnerProcess(commandConfiguration);

            //      ProcessResult result = await _specializedCliCommandInvoker.ExecuteAsync(runnerCommand, CancellationToken.None);
                
            //       Assert.True(Process.GetProcessesByName("Calculator").Any() &&
            //                     result.WasSuccessful);
        }
    }
}