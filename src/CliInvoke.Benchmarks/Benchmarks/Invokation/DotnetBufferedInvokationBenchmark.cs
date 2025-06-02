using System.Diagnostics;

using AlastairLundy.CliInvoke.Abstractions;

using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Builders.Abstractions;

using AlastairLundy.CliInvoke.Core.Abstractions;
using AlastairLundy.CliInvoke.Core.Primitives;
using AlastairLundy.CliInvoke.Core.Primitives.Results;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

using CliInvoke.Benchmarking.Data;
using CliInvoke.Benchmarking.Helpers;

using CliWrap;
using CliWrap.Buffered;
using Cysharp.Diagnostics;
using CliCommandConfiguration = AlastairLundy.CliInvoke.CliCommandConfiguration;

namespace CliInvoke.Benchmarking.Benchmarks.Invokation;

[MemoryDiagnoser(true),
 Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class DotnetBufferedInvokationBenchmark
{
    private readonly IProcessFactory _processFactory;
    private readonly ICliCommandInvoker _cliCommandInvoker;
    
    private BufferedTestHelper _bufferedTestHelper;

    public DotnetBufferedInvokationBenchmark()
    {
        _bufferedTestHelper = new BufferedTestHelper();
        _processFactory = CliInvokeHelpers.CreateProcessFactory();
        _cliCommandInvoker = CliInvokeHelpers.CreateCliCommandInvoker();
    }
    
    [Benchmark]
    public async Task<(string standardOutput, string standardError)> CliInvoke_ProcessFactory()
    {
        ProcessConfiguration processConfiguration =
#pragma warning disable CA1416
            new ProcessConfiguration(_bufferedTestHelper.TargetFilePath, _bufferedTestHelper.Arguments, 
                commandResultValidation: ProcessResultValidation.ExitCodeZero);
#pragma warning restore CA1416

#pragma warning disable CA1416
        ProcessStartInfo startInfo = processConfiguration.ToProcessStartInfo();
#pragma warning restore CA1416
        
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        Process process = _processFactory.StartNew(startInfo);
       
        BufferedProcessResult result = await _processFactory.ContinueWhenExitBufferedAsync(process);
      
        return (result.StandardOutput, result.StandardError);
    }
    
    [Benchmark(Baseline = true)]
    public async Task<(string standardOutput, string standardError)> CliInvoke_CliCommandInvoker()
    {
        ICliCommandConfigurationBuilder commandConfigurationBuilder = new
                CliCommandConfigurationBuilder(_bufferedTestHelper.TargetFilePath)
            .WithArguments(_bufferedTestHelper.Arguments)
            .WithValidation(ProcessResultValidation.ExitCodeZero);
        
        CliCommandConfiguration configuration = commandConfigurationBuilder.Build();

        BufferedProcessResult result = await _cliCommandInvoker.ExecuteBufferedAsync(configuration);

        return (result.StandardOutput, result.StandardError);
    }
    
    [Benchmark]
    public async Task<(string standardOutput, string standardError)> CliWrap()
    {
        BufferedCommandResult result = await Cli.Wrap(_bufferedTestHelper.TargetFilePath)
            .WithArguments(_bufferedTestHelper.Arguments)
            .WithValidation(global::CliWrap.CommandResultValidation.ZeroExitCode)
            .ExecuteBufferedAsync();
      
        return (result.StandardOutput, result.StandardError);
    }
    
    [Benchmark]
    public async Task<(string standardOutput, string standardError)> MedallionShell()
    {
        Medallion.Shell.CommandResult result = await Medallion.Shell.Command.Run(_bufferedTestHelper.TargetFilePath, _bufferedTestHelper.Arguments).Task;
        
        return (result.StandardOutput, result.StandardError);
    }
    
    [Benchmark]
    public async Task<(string standardOut, string standardError)> ProcessX()
    {
        (Process process,
            ProcessAsyncEnumerable stdOut,
            ProcessAsyncEnumerable stdError) result =
            Cysharp.Diagnostics.ProcessX.GetDualAsyncEnumerable($"{_bufferedTestHelper.TargetFilePath} {_bufferedTestHelper.Arguments}");

        Task<string[]> standardOutTask = result.stdOut.ToTask();
        Task<string[]> standardErrorTask = result.stdError.ToTask();

        await Task.WhenAll([standardOutTask, standardErrorTask]);
        
        string standardOut = string.Join(Environment.NewLine, standardOutTask.Result);
        string standardError = string.Join(Environment.NewLine, standardErrorTask.Result);
        
        return (standardOut, standardError);
    }
}