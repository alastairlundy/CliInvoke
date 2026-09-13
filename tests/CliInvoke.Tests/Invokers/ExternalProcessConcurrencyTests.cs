using System.Threading;
using CliInvoke.Core;
using CliInvoke.Core.Processes;
using CliInvoke.Factories;
using CliInvoke.Piping;
using CliInvoke.Tests.Internal.Constants;
using Assert = Xunit.Assert;

namespace CliInvoke.Tests.Invokers;

#pragma warning disable CS0618
public class ExternalProcessConcurrencyTests
{
    private static readonly ExternalProcessFactory Factory = new(FilePathResolver.Shared, ProcessPipeHandler.Shared);

    private static ProcessExitConfiguration CreateNoTimeoutExitConfig()
    {
        return new ProcessExitConfiguration(
            ProcessTimeoutPolicy.None,
            ProcessResultValidation.None,
            ProcessCancellationExceptionBehavior.SuppressException);
    }

    [Fact]
    public async Task Concurrent_BufferedRuns_NoCancellation_NoOCE()
    {
        int batchSize = 4;
        int iterations = 10;

        for (int i = 0; i < iterations; i++)
        {
            Task<BufferedProcessResult>[] tasks = new Task<BufferedProcessResult>[batchSize];

            for (int j = 0; j < batchSize; j++)
            {
                tasks[j] = Task.Run(async () =>
                {
                    using ProcessConfiguration config = new ProcessConfiguration(
                        ProcessTestHelper.GetTargetFilePath(),
                        arguments: "/c echo test-output",
                        redirectOutputs: true);

                    using IExternalProcess process = Factory.CreateExternalProcess(config,
                        CreateNoTimeoutExitConfig());
                    await process.StartAsync(CancellationToken.None);
                    return await process.WaitForBufferedExitOrTimeoutAsync(CancellationToken.None);
                });
            }

            BufferedProcessResult[] results = await Task.WhenAll(tasks);

            foreach (BufferedProcessResult result in results)
            {
                Assert.NotNull(result);
                Assert.Equal(0, result.ExitCode);
            }
        }
    }

    [Fact]
    public async Task Concurrent_BufferedRuns_OutputCorrectness()
    {
        int batchSize = 4;
        Task<BufferedProcessResult>[] tasks = new Task<BufferedProcessResult>[batchSize];

        for (int j = 0; j < batchSize; j++)
        {
            tasks[j] = Task.Run(async () =>
            {
                using ProcessConfiguration config = new ProcessConfiguration(
                    ProcessTestHelper.GetTargetFilePath(),
                    arguments: "/c echo test-output",
                    redirectOutputs: true);

                using IExternalProcess process = Factory.CreateExternalProcess(config,
                    CreateNoTimeoutExitConfig());
                await process.StartAsync(CancellationToken.None);
                return await process.WaitForBufferedExitOrTimeoutAsync(CancellationToken.None);
            });
        }

        BufferedProcessResult[] results = await Task.WhenAll(tasks);

        foreach (BufferedProcessResult result in results)
        {
            Assert.NotNull(result);
            Assert.Equal(0, result.ExitCode);
            Assert.Contains("test-output", result.StandardOutput);
        }
    }

    [Fact]
    public async Task CallerCancellation_CompletesCleanlyWithoutHang()
    {
        using ProcessConfiguration config = new ProcessConfiguration(
            ProcessTestHelper.GetTargetFilePath(),
            arguments: "/c timeout 10 /nobreak",
            redirectOutputs: true);

        using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        using IExternalProcess process = Factory.CreateExternalProcess(config,
            CreateNoTimeoutExitConfig());
        await process.StartAsync(cts.Token);

        Stopwatch sw = Stopwatch.StartNew();
        BufferedProcessResult result = await process.WaitForBufferedExitOrTimeoutAsync(cts.Token);
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 5000,
            $"Expected cancellation to complete quickly but took {sw.ElapsedMilliseconds}ms");
        Assert.NotNull(result);
    }
}
