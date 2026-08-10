using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CliInvoke.Builders;
using CliInvoke.Core;
using CliInvoke.Core.Middleware;

namespace CliInvoke.Tests.Middleware;

public class ChainDisposalTests
{
    [Test]
    public async Task Chain_ReturnsPipedProcessResult_WithoutDisposing()
    {
        var stdout = new MemoryStream();
        var stderr = new MemoryStream();
        var pipedResult = new PipedProcessResult("test.exe", 0, 1, DateTime.UtcNow, DateTime.UtcNow, stdout, stderr);

        var chain = new MiddlewareChain(
            new List<IProcessMiddleware>(),
            (ctx, ct) =>
            {
                ctx.Result = pipedResult;
                return Task.CompletedTask;
            });

        var ctx = new InvocationContext(
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
        var stdout = new MemoryStream();
        var stderr = new MemoryStream();
        var pipedResult = new PipedProcessResult("test.exe", 0, 1, DateTime.UtcNow, DateTime.UtcNow, stdout, stderr);

        var chain = new MiddlewareChain(
            new List<IProcessMiddleware>(),
            (ctx, ct) =>
            {
                ctx.Result = pipedResult;
                return Task.CompletedTask;
            });

        var ctx = new InvocationContext(
            new ProcessConfigurationBuilder("test.exe").Build(),
            ProcessExitConfiguration.Default,
            InvocationMode.Piped);

        await chain.RunAsync(ctx, CancellationToken.None);

        // User disposes after chain returns
        var result = (PipedProcessResult)ctx.Result!;
        result.Dispose();

        await Assert.That(stdout.CanRead).IsEqualTo(false);
        await Assert.That(stderr.CanRead).IsEqualTo(false);
    }
}
