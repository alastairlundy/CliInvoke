using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using CliInvoke.Builders;
using CliInvoke.Core.Extensibility;
using CliInvoke.Core.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace CliInvoke.Specializations.Tests.Invokers;

[ClassDataSource<TestFixture>(Shared = SharedType.PerClass)]
public class CmdInvokerTests
{
    private readonly CmdProcessInvoker cmdProcessInvoker;

    [SupportedOSPlatform("windows")]
    public CmdInvokerTests(TestFixture testFixture)
    {
        IRunnerConfigurationFactory runnerProcessFactory = testFixture.ServiceProvider.GetRequiredService<IRunnerConfigurationFactory>();
        
        IExternalProcessFactory externalProcessFactory = testFixture.ServiceProvider.GetRequiredService<IExternalProcessFactory>();
        
        cmdProcessInvoker = new CmdProcessInvoker(runnerProcessFactory, externalProcessFactory);
    }

    [Test]
    public async Task Invoke_Calc_Open_With_CMD_Test()
    {
        if (!OperatingSystem.IsWindows())
            return;

        IProcessConfigurationBuilder configurationBuilder = new ProcessConfigurationBuilder
                (ExecutedCommandHelper.WinCalcExePath)
            .SetWorkingDirectory(ExecutedCommandHelper.WinCalcExePath.Replace("calc.exe", string.Empty))
            .EnableWindowCreation(true);

        ProcessConfiguration commandConfiguration = configurationBuilder.Build();

        ProcessResult result = await cmdProcessInvoker.ExecuteAsync(commandConfiguration,
            new ProcessExitConfiguration(ProcessTimeoutPolicy.FromTimeSpan(TimeSpan.FromMinutes(1)),
                cancellationThrowsException: false), CancellationToken.None);

        await Assert.That(Process.GetProcesses().Any(p => p.ProcessName.Contains("calculatorapp",
                StringComparison.InvariantCultureIgnoreCase)))
            .IsTrue();

        await Assert.That(result.ExitCode)
            .IsEqualTo(0);
    }
}