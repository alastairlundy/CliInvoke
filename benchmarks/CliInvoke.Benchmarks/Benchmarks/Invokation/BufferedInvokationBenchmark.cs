using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.WhatExecLib.Detectors;
using AlastairLundy.WhatExecLib.Locators;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CliInvoke.Benchmarking.Data;
using CliInvoke.Benchmarking.Helpers;
using CliWrap;
using CliWrap.Buffered;
using Cysharp.Diagnostics;

namespace CliInvoke.Benchmarking.Benchmarks.Invokation;

[MemoryDiagnoser(true), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class BufferedInvokationBenchmark
{
    private readonly IProcessInvoker _processInvoker;
    
    private string _mockDataSimFilePath;

    private const string MockDataSimArguments = "gen-fake-data";
    
    public BufferedInvokationBenchmark()
    {
        _processInvoker = CliInvokeHelpers.CreateProcessInvoker();
    }

    [GlobalSetup]
    public void Setup()
    {
        CommandTask<CommandResult> buildBenchmarkMockDataSim = Cli
            .Wrap(new DotnetCommandHelper().DotnetExecutableTargetFilePath)
            .WithWorkingDirectory("../CliInvoke.Benchmarking.MockDataSimulationTool/")
            .WithArguments("build -c Release")
            .ExecuteAsync();

        buildBenchmarkMockDataSim.Task.Wait();

        _mockDataSimFilePath = "../CliInvoke.Benchmarking.MockDataSimulationTool/bin/Release/CliInvokeMockDataSimTool";

       var fileLocator = new ExecutableFileInstancesLocator(new ExecutableFileDetector());

        if (OperatingSystem.IsWindows())
        {
            _mockDataSimFilePath += ".exe";
        }
    }

    [Benchmark]
    public async Task<(string standardOutput, string standardError)> CliInvoke_ProcessInvoker()
    {
        IProcessConfigurationBuilder processConfigurationBuilder = new ProcessConfigurationBuilder(
               _mockDataSimFilePath
            )
            .SetArguments(MockDataSimArguments)
            .RedirectStandardOutput(true)
            .RedirectStandardError(true);
        
        ProcessConfiguration configuration = processConfigurationBuilder.Build();
        
        BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(configuration, disposeOfConfig:true);
      
        return (result.StandardOutput, result.StandardError);
    }
    
    [Benchmark]
    public async Task<(string standardOutput, string standardError)> CliWrap()
    {
        BufferedCommandResult result = await Cli.Wrap(_mockDataSimFilePath)
            .WithArguments("gen-fake-data")
            .WithValidation(global::CliWrap.CommandResultValidation.ZeroExitCode)
            .ExecuteBufferedAsync();
      
        return (result.StandardOutput, result.StandardError);
    }
    
    [Benchmark]
    public async Task<(string standardOutput, string standardError)> MedallionShell()
    {
        Medallion.Shell.CommandResult result = await Medallion.Shell.Command.Run(_mockDataSimFilePath, MockDataSimArguments).Task;
        
        return (result.StandardOutput, result.StandardError);
    }
    
    [Benchmark]
    public async Task<(string standardOut, string standardError)> ProcessX()
    {
        (Process process,
            ProcessAsyncEnumerable stdOut,
            ProcessAsyncEnumerable stdError) result =
            Cysharp.Diagnostics.ProcessX.GetDualAsyncEnumerable($"{_mockDataSimFilePath} {MockDataSimArguments}");

        Task<string[]> standardOutTask = result.stdOut.ToTask();
        Task<string[]> standardErrorTask = result.stdError.ToTask();

        await Task.WhenAll([standardOutTask, standardErrorTask]);
        
        string standardOut = string.Join(Environment.NewLine, standardOutTask.Result);
        string standardError = string.Join(Environment.NewLine, standardErrorTask.Result);
        
        return (standardOut, standardError);
    }
}
