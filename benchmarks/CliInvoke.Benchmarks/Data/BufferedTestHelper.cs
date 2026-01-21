using CliInvoke.Core;
using CliInvoke.Core.Builders;
using CliInvoke.Factories;
using CliInvoke.Piping;
using DotExtensions.IO.Directories;

namespace CliInvoke.Benchmarking.Data;

public class BufferedTestHelper
{
    public BufferedTestHelper()
    {
        TargetFilePath = ""; 
        
        TargetFilePath = GetMockDataSimExePath();
    }

    private string GetMockDataSimExePath()
    {
        IProcessInvoker processInvoker = new ProcessInvoker(new FilePathResolver(), new ProcessPipeHandler());

        DirectoryInfo directoryInfo = new DirectoryInfo(Environment.CurrentDirectory)
            .Root;

        FileInfo? mockDataProjectFile = directoryInfo.SafelyEnumerateFiles("CliInvoke.Benchmarking.MockDataSimulationTool.csproj",
                SearchOption.AllDirectories)
            .FirstOrDefault();
        
        ProcessConfigurationFactory processConfigurationFactory = new();

        if (mockDataProjectFile is not null)
        {
            using ProcessConfiguration buildConfig =
                new ProcessConfiguration(OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet",
                    false, true,
                    true,
                    "build -c Release", mockDataProjectFile.Directory?.FullName);

            Task<BufferedProcessResult> resultTask = processInvoker.ExecuteBufferedAsync(buildConfig, 
                ProcessExitConfiguration.DefaultNoException,
                false, CancellationToken.None);

            resultTask.Wait();
        }
        
        string whichOrWhere = OperatingSystem.IsWindows() ? "where.exe" : "which";
        
        string mockDataExe = OperatingSystem.IsWindows() ? "CliInvokeMockDataSimTool.exe" : "CliInvokeMockDataSimTool";
        
        using ProcessConfiguration configuration = processConfigurationFactory.Create(whichOrWhere,
            mockDataExe);
        
        Task<BufferedProcessResult> task = processInvoker.ExecuteBufferedAsync(configuration, ProcessExitConfiguration.DefaultNoException,
            false, CancellationToken.None);
        
        task.Wait();
        
        if (task.Result.ExitCode != 0)
        {
            throw new ArgumentException("CliInvoke Mock Data Simulation Tool could not be found.");
        }

        return task.Result.StandardOutput.Split(Environment.NewLine).First();
    }

    public string TargetFilePath
    {
        get;
        private set => field = value;
    }

    public string Arguments => "gen-fake-text";
}
