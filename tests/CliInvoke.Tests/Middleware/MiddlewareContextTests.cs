using System.Collections.Generic;
using CliInvoke.Core.Middleware;

namespace CliInvoke.Tests.Middleware;

public class MiddlewareContextTests
{
    [Test]
    public async Task Next_IsTheSuppliedDelegate()
    {
        bool called = false;
        Func<InvocationContext, CancellationToken, Task> nextDelegate = (ctx, ct) =>
        {
            called = true;
            return Task.CompletedTask;
        };

        MiddlewareContext context = new MiddlewareContext(nextDelegate, CancellationToken.None);

        await Assert.That(context.Next).IsEqualTo(nextDelegate);
    }

    [Test]
    public async Task CancellationToken_IsPreserved()
    {
        using CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        MiddlewareContext context = new MiddlewareContext(
            (ctx, ct) => Task.CompletedTask,
            cts.Token);

        await Assert.That(context.CancellationToken.IsCancellationRequested).IsEqualTo(true);
    }

    [Test]
    public async Task Items_IsSharedAcrossMiddlewareSteps()
    {
        MiddlewareItems items = new MiddlewareItems();
        items.Set("shared", "value1");

        MiddlewareContext firstContext = new MiddlewareContext(
            (ctx, ct) => Task.CompletedTask,
            CancellationToken.None,
            items);
        MiddlewareContext secondContext = new MiddlewareContext(
            (ctx, ct) => Task.CompletedTask,
            CancellationToken.None,
            items);

        firstContext.Items.Set("shared", "value2");

        await Assert.That(secondContext.Items.Get<string>("shared")).IsEqualTo("value2");
    }

    [Test]
    public async Task Items_IsInitializedForEachContext()
    {
        MiddlewareContext context1 = new MiddlewareContext(
            (ctx, ct) => Task.CompletedTask,
            CancellationToken.None);
        MiddlewareContext context2 = new MiddlewareContext(
            (ctx, ct) => Task.CompletedTask,
            CancellationToken.None);

        context1.Items.Set("key", "value1");

        await Assert.That(() => context2.Items.Get<string>("key"))
            .Throws<KeyNotFoundException>();
    }
}
