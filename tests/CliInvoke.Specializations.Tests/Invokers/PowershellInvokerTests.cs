using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using CliInvoke.Builders;
using CliInvoke.Core.Extensibility.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace CliInvoke.Specializations.Tests.Invokers;

[ClassDataSource<TestFixture>(Shared = SharedType.PerClass)]
public class PowershellInvokerTests : IDisposable
{
    private readonly PowershellProcessInvoker _powershellProcessInvoker;
    
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    public PowershellInvokerTests(TestFixture testFixture)
    {
        IProcessInvoker procInvoker = testFixture.ServiceProvider.GetRequiredService<IProcessInvoker>();
        IRunnerProcessFactory runnerProcessFactory = testFixture.ServiceProvider.GetRequiredService<IRunnerProcessFactory>();
        IFilePathResolver filePathResolver = testFixture.ServiceProvider.GetRequiredService<IFilePathResolver>();
        
        _powershellProcessInvoker = new PowershellProcessInvoker(procInvoker, runnerProcessFactory, filePathResolver);
    }
    
    [Test]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    public async Task Invoke_Open_With_Powershell_Test()
    {
        if (!OperatingSystem.IsWindows())
            return;

        string executable = OperatingSystem.IsWindows()
            ? ExecutedCommandHelper.WinCalcExePath
            : ExecutedCommandHelper.WhichUnixPath;

        string workingDirectory = OperatingSystem.IsWindows()
            ? ExecutedCommandHelper.WinCalcExePath.Replace("calc.exe", string.Empty)
            : Environment.CurrentDirectory;

        IProcessConfigurationBuilder configurationBuilder = new ProcessConfigurationBuilder(executable)
            .SetWorkingDirectory(workingDirectory)
            .ConfigureWindowCreation(true);

        ProcessConfiguration commandConfiguration = configurationBuilder.Build();

        ProcessResult result = await _powershellProcessInvoker.ExecuteAsync(commandConfiguration,
            new ProcessExitConfiguration(ProcessTimeoutPolicy.FromTimeSpan(TimeSpan.FromMinutes(1)),
                ProcessResultValidation.None, ProcessCancellationExceptionBehavior.SuppressException),
            false, CancellationToken.None);

        await Assert.That(Process.GetProcesses().Any(p => p.ProcessName.Contains("calculatorapp", 
                StringComparison.InvariantCultureIgnoreCase)))
            .IsTrue();

        await Assert.That(result.ExitCode)
            .IsEqualTo(0);
    }

    public void Dispose()
    {
        _powershellProcessInvoker.Dispose();
        GC.SuppressFinalize(this);
    }
}