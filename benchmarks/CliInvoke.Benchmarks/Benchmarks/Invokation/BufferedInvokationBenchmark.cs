using CliInvoke.Benchmarking.Data;
using CliInvoke.Benchmarking.Helpers;
using CliInvoke.Builders;
using CliInvoke.Core;
using CliInvoke.Core.Builders;
using CliWrap;
using CliWrap.Buffered;

namespace CliInvoke.Benchmarking.Benchmarks.Invokation;

[SimpleJob(RuntimeMoniker.Net90)]
[SimpleJob(RuntimeMoniker.Mono10_0)]
[MemoryDiagnoser(true), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class BufferedInvokationBenchmark
{
    private readonly IProcessInvoker _processInvoker;

    private readonly BufferedTestHelper _bufferedTestHelper;

    public BufferedInvokationBenchmark()
    {
        _processInvoker = CliInvokeHelpers.CreateProcessInvoker();
        _bufferedTestHelper = new BufferedTestHelper();
    }
    
    [Benchmark]
    public async Task<string> CliInvoke_ProcessInvoker()
    {
        IProcessConfigurationBuilder processConfigurationBuilder = new ProcessConfigurationBuilder(
                _bufferedTestHelper.TargetFilePath)
            .SetArguments(_bufferedTestHelper.Arguments)
            .RedirectStandardOutput(false)
            .RedirectStandardError(false);

        ProcessConfiguration configuration = processConfigurationBuilder.Build();

        BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(configuration);

        return result.StandardOutput;
    }
    
    [Benchmark]
    public async Task<string> CliWrap()
    {
        BufferedCommandResult result = await Cli.Wrap(_bufferedTestHelper.TargetFilePath)
            .WithArguments(_bufferedTestHelper.Arguments)
            .WithValidation(CommandResultValidation.ZeroExitCode)
            .ExecuteBufferedAsync();

        return result.StandardOutput;
    }
    
    [Benchmark]
    public async Task<string> MedallionShell()
    {
        Medallion.Shell.CommandResult result = await Medallion
            .Shell.Command.Run(
                _bufferedTestHelper.TargetFilePath,
                _bufferedTestHelper.Arguments
            )
            .Task;

        return result.StandardOutput;
    }
    
    [Benchmark]
    public async Task<string> SimpleExec()
    {
        (string StandardOutput, string StandardError) result = await global::SimpleExec.Command.ReadAsync(
            _bufferedTestHelper.TargetFilePath,
            _bufferedTestHelper.Arguments,
            handleExitCode: code => code == 0
        );

        return result.StandardOutput;
    }
}