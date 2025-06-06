﻿using System.Diagnostics;

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
        _dotnetCommandHelper = new DotnetCommandHelper();
        _processFactory = CliInvokeHelpers.CreateProcessFactory();
        _cliCommandInvoker = CliInvokeHelpers.CreateCliCommandInvoker();
    }

    [Benchmark]
    public async Task<int> CliInvoke_ProcessFactory()
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
    public async Task<int> CliInvoke_CliCommandInvoker()
    {
        ICliCommandConfigurationBuilder commandConfigurationBuilder = new
                CliCommandConfigurationBuilder(_dotnetCommandHelper.DotnetExecutableTargetFilePath)
            .WithArguments(_dotnetCommandHelper.Arguments)
            .WithValidation(ProcessResultValidation.ExitCodeZero);
        
        CliCommandConfiguration configuration = commandConfigurationBuilder.Build();

        ProcessResult result = await _cliCommandInvoker.ExecuteAsync(configuration);
        
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
}