using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AlastairLundy.CliInvoke.Builders;

using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.CliInvoke.Core.Primitives;

using AlastairLundy.CliInvoke.Piping;

using AlastairLundy.CliInvoke.Tests.Helpers;
using AlastairLundy.CliInvoke.Tests.TestData;

using Xunit;

namespace AlastairLundy.CliInvoke.Tests.Invokers;

public class CliCommandInvokerTests : IClassFixture<TestFixture>
{
    private readonly IProcessConfigurationInvoker _processInvoker;
        
    //   private CrossPlatformTestExecutables _crossPlatformTestExecutables;
        
    public CliCommandInvokerTests()
    {
        /*
        IServiceProvider services = fixture.ServiceProvider;
            */
            
        _processInvoker = new ProcessConfigurationInvoker(
            new FilePathResolver(),
            new ProcessPipeHandler());
            
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
        if (OperatingSystem.IsWindows())
        {
            IProcessConfigurationBuilder configurationBuilder = new ProcessConfigurationBuilder(
                    WindowsTestExecutables.WinCalcExePath)
                .WithWorkingDirectory(Environment.SystemDirectory);
                
            ProcessConfiguration commandConfiguration = configurationBuilder.Build();
                
            ProcessResult result = await _processInvoker.ExecuteAsync(commandConfiguration,
                null,
                CancellationToken.None);
                
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