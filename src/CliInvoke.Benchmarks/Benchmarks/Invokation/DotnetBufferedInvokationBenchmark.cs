using System.ComponentModel;
using System.Diagnostics;

using AlastairLundy.CliInvoke;
using AlastairLundy.CliInvoke.Abstractions;

using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Builders.Abstractions;

using AlastairLundy.CliInvoke.Core.Abstractions;
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
using CliInvoke.Benchmarking.Helpers;
using CliWrap;
using CliWrap.Buffered;

using CliCommandConfiguration = AlastairLundy.CliInvoke.CliCommandConfiguration;

namespace CliInvoke.Benchmarking.Benchmarks.Invokation;

[MemoryDiagnoser(true),
 Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class DotnetBufferedInvokationBenchmark
{
    private readonly IProcessFactory _processFactory;
    private readonly ICliCommandInvoker _cliCommandInvoker;
    
    private DotnetCommandHelper _dotnetCommandHelper;

    public DotnetBufferedInvokationBenchmark()
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
    public async Task<(string standardOutput, string standardError)> CliInvoke_ProcessFactory()
    {
        ProcessConfiguration processConfiguration =
#pragma warning disable CA1416
            new ProcessConfiguration(_dotnetCommandHelper.DotnetExecutableTargetFilePath, "--list-sdks", 
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
    [DisplayName("CliInvoke_CliCommandInvoker")]
    public async Task<(string standardOutput, string standardError)> CliInvoke_CliCommandInvoker()
    {
        ICliCommandConfigurationBuilder commandConfigurationBuilder = new
                CliCommandConfigurationBuilder(_dotnetCommandHelper.DotnetExecutableTargetFilePath)
            .WithTargetFile(_dotnetCommandHelper.DotnetExecutableTargetFilePath)
            .WithArguments("--list-sdks")
            .WithValidation(ProcessResultValidation.ExitCodeZero);
        
        CliCommandConfiguration configuration = commandConfigurationBuilder.Build();

        BufferedProcessResult result = await _cliCommandInvoker.ExecuteBufferedAsync(configuration);

        return (result.StandardOutput, result.StandardError);
    }
    
    [Benchmark]
    [DisplayName("CliWrap")]
    public async Task<(string standardOutput, string standardError)> CliWrap()
    {
        BufferedCommandResult result = await Cli.Wrap(_dotnetCommandHelper.DotnetExecutableTargetFilePath)
            .WithArguments("--list-sdks")
            .WithValidation(global::CliWrap.CommandResultValidation.ZeroExitCode)
            .ExecuteBufferedAsync();
      
        return (result.StandardOutput, result.StandardError);
    }
    
    [Benchmark]
    [DisplayName("MedallionShell")]
    public async Task<(string standardOutput, string standardError)> MedallionShell()
    {
        Medallion.Shell.CommandResult result = await Medallion.Shell.Command.Run(_dotnetCommandHelper.DotnetExecutableTargetFilePath, "--list-sdks").Task;
        
        return (result.StandardOutput, result.StandardError);
    }
}