using System.Collections.Generic;
using CliInvoke.Core.Middleware;
using TUnit.Assertions.Enums;

namespace CliInvoke.Tests.Middleware;

public class UseWhenTests
{
    [Test]
    public async Task UseWhen_SyncPredicate_True_RunsSubPipeline()
    {
        List<string> callLog = [];
        ProcessMiddlewareBuilder builder = new ProcessMiddlewareBuilder(_ => throw new InvalidOperationException("Should not resolve"));

        builder.UseWhen(
            _ => true,
            sub => sub.UseMiddleware(new FakeMiddleware("SubA", callLog)));

        IReadOnlyList<IProcessMiddleware> pipeline = builder.Build();
        MiddlewareChain chain = new MiddlewareChain(pipeline, ctx =>
        {
            callLog.Add("terminal");
            return Task.CompletedTask;
        });

        InvocationContext ctx = CreateContext();
        await chain.RunAsync(ctx, CancellationToken.None);

        await Assert.That(callLog).IsEquivalentTo(new List<string> { "SubA", "terminal" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task UseWhen_SyncPredicate_False_SkipsSubPipeline()
    {
        List<string> callLog = [];
        ProcessMiddlewareBuilder builder = new ProcessMiddlewareBuilder(_ => throw new InvalidOperationException("Should not resolve"));

        builder.UseWhen(
            _ => false,
            sub => sub.UseMiddleware(new FakeMiddleware("SubA", callLog)));

        IReadOnlyList<IProcessMiddleware> pipeline = builder.Build();
        MiddlewareChain chain = new MiddlewareChain(pipeline, ctx =>
        {
            callLog.Add("terminal");
            return Task.CompletedTask;
        });

        InvocationContext ctx = CreateContext();
        await chain.RunAsync(ctx, CancellationToken.None);

        await Assert.That(callLog).IsEquivalentTo(new List<string> { "terminal" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task UseWhen_AsyncPredicate_True_RunsSubPipeline()
    {
        List<string> callLog = [];
        ProcessMiddlewareBuilder builder = new ProcessMiddlewareBuilder(_ => throw new InvalidOperationException("Should not resolve"));

        builder.UseWhen(
            _ => Task.FromResult(true),
            sub => sub.UseMiddleware(new FakeMiddleware("SubA", callLog)));

        IReadOnlyList<IProcessMiddleware> pipeline = builder.Build();
        MiddlewareChain chain = new MiddlewareChain(pipeline, ctx =>
        {
            callLog.Add("terminal");
            return Task.CompletedTask;
        });

        InvocationContext ctx = CreateContext();
        await chain.RunAsync(ctx, CancellationToken.None);

        await Assert.That(callLog).IsEquivalentTo(new List<string> { "SubA", "terminal" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task UseWhen_AsyncPredicate_False_SkipsSubPipeline()
    {
        List<string> callLog = [];
        ProcessMiddlewareBuilder builder = new ProcessMiddlewareBuilder(_ => throw new InvalidOperationException("Should not resolve"));

        builder.UseWhen(
            _ => Task.FromResult(false),
            sub => sub.UseMiddleware(new FakeMiddleware("SubA", callLog)));

        IReadOnlyList<IProcessMiddleware> pipeline = builder.Build();
        MiddlewareChain chain = new MiddlewareChain(pipeline, ctx =>
        {
            callLog.Add("terminal");
            return Task.CompletedTask;
        });

        InvocationContext ctx = CreateContext();
        await chain.RunAsync(ctx, CancellationToken.None);

        await Assert.That(callLog).IsEquivalentTo(new List<string> { "terminal" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task UseWhen_WithOuterMiddleware_PredicateTrue_RunsBoth()
    {
        List<string> callLog = [];
        ProcessMiddlewareBuilder builder = new ProcessMiddlewareBuilder(_ => throw new InvalidOperationException("Should not resolve"));

        builder.UseMiddleware(new FakeMiddleware("Outer", callLog));
        builder.UseWhen(
            _ => true,
            sub => sub.UseMiddleware(new FakeMiddleware("SubA", callLog)));

        IReadOnlyList<IProcessMiddleware> pipeline = builder.Build();
        MiddlewareChain chain = new MiddlewareChain(pipeline, ctx =>
        {
            callLog.Add("terminal");
            return Task.CompletedTask;
        });

        InvocationContext ctx = CreateContext();
        await chain.RunAsync(ctx, CancellationToken.None);

        await Assert.That(callLog).IsEquivalentTo(new List<string> { "Outer", "SubA", "terminal" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task UseWhen_WithOuterMiddleware_PredicateFalse_RunsOnlyOuter()
    {
        List<string> callLog = [];
        ProcessMiddlewareBuilder builder = new ProcessMiddlewareBuilder(_ => throw new InvalidOperationException("Should not resolve"));

        builder.UseMiddleware(new FakeMiddleware("Outer", callLog));
        builder.UseWhen(
            _ => false,
            sub => sub.UseMiddleware(new FakeMiddleware("SubA", callLog)));

        IReadOnlyList<IProcessMiddleware> pipeline = builder.Build();
        MiddlewareChain chain = new MiddlewareChain(pipeline, ctx =>
        {
            callLog.Add("terminal");
            return Task.CompletedTask;
        });

        InvocationContext ctx = CreateContext();
        await chain.RunAsync(ctx, CancellationToken.None);

        await Assert.That(callLog).IsEquivalentTo(new List<string> { "Outer", "terminal" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task UseWhen_NestedSubPipelines_PredicateTrue()
    {
        List<string> callLog = [];
        ProcessMiddlewareBuilder builder = new ProcessMiddlewareBuilder(_ => throw new InvalidOperationException("Should not resolve"));

        builder.UseWhen(
            _ => true,
            sub =>
            {
                sub.UseMiddleware(new FakeMiddleware("OuterSub", callLog));
                sub.UseWhen(
                    _ => true,
                    inner => inner.UseMiddleware(new FakeMiddleware("InnerSub", callLog)));
            });

        IReadOnlyList<IProcessMiddleware> pipeline = builder.Build();
        MiddlewareChain chain = new MiddlewareChain(pipeline, ctx =>
        {
            callLog.Add("terminal");
            return Task.CompletedTask;
        });

        InvocationContext ctx = CreateContext();
        await chain.RunAsync(ctx, CancellationToken.None);

        await Assert.That(callLog).IsEquivalentTo(new List<string> { "OuterSub", "InnerSub", "terminal" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task UseWhen_NestedSubPipelines_InnerPredicateFalse()
    {
        List<string> callLog = [];
        ProcessMiddlewareBuilder builder = new ProcessMiddlewareBuilder(_ => throw new InvalidOperationException("Should not resolve"));

        builder.UseWhen(
            _ => true,
            sub =>
            {
                sub.UseMiddleware(new FakeMiddleware("OuterSub", callLog));
                sub.UseWhen(
                    _ => false,
                    inner => inner.UseMiddleware(new FakeMiddleware("InnerSub", callLog)));
            });

        IReadOnlyList<IProcessMiddleware> pipeline = builder.Build();
        MiddlewareChain chain = new MiddlewareChain(pipeline, ctx =>
        {
            callLog.Add("terminal");
            return Task.CompletedTask;
        });

        InvocationContext ctx = CreateContext();
        await chain.RunAsync(ctx, CancellationToken.None);

        await Assert.That(callLog).IsEquivalentTo(new List<string> { "OuterSub", "terminal" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task UseWhen_ThrowsArgumentNullException_ForNullPredicate()
    {
        ProcessMiddlewareBuilder builder = new ProcessMiddlewareBuilder(_ => null!);

        await Assert.That(() => builder.UseWhen((Func<InvocationContext, bool>)null!, _ => { }))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task UseWhen_ThrowsArgumentNullException_ForNullConfiguration()
    {
        ProcessMiddlewareBuilder builder = new ProcessMiddlewareBuilder(_ => null!);

        await Assert.That(() => builder.UseWhen(_ => true, null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task UseWhen_ThrowsArgumentNullException_ForNullAsyncPredicate()
    {
        ProcessMiddlewareBuilder builder = new ProcessMiddlewareBuilder(_ => null!);

        await Assert.That(() => builder.UseWhen((Func<InvocationContext, Task<bool>>)null!, _ => { }))
            .Throws<ArgumentNullException>();
    }

    private static InvocationContext CreateContext()
    {
        return new InvocationContext(
            new ProcessConfigurationBuilder("test.exe").Build(),
            ProcessExitConfiguration.Default,
            InvocationMode.Raw);
    }
}
