using System.Threading.Tasks;

using AlastairLundy.CliInvoke.Abstractions;

using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Builders.Abstractions;

using AlastairLundy.CliInvoke.Core.Primitives.Results;

using AlastairLundy.CliInvoke.Tests.Helpers;
using AlastairLundy.CliInvoke.Tests.TestData;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace AlastairLundy.CliInvoke.Tests.Invokers
{
    public class CliCommandInvokerTests : IClassFixture<TestFixture>
    {
        private readonly ICliCommandInvoker _cliCommandInvoker;
        
        public CliCommandInvokerTests(TestFixture fixture)
        {
            ICliCommandInvoker cliInvoker = fixture.ServiceProvider
                .GetRequiredService<ICliCommandInvoker>();
            
            _cliCommandInvoker = cliInvoker;
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
        public async Task Invoke_Dotnet_List_Sdk()
        {
            ICliCommandConfigurationBuilder configurationBuilder = new
                    CliCommandConfigurationBuilder(CrossPlatformTestExecutables.DotnetExePath)
                .WithArguments("--list-sdks")
                .WithValidation(ProcessResultValidation.ExitCodeZero);
            
            CliCommandConfiguration cliCommandConfiguration = configurationBuilder.Build();
            
            BufferedProcessResult bufferedProcess = await _cliCommandInvoker.ExecuteBufferedAsync(cliCommandConfiguration);
            
            Assert.Equal(0, bufferedProcess.ExitCode);
        }
    }
}