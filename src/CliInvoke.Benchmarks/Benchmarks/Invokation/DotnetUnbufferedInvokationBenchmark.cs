using System.ComponentModel;
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

using CliCommandConfiguration = AlastairLundy.CliInvoke.CliCommandConfiguration;

namespace CliInvoke.Benchmarking.Benchmarks.Invokation;

[MemoryDiagnoser(true), 
 Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class DotnetUnbufferedInvokationBenchmark
{
    private readonly IProcessFactory _processFactory;
    private readonly ICliCommandInvoker _cliCommandInvoker;
    
    private DotnetCommandHelper _dotnetCommandHelper;
    
    public DotnetUnbufferedInvokationBenchmark()
    {
        _processFactory = CliInvokeHelpers.CreateProcessFactory();
        _cliCommandInvoker = CliInvokeHelpers.CreateCliCommandInvoker();
    }
    
    [GlobalSetup]
    public void Setup()
    {
        _dotnetCommandHelper = new DotnetCommandHelper();
    }

    [Benchmark]
    [DisplayName("CliInvoke_ProcessFactory")]
    public async Task<int> CliInvoke_ProcessFactory_Benchmark()
    {
        ProcessConfiguration processConfiguration =
#pragma warning disable CA1416
            new ProcessConfiguration(_dotnetCommandHelper.DotnetExecutableTargetFilePath, "--list-sdks", 
                commandResultValidation: ProcessResultValidation.ExitCodeZero);
#pragma warning restore CA1416

        Process process = _processFactory.StartNew(processConfiguration);
       
      ProcessResult result = await _processFactory.ContinueWhenExitAsync(process);
      
      return result.ExitCode;
    }
    
    [Benchmark]
    [DisplayName("CliInvoke_CliCommandInvoker")]
    public async Task<int> CliInvoke_CliCommandInvoker_Benchmark()
    {
        ICliCommandConfigurationBuilder commandConfigurationBuilder = new
                CliCommandConfigurationBuilder(_dotnetCommandHelper.DotnetExecutableTargetFilePath)
            .WithTargetFile(_dotnetCommandHelper.DotnetExecutableTargetFilePath)
            .WithArguments("--list-sdks")
            .WithValidation(ProcessResultValidation.ExitCodeZero);
        
        CliCommandConfiguration configuration = commandConfigurationBuilder.Build();

        ProcessResult result = await _cliCommandInvoker.ExecuteAsync(configuration);
        
        return result.ExitCode;
    }

    [Benchmark]
    [DisplayName("CliWrap")]
    public async Task<int> CliWrap()
    {
      CliWrap.CommandResult result = await Cli.Wrap(_dotnetCommandHelper.DotnetExecutableTargetFilePath)
            .WithArguments("--list-sdks")
            .WithValidation(global::CliWrap.CommandResultValidation.ZeroExitCode)
            .ExecuteAsync();
      
      return result.ExitCode;
    }

    [Benchmark]
    [DisplayName("MedallionShell")]
    public async Task<int> MedallionShell()
    {
        Medallion.Shell.CommandResult result = await Medallion.Shell.Command
            .Run(_dotnetCommandHelper.DotnetExecutableTargetFilePath, "--list-sdks").Task;
        
        return result.ExitCode;
    }
}