using System.Diagnostics;

using AlastairLundy.CliInvoke.Core;

using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.DotPrimitives.Processes;
using AlastairLundy.DotPrimitives.Processes.Results;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;

using CliInvoke.Benchmarking.Data;
using CliInvoke.Benchmarking.Helpers;

using CliWrap;

namespace CliInvoke.Benchmarking.Benchmarks.Invokation;

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser(true), 
 Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class BasicUnbufferedInvokationBenchmark
{
    private readonly IProcessFactory _processFactory;
    private readonly IProcessInvoker _processInvoker;
    
    private DotnetCommandHelper _dotnetCommandHelper;
    
    public BasicUnbufferedInvokationBenchmark()
    {
        _dotnetCommandHelper = new DotnetCommandHelper();
        _processFactory = CliInvokeHelpers.CreateProcessFactory();
        _processInvoker = CliInvokeHelpers.CreateProcessInvoker();
    }

    [Benchmark]
    public async Task<int> CliInvoke_ProcessFactory()
    {
        IProcessConfigurationBuilder processConfigurationBuilder = new
                ProcessConfigurationBuilder(_dotnetCommandHelper.DotnetExecutableTargetFilePath)
            .WithArguments(_dotnetCommandHelper.Arguments)
            .WithValidation(ProcessResultValidation.ExitCodeZero);
        
        ProcessConfiguration configuration = processConfigurationBuilder.Build();

        Process process = _processFactory.StartNew(configuration);
       
        ProcessResult result = await _processFactory.ContinueWhenExitAsync(process);
      
        return result.ExitCode;
    }
    
    [Benchmark]
    public async Task<int> CliInvoke_CliCommandInvoker()
    {
        IProcessConfigurationBuilder processConfigurationBuilder = new
                ProcessConfigurationBuilder(_dotnetCommandHelper.DotnetExecutableTargetFilePath)
            .WithArguments(_dotnetCommandHelper.Arguments)
            .WithValidation(ProcessResultValidation.ExitCodeZero);
        
        ProcessConfiguration configuration = processConfigurationBuilder.Build();

        ProcessResult result = await _processInvoker.ExecuteAsync(configuration);
        
        return result.ExitCode;
    }

    [Benchmark]
    public async Task<int> CliWrap()
    {
      CliWrap.CommandResult result = await Cli.Wrap(_dotnetCommandHelper.DotnetExecutableTargetFilePath)
            .WithArguments(_dotnetCommandHelper.Arguments)
            .WithValidation(global::CliWrap.CommandResultValidation.ZeroExitCode)
            .ExecuteAsync();
      
      return result.ExitCode;
    }

    [Benchmark]
    public async Task<int> MedallionShell()
    {
        Medallion.Shell.CommandResult result = await Medallion.Shell.Command
            .Run(_dotnetCommandHelper.DotnetExecutableTargetFilePath, _dotnetCommandHelper.Arguments).Task;
        
        return result.ExitCode;
    }

    [Benchmark]
    public async Task<int> SimpleExec()
    {
        int exitCode = 0;
        
        await global::SimpleExec.Command.RunAsync(_dotnetCommandHelper.DotnetExecutableTargetFilePath,
            _dotnetCommandHelper.Arguments, createNoWindow:true,  handleExitCode: code => (exitCode = code) < 8);

        return await new ValueTask<int>(exitCode);
    }
}