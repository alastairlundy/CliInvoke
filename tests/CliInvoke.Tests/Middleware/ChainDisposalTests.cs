using System.Collections.Generic;
using CliInvoke.Core.Middleware;

namespace CliInvoke.Tests.Middleware;

public class ChainDisposalTests
{
    [Test]
    public async Task Chain_ReturnsPipedProcessResult_WithoutDisposing()
    {
        MemoryStream stdout = new MemoryStream();
        MemoryStream stderr = new MemoryStream();
        PipedProcessResult pipedResult = new PipedProcessResult("test.exe", 0, 1, DateTime.UtcNow, DateTime.UtcNow, stdout, stderr);

        MiddlewareChain chain = new MiddlewareChain(
            new List<IProcessMiddleware>(),
            (ctx, ct) =>
            {
                ctx.Result = pipedResult;
                return Task.CompletedTask;
            });

        InvocationContext ctx = new InvocationContext(
            new ProcessConfigurationBuilder("test.exe").Build(),
            ProcessExitConfiguration.Default,
            InvocationMode.Piped);

        await chain.RunAsync(ctx, CancellationToken.None);

        await Assert.That(ctx.Result).IsEqualTo(pipedResult);
        await Assert.That(stdout.CanRead).IsEqualTo(true);
        await Assert.That(stderr.CanRead).IsEqualTo(true);
    }

    [Test]
    public async Task UserCanDisposeResult_AfterChainReturns()
    {
        MemoryStream stdout = new MemoryStream();
        MemoryStream stderr = new MemoryStream();
        PipedProcessResult pipedResult = new PipedProcessResult("test.exe", 0, 1, DateTime.UtcNow, DateTime.UtcNow, stdout, stderr);

        MiddlewareChain chain = new MiddlewareChain(
            new List<IProcessMiddleware>(),
            (ctx, ct) =>
            {
                ctx.Result = pipedResult;
                return Task.CompletedTask;
            });

        InvocationContext ctx = new InvocationContext(
            new ProcessConfigurationBuilder("test.exe").Build(),
            ProcessExitConfiguration.Default,
            InvocationMode.Piped);

        await chain.RunAsync(ctx, CancellationToken.None);

        // User disposes after chain returns
        PipedProcessResult result = (PipedProcessResult)ctx.Result!;
        result.Dispose();

        await Assert.That(stdout.CanRead).IsEqualTo(false);
        await Assert.That(stderr.CanRead).IsEqualTo(false);
    }
}
