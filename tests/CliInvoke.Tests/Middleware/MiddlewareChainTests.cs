using System.Collections.Generic;
using CliInvoke.Core.Middleware;
using TUnit.Assertions.Enums;

namespace CliInvoke.Tests.Middleware;

public class MiddlewareChainTests
{
    [Test]
    public async Task RunAsync_InvokesMiddlewareInRegistrationOrder()
    {
        List<string> callLog = new List<string>();
        FakeMiddleware middlewareA = new FakeMiddleware("A", callLog);
        FakeMiddleware middlewareB = new FakeMiddleware("B", callLog);
        
        MiddlewareChain chain = new MiddlewareChain(
            new List<IProcessMiddleware> { middlewareA, middlewareB },
            (ctx, ct) => { callLog.Add("terminal"); return Task.CompletedTask; });

        InvocationContext ctx = new InvocationContext(
            new ProcessConfigurationBuilder("test.exe").Build(),
            ProcessExitConfiguration.Default,
            InvocationMode.Raw);

        await chain.RunAsync(ctx, CancellationToken.None);

        await Assert.That(callLog).IsEquivalentTo(new List<string> { "A", "B", "terminal" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task RunAsync_ShortCircuitsWhenMiddlewareDoesNotCallNext()
    {
        List<string> callLog = new List<string>();
        FakeMiddleware middlewareA = new FakeMiddleware("A", callLog, FakeMiddlewareMode.NeverInvokeNext);
        FakeMiddleware middlewareB = new FakeMiddleware("B", callLog);

        MiddlewareChain chain = new MiddlewareChain(
            new List<IProcessMiddleware> { middlewareA, middlewareB },
            (ctx, ct) => { callLog.Add("terminal"); return Task.CompletedTask; });

        InvocationContext ctx = new InvocationContext(
            new ProcessConfigurationBuilder("test.exe").Build(),
            ProcessExitConfiguration.Default,
            InvocationMode.Raw);

        await chain.RunAsync(ctx, CancellationToken.None);

        await Assert.That(callLog).IsEquivalentTo(new List<string> { "A" });
    }

    [Test]
    public async Task RunAsync_PropagatesExceptionFromTerminal()
    {
        List<string> callLog = new List<string>();
        InvalidOperationException expectedException = new InvalidOperationException("terminal failed");
        FakeMiddleware middlewareA = new FakeMiddleware("A", callLog);

        MiddlewareChain chain = new MiddlewareChain(
            new List<IProcessMiddleware> { middlewareA },
            (ctx, ct) => throw expectedException);

        InvocationContext ctx = new InvocationContext(
            new ProcessConfigurationBuilder("test.exe").Build(),
            ProcessExitConfiguration.Default,
            InvocationMode.Raw);

        InvalidOperationException? thrown = await Assert.That(() => chain.RunAsync(ctx, CancellationToken.None))
            .Throws<InvalidOperationException>();
        
        await Assert.That(thrown.Message).IsEqualTo("terminal failed");
    }

    [Test]
    public async Task RunAsync_PropagatesExceptionFromUpstreamMiddleware()
    {
        List<string> callLog = new List<string>();
        InvalidOperationException expectedException = new InvalidOperationException("upstream failed");
        FakeMiddleware middlewareA = new FakeMiddleware("A", callLog, FakeMiddlewareMode.ThrowOnInvoke, expectedException);
        FakeMiddleware middlewareB = new FakeMiddleware("B", callLog);

        MiddlewareChain chain = new MiddlewareChain(
            new List<IProcessMiddleware> { middlewareA, middlewareB },
            (ctx, ct) => { callLog.Add("terminal"); return Task.CompletedTask; });

        InvocationContext ctx = new InvocationContext(
            new ProcessConfigurationBuilder("test.exe").Build(),
            ProcessExitConfiguration.Default,
            InvocationMode.Raw);

        InvalidOperationException? thrown = await Assert.That(() => chain.RunAsync(ctx, CancellationToken.None))
            .Throws<InvalidOperationException>();

        await Assert.That(thrown.Message).IsEqualTo("upstream failed");
        await Assert.That(callLog).IsEquivalentTo(new List<string> { "A" });
    }

    [Test]
    public async Task RunAsync_PropagatesCancelledToken()
    {
        List<string> callLog = new List<string>();
        FakeMiddleware middlewareA = new FakeMiddleware("A", callLog);

        MiddlewareChain chain = new MiddlewareChain(
            new List<IProcessMiddleware> { middlewareA },
            (ctx, ct) =>
            {
                callLog.Add("terminal");
                ct.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            });

        using CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        InvocationContext ctx = new InvocationContext(
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
        List<string> callLog = new List<string>();
        FakeMiddleware middlewareA = new FakeMiddleware("A", callLog);
        FakeMiddleware middlewareB = new FakeMiddleware("B", callLog);

        MiddlewareChain chain = new MiddlewareChain(
            new List<IProcessMiddleware> { middlewareA, middlewareB },
            (ctx, ct) => { callLog.Add("terminal"); return Task.CompletedTask; });

        InvocationContext ctx = new InvocationContext(
            new ProcessConfigurationBuilder("test.exe").Build(),
            ProcessExitConfiguration.Default,
            InvocationMode.Raw);

        await chain.RunAsync(ctx, CancellationToken.None);

        await Assert.That(callLog[^1]).IsEqualTo("terminal");
    }

    [Test]
    public async Task RunAsync_EmptyMiddlewareList_InvokesTerminalDirectly()
    {
        List<string> callLog = new List<string>();

        MiddlewareChain chain = new MiddlewareChain(
            new List<IProcessMiddleware>(),
            (ctx, ct) => { callLog.Add("terminal"); return Task.CompletedTask; });

        InvocationContext ctx = new InvocationContext(
            new ProcessConfigurationBuilder("test.exe").Build(),
            ProcessExitConfiguration.Default,
            InvocationMode.Raw);

        await chain.RunAsync(ctx, CancellationToken.None);

        await Assert.That(callLog).IsEquivalentTo(new List<string> { "terminal" });
    }
}
