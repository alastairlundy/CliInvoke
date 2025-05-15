
using System.Threading;
using System.Threading.Tasks;
using AlastairLundy.CliInvoke;
using AlastairLundy.CliInvoke.Abstractions;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Builders.Abstractions;
using AlastairLundy.CliInvoke.Extensibility.Abstractions.Invokers;
using AlastairLundy.CliInvoke.Specializations.Invokers;
using AlastairLundy.Extensions.Processes.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CliInvoke.Specializations.Tests.Invokers;

#if NET48_OR_GREATER
using OperatingSystem = Polyfills.OperatingSystemPolyfill;
#endif

public class CmdInvokerTests : IClassFixture<TestFixture>
{
    private readonly ISpecializedCliCommandInvoker _specializedCliCommandInvoker;
    
    public CmdInvokerTests(TestFixture testFixture)
    {
        ICliCommandInvoker cliInvoker = testFixture.ServiceProvider.GetRequiredService<ICliCommandInvoker>();
        
        _specializedCliCommandInvoker = new CmdCliCommandInvoker(cliInvoker);
    }
    
    [Fact]
    public async Task Invoke_Echo_With_CMD_Test()
    {
#if NET48_OR_GREATER || NET5_0_OR_GREATER
        if (OperatingSystem.IsWindows())
#else
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
        {
            ICliCommandConfigurationBuilder configurationBuilder = new CliCommandConfigurationBuilder("echo")
                .WithArguments("test")
                .WithWorkingDirectory(Environment.SystemDirectory)
                .WithValidation(ProcessResultValidation.ExitCodeZero);
            
            CliCommandConfiguration commandConfiguration = configurationBuilder.Build();

            CliCommandConfiguration runnerCommand = _specializedCliCommandInvoker.CreateRunnerCommand(commandConfiguration);

            BufferedProcessResult result = await _specializedCliCommandInvoker.ExecuteBufferedAsync(runnerCommand, CancellationToken.None);
            
            Assert.Equal("test", result.StandardOutput);
        }
        
    }
}