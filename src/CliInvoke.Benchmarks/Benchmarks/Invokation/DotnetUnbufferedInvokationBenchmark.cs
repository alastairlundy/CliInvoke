using System.ComponentModel;
using System.Diagnostics;

using AlastairLundy.CliInvoke;
using AlastairLundy.CliInvoke.Abstractions;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Builders.Abstractions;

using AlastairLundy.CliInvoke.Core.Abstractions;
using AlastairLundy.CliInvoke.Core.Abstractions.Builders;
using AlastairLundy.CliInvoke.Core.Abstractions.Legacy;
using AlastairLundy.CliInvoke.Core.Abstractions.Legacy.Utilities;
using AlastairLundy.CliInvoke.Core.Abstractions.Piping;
using AlastairLundy.CliInvoke.Core.Primitives;
using AlastairLundy.CliInvoke.Core.Primitives.Results;

using AlastairLundy.CliInvoke.Legacy;
using AlastairLundy.CliInvoke.Legacy.Piping;
using AlastairLundy.CliInvoke.Legacy.Utilities;

using AlastairLundy.Extensions.IO.Abstractions.Files;
using AlastairLundy.Extensions.IO.Files;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using CliInvoke.Benchmarking.Data;

using CliWrap;

namespace CliInvoke.Benchmarking.Benchmarks.Invokation;

[MemoryDiagnoser(true), Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net90)]
public class DotnetUnbufferedInvokationBenchmark
{
    private readonly IProcessFactory _processFactory;
    private readonly ICliCommandInvoker _cliCommandInvoker;
    
    private DotnetCommandHelper _dotnetCommandHelper;
    
    public DotnetUnbufferedInvokationBenchmark()
    {
        IFilePathResolver filePathResolver = new FilePathResolver();

        IProcessPipeHandler processPipeHandler = new ProcessPipeHandler();
        IProcessRunnerUtility processRunnerUtility = new ProcessRunnerUtility(filePathResolver);
        IPipedProcessRunner pipedProcessRunner = new PipedProcessRunner(processRunnerUtility,processPipeHandler);
        ICommandProcessFactory commandProcessFactory = new CommandProcessFactory();
        
        _processFactory = new ProcessFactory(filePathResolver);
        _cliCommandInvoker = new CliCommandInvoker(pipedProcessRunner, processPipeHandler, commandProcessFactory);
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
            .WithArguments("--list-sdks")
            .WithValidation(ProcessResultValidation.ExitCodeZero);
        
        CliCommandConfiguration configuration = commandConfigurationBuilder.Build();

        ProcessResult result = await _cliCommandInvoker.ExecuteAsync(configuration);
        
        return result.ExitCode;
    }

    [Benchmark]
    [DisplayName("CliWrap")]
    public async Task<int> CliWrap_Benchmark()
    {
      CliWrap.CommandResult result = await Cli.Wrap(_dotnetCommandHelper.DotnetExecutableTargetFilePath)
            .WithArguments("--list-sdks")
            .WithValidation(CliWrap.CommandResultValidation.ZeroExitCode)
            .ExecuteAsync();
      
      return result.ExitCode;
    }

    [Benchmark]
    [DisplayName("MedallionShell")]
    public async Task<int> MedallionShell_Benchmark()
    {
        Medallion.Shell.CommandResult result = await Medallion.Shell.Command
            .Run(_dotnetCommandHelper.DotnetExecutableTargetFilePath, "--list-sdks").Task;
        
        return result.ExitCode;
    }
}