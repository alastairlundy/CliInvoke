using AlastairLundy.CliInvoke;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Core.Builders;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using CliInvoke.Benchmarking.Data;
using CliInvoke.Benchmarking.Helpers;
using CliWrap;

namespace CliInvoke.Benchmarking.Benchmarks.Invokation;

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser(true), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class BasicUnbufferedInvokationBenchmark
{
    private readonly IProcessInvoker _processInvoker;

    private readonly DotnetCommandHelper _dotnetCommandHelper;

    public BasicUnbufferedInvokationBenchmark()
    {
        _processInvoker = CliInvokeHelpers.CreateProcessInvoker();
        _dotnetCommandHelper = new DotnetCommandHelper();
    }

    [Benchmark]
    public async Task<int> CliInvoke_ProcessInvoker()
    {
        IProcessConfigurationBuilder processConfigurationBuilder = new ProcessConfigurationBuilder(
            _dotnetCommandHelper.DotnetExecutableTargetFilePath
        )
            .SetArguments(_dotnetCommandHelper.Arguments)
            .RedirectStandardOutput(false)
            .RedirectStandardError(false);

        ProcessConfiguration configuration = processConfigurationBuilder.Build();

        ProcessResult result = await _processInvoker.ExecuteAsync(configuration);

        return result.ExitCode;
    }

    [Benchmark]
    public async Task<int> CliWrap()
    {
        CommandResult result = await Cli.Wrap(_dotnetCommandHelper.DotnetExecutableTargetFilePath)
            .WithArguments(_dotnetCommandHelper.Arguments)
            .WithValidation(CommandResultValidation.ZeroExitCode)
            .ExecuteAsync();

        return result.ExitCode;
    }

    [Benchmark]
    public async Task<int> MedallionShell()
    {
        Medallion.Shell.CommandResult result = await Medallion
            .Shell.Command.Run(
                _dotnetCommandHelper.DotnetExecutableTargetFilePath,
                _dotnetCommandHelper.Arguments
            )
            .Task;

        return result.ExitCode;
    }

    [Benchmark]
    public async Task<int> SimpleExec()
    {
        int exitCode = 0;

        await global::SimpleExec.Command.RunAsync(
            _dotnetCommandHelper.DotnetExecutableTargetFilePath,
            _dotnetCommandHelper.Arguments,
            createNoWindow: true,
            handleExitCode: code => (exitCode = code) < 8
        );

        return await new ValueTask<int>(exitCode);
    }
}
