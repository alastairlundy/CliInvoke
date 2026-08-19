using System.Collections.Generic;
using CliInvoke.Core.Middleware;
using TUnit.Assertions.Enums;

namespace CliInvoke.Tests.Middleware;

public class ConditionalMiddlewareTests
{
    [Test]
    public async Task InvokeAsync_PredicateTrue_RunsSubPipeline()
    {
        List<string> callLog = [];
        FakeMiddleware subMiddleware = new FakeMiddleware("SubA", callLog);
        ConditionalMiddleware conditional = new ConditionalMiddleware(
            _ => Task.FromResult(true),
            new List<IProcessMiddleware> { subMiddleware });

        Func<InvocationContext, Task> outerNext = ctx =>
        {
            callLog.Add("outerNext");
            return Task.CompletedTask;
        };

        InvocationContext ctx = CreateContext();
        await conditional.InvokeAsync(ctx, outerNext);

        await Assert.That(callLog).IsEquivalentTo(new List<string> { "SubA", "outerNext" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task InvokeAsync_PredicateFalse_SkipsToOuterNext()
    {
        List<string> callLog = [];
        FakeMiddleware subMiddleware = new FakeMiddleware("SubA", callLog);
        ConditionalMiddleware conditional = new ConditionalMiddleware(
            _ => Task.FromResult(false),
            new List<IProcessMiddleware> { subMiddleware });

        Func<InvocationContext, Task> outerNext = ctx =>
        {
            callLog.Add("outerNext");
            return Task.CompletedTask;
        };

        InvocationContext ctx = CreateContext();
        await conditional.InvokeAsync(ctx, outerNext);

        await Assert.That(callLog).IsEquivalentTo(new List<string> { "outerNext" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task InvokeAsync_PredicateTrue_DoesNotCallOuterNext()
    {
        List<string> callLog = [];
        Func<InvocationContext, Task> outerNext = ctx =>
        {
            callLog.Add("outerNext");
            return Task.CompletedTask;
        };

        FakeMiddleware blockingMiddleware = new FakeMiddleware("SubA", callLog, FakeMiddlewareMode.NeverInvokeNext);
        ConditionalMiddleware conditional = new ConditionalMiddleware(
            _ => Task.FromResult(true),
            new List<IProcessMiddleware> { blockingMiddleware });

        InvocationContext ctx = CreateContext();
        await conditional.InvokeAsync(ctx, outerNext);

        await Assert.That(callLog).IsEquivalentTo(new List<string> { "SubA" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task InvokeAsync_MultipleSubMiddleware_ExecutesInOrder()
    {
        List<string> callLog = [];
        FakeMiddleware subA = new FakeMiddleware("SubA", callLog);
        FakeMiddleware subB = new FakeMiddleware("SubB", callLog);
        ConditionalMiddleware conditional = new ConditionalMiddleware(
            _ => Task.FromResult(true),
            new List<IProcessMiddleware> { subA, subB });

        Func<InvocationContext, Task> outerNext = ctx =>
        {
            callLog.Add("outerNext");
            return Task.CompletedTask;
        };

        InvocationContext ctx = CreateContext();
        await conditional.InvokeAsync(ctx, outerNext);

        await Assert.That(callLog).IsEquivalentTo(new List<string> { "SubA", "SubB", "outerNext" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task InvokeAsync_SyncPredicate_WorksCorrectly()
    {
        List<string> callLog = [];
        FakeMiddleware subMiddleware = new FakeMiddleware("SubA", callLog);
        ConditionalMiddleware conditional = new ConditionalMiddleware(
            ctx => Task.FromResult(true),
            new List<IProcessMiddleware> { subMiddleware });

        Func<InvocationContext, Task> outerNext = ctx =>
        {
            callLog.Add("outerNext");
            return Task.CompletedTask;
        };

        InvocationContext ctx = CreateContext();
        await conditional.InvokeAsync(ctx, outerNext);

        await Assert.That(callLog).IsEquivalentTo(new List<string> { "SubA", "outerNext" }, CollectionOrdering.Matching);
    }

    private static InvocationContext CreateContext()
    {
        return new InvocationContext(
            new ProcessConfigurationBuilder("test.exe").Build(),
            ProcessExitConfiguration.Default,
            InvocationMode.Raw);
    }
}
