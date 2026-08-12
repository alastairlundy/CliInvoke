using System.Collections.Generic;
using CliInvoke.Core.Middleware;

namespace CliInvoke.Tests.Middleware;

public class MiddlewareChainTests
{
    [Test]
    public async Task RunAsync_InvokesMiddlewareInRegistrationOrder()
    {
        var callLog = new List<string>();
        var middlewareA = new FakeMiddleware("A", callLog);
        var middlewareB = new FakeMiddleware("B", callLog);
        
        var chain = new MiddlewareChain(
            new List<IProcessMiddleware> { middlewareA, middlewareB },
            (ctx, ct) => { callLog.Add("terminal"); return Task.CompletedTask; });

        var ctx = new InvocationContext(
            new ProcessConfigurationBuilder("test.exe").Build(),
            ProcessExitConfiguration.Default,
            InvocationMode.Raw);

        await chain.RunAsync(ctx, CancellationToken.None);

        await Assert.That(callLog).IsEquivalentTo(new List<string> { "A", "B", "terminal" });
    }

    [Test]
    public async Task RunAsync_ShortCircuitsWhenMiddlewareDoesNotCallNext()
    {
        var callLog = new List<string>();
        var middlewareA = new FakeMiddleware("A", callLog, FakeMiddlewareMode.NeverInvokeNext);
        var middlewareB = new FakeMiddleware("B", callLog);

        var chain = new MiddlewareChain(
            new List<IProcessMiddleware> { middlewareA, middlewareB },
            (ctx, ct) => { callLog.Add("terminal"); return Task.CompletedTask; });

        var ctx = new InvocationContext(
            new ProcessConfigurationBuilder("test.exe").Build(),
            ProcessExitConfiguration.Default,
            InvocationMode.Raw);

        await chain.RunAsync(ctx, CancellationToken.None);

        await Assert.That(callLog).IsEquivalentTo(new List<string> { "A" });
    }

    [Test]
    public async Task RunAsync_PropagatesExceptionFromTerminal()
    {
        var callLog = new List<string>();
        var expectedException = new InvalidOperationException("terminal failed");
        var middlewareA = new FakeMiddleware("A", callLog);

        var chain = new MiddlewareChain(
            new List<IProcessMiddleware> { middlewareA },
            (ctx, ct) => throw expectedException);

        var ctx = new InvocationContext(
            new ProcessConfigurationBuilder("test.exe").Build(),
            ProcessExitConfiguration.Default,
            InvocationMode.Raw);

        var thrown = await Assert.That(() => chain.RunAsync(ctx, CancellationToken.None))
            .Throws<InvalidOperationException>();
        
        await Assert.That(thrown.Message).IsEqualTo("terminal failed");
    }

    [Test]
    public async Task RunAsync_PropagatesExceptionFromUpstreamMiddleware()
    {
        var callLog = new List<string>();
        var expectedException = new InvalidOperationException("upstream failed");
        var middlewareA = new FakeMiddleware("A", callLog, FakeMiddlewareMode.ThrowOnInvoke, expectedException);
        var middlewareB = new FakeMiddleware("B", callLog);

        var chain = new MiddlewareChain(
            new List<IProcessMiddleware> { middlewareA, middlewareB },
            (ctx, ct) => { callLog.Add("terminal"); return Task.CompletedTask; });

        var ctx = new InvocationContext(
            new ProcessConfigurationBuilder("test.exe").Build(),
            ProcessExitConfiguration.Default,
            InvocationMode.Raw);

        var thrown = await Assert.That(() => chain.RunAsync(ctx, CancellationToken.None))
            .Throws<InvalidOperationException>();

        await Assert.That(thrown.Message).IsEqualTo("upstream failed");
        await Assert.That(callLog).IsEquivalentTo(new List<string> { "A" });
    }

    [Test]
    public async Task RunAsync_PropagatesCancelledToken()
    {
        var callLog = new List<string>();
        var middlewareA = new FakeMiddleware("A", callLog);

        var chain = new MiddlewareChain(
            new List<IProcessMiddleware> { middlewareA },
            (ctx, ct) =>
            {
                callLog.Add("terminal");
                ct.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            });

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var ctx = new InvocationContext(
            new ProcessConfigurationBuilder("test.exe").Build(),
            ProcessExitConfiguration.Default,
            InvocationMode.Raw,
            cts.Token);

        await Assert.That(() => chain.RunAsync(ctx, CancellationToken.None))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task RunAsync_TerminalPipelineIsAlwaysLastStep()
    {
        var callLog = new List<string>();
        var middlewareA = new FakeMiddleware("A", callLog);
        var middlewareB = new FakeMiddleware("B", callLog);

        var chain = new MiddlewareChain(
            new List<IProcessMiddleware> { middlewareA, middlewareB },
            (ctx, ct) => { callLog.Add("terminal"); return Task.CompletedTask; });

        var ctx = new InvocationContext(
            new ProcessConfigurationBuilder("test.exe").Build(),
            ProcessExitConfiguration.Default,
            InvocationMode.Raw);

        await chain.RunAsync(ctx, CancellationToken.None);

        await Assert.That(callLog[^1]).IsEqualTo("terminal");
    }

    [Test]
    public async Task RunAsync_EmptyMiddlewareList_InvokesTerminalDirectly()
    {
        var callLog = new List<string>();

        var chain = new MiddlewareChain(
            new List<IProcessMiddleware>(),
            (ctx, ct) => { callLog.Add("terminal"); return Task.CompletedTask; });

        var ctx = new InvocationContext(
            new ProcessConfigurationBuilder("test.exe").Build(),
            ProcessExitConfiguration.Default,
            InvocationMode.Raw);

        await chain.RunAsync(ctx, CancellationToken.None);

        await Assert.That(callLog).IsEquivalentTo(new List<string> { "terminal" });
    }
}
