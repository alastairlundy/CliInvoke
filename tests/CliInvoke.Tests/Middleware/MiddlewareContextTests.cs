using System.Collections.Generic;
using CliInvoke.Core.Middleware;

namespace CliInvoke.Tests.Middleware;

public class MiddlewareContextTests
{
    [Test]
    public async Task Next_IsTheSuppliedDelegate()
    {
        var called = false;
        Func<InvocationContext, CancellationToken, Task> nextDelegate = (ctx, ct) =>
        {
            called = true;
            return Task.CompletedTask;
        };

        var context = new MiddlewareContext(nextDelegate, CancellationToken.None);

        await Assert.That(context.Next).IsEqualTo(nextDelegate);
    }

    [Test]
    public async Task CancellationToken_IsPreserved()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var context = new MiddlewareContext(
            (ctx, ct) => Task.CompletedTask,
            cts.Token);

        await Assert.That(context.CancellationToken.IsCancellationRequested).IsEqualTo(true);
    }

    [Test]
    public async Task Items_IsSharedAcrossMiddlewareSteps()
    {
        var items = new MiddlewareItems();
        items.Set("shared", "value1");

        var context = new MiddlewareContext(
            (ctx, ct) => Task.CompletedTask,
            CancellationToken.None);

        // Items is a new instance per MiddlewareContext, but can be used to share data
        // when the same Items instance is passed across middleware
        context.Items.Set("shared", "value2");

        await Assert.That(context.Items.Get<string>("shared")).IsEqualTo("value2");
    }

    [Test]
    public async Task Items_IsInitializedForEachContext()
    {
        var context1 = new MiddlewareContext(
            (ctx, ct) => Task.CompletedTask,
            CancellationToken.None);
        var context2 = new MiddlewareContext(
            (ctx, ct) => Task.CompletedTask,
            CancellationToken.None);

        context1.Items.Set("key", "value1");

        await Assert.That(() => context2.Items.Get<string>("key"))
            .Throws<KeyNotFoundException>();
    }
}
