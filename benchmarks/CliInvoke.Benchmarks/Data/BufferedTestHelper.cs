using AlastairLundy.WhatExecLib;
using AlastairLundy.WhatExecLib.Detectors;
using AlastairLundy.WhatExecLib.Locators;
using CliInvoke.Core;
using CliInvoke.Factories;
using CliInvoke.Piping;

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
        
        ProcessConfigurationFactory processConfigurationFactory = new();

        string whichOrWhere = OperatingSystem.IsWindows() ? "where.exe" : "which";
        
        string mockDataExe = OperatingSystem.IsWindows() ? "CliInvokeMockDataSimTool.exe" : "CliInvokeMockDataSimTool";
        
        using ProcessConfiguration configuration = processConfigurationFactory.Create(whichOrWhere,
            mockDataExe);
        
        BufferedProcessResult task = processInvoker.ExecuteBufferedAsync(configuration).Result;

        return task.StandardOutput.Split(Environment.NewLine).First();
    }

    public string TargetFilePath
    {
        get;
        private set => field = value;
    }

    public string Arguments => "gen-fake-text";
}
