using System.Collections.Generic;
using CliInvoke.Core.Middleware;

namespace CliInvoke.Tests.Middleware;

public class ChainDisposalTests
{
    [Test]
    public async Task Chain_ReturnsBufferedProcessResult_WithoutDisposing()
    {
        BufferedProcessResult bufferedResult = new BufferedProcessResult(
            "test.exe", 0, 1, "output", "error", DateTime.UtcNow, DateTime.UtcNow,
            canceled: false, signal: null, wasTruncated: false);

        MiddlewareChain chain = new MiddlewareChain(
            new List<IProcessMiddleware>(),
            (ctx) =>
            {
                ctx.Result = bufferedResult;
                return Task.CompletedTask;
            });

        InvocationContext ctx = new InvocationContext(
            new ProcessConfigurationBuilder("test.exe").Build(),
            ProcessExitConfiguration.Default,
            InvocationMode.Buffered);

        await chain.RunAsync(ctx, CancellationToken.None);

        await Assert.That(ctx.Result).IsEqualTo(bufferedResult);
        await Assert.That(bufferedResult.StandardOutput).IsEqualTo("output");
    }

    [Test]
    public async Task UserCanReadResult_AfterChainReturns()
    {
        BufferedProcessResult bufferedResult = new BufferedProcessResult(
            "test.exe", 0, 1, "output", "error", DateTime.UtcNow, DateTime.UtcNow,
            canceled: false, signal: null, wasTruncated: false);

        MiddlewareChain chain = new MiddlewareChain(
            new List<IProcessMiddleware>(),
            (ctx) =>
            {
                ctx.Result = bufferedResult;
                return Task.CompletedTask;
            });

        InvocationContext ctx = new InvocationContext(
            new ProcessConfigurationBuilder("test.exe").Build(),
            ProcessExitConfiguration.Default,
            InvocationMode.Buffered);

        await chain.RunAsync(ctx, CancellationToken.None);

        BufferedProcessResult result = (BufferedProcessResult)ctx.Result!;
        await Assert.That(result.StandardOutput).IsEqualTo("output");
        await Assert.That(result.StandardError).IsEqualTo("error");
    }
}
