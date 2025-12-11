using System.Diagnostics;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CliInvoke.Benchmarking.Data;
using CliInvoke.Benchmarking.Helpers;
using CliInvoke.Core;

using CliWrap;
using CliWrap.Buffered;
using Cysharp.Diagnostics;

namespace CliInvoke.Benchmarking.Benchmarks.Invokation;

[MemoryDiagnoser(true), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class BufferedInvokationBenchmark
{
    private readonly IProcessInvoker _processInvoker;

    private readonly BufferedTestHelper _bufferedTestHelper;

    public BufferedInvokationBenchmark()
    {
        _bufferedTestHelper = new BufferedTestHelper();
        _processInvoker = CliInvokeHelpers.CreateProcessInvoker();
    }

    [Benchmark]
    public async Task<(string standardOutput, string standardError)> CliInvoke_ProcessFactory()
    {
        using ProcessConfiguration processConfiguration =
            new ProcessConfiguration(_bufferedTestHelper.TargetFilePath,
                false,
                true,
                true,
                _bufferedTestHelper.Arguments);

        BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(processConfiguration,
            ProcessExitConfiguration.NoTimeoutDefault);
      
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
        Medallion.Shell.CommandResult result = await Medallion.Shell.Command.Run(_bufferedTestHelper.TargetFilePath,
            _bufferedTestHelper.Arguments).Task;
        
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
        
        string standardOut = string.Join(Environment.NewLine,
            standardOutTask.Result);
        string standardError = string.Join(Environment.NewLine,
            standardErrorTask.Result);
        
        return (standardOut, standardError);
    }
}
