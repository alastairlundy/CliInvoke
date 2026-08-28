/*
    CliInvoke.Extensions.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Core.Middleware;
using CliInvoke.Extensions.Middleware.Truncation;

namespace CliInvoke.Extensions.Tests.Middleware.Truncation;

/// <summary>
///     Tests for <see cref="OutputTruncationMiddleware"/> and the
///     <see cref="OutputTruncationMiddlewareExtensions.UseOutputTruncation"/> registration extension.
/// </summary>
public class OutputTruncationMiddlewareTests
{
    private static InvocationContext CreateContext(MiddlewareItems items)
    {
        ProcessConfiguration config = ProcessConfigurationFactory.Create("cmd.exe", "/C echo hi");
        var ctx = new InvocationContext(config, ProcessExitConfiguration.CreateGraceful(), InvocationMode.Buffered,
            CancellationToken.None);
        ctx.Middleware = new MiddlewareContext(_ => Task.CompletedTask, CancellationToken.None, items);
        return ctx;
    }

    [Test]
    public async Task InvokeAsync_WritesDefaultCap_ToMiddlewareItems()
    {
        var items = new MiddlewareItems();
        InvocationContext ctx = CreateContext(items);
        var middleware = new OutputTruncationMiddleware(TruncationOptions.Default);

        await middleware.InvokeAsync(ctx, c => Task.CompletedTask);

        await Assert.That(items.Get<long>(TruncationDefaults.MaxBytesPerStreamKey)).IsEqualTo(1_048_576);
    }

    [Test]
    public async Task InvokeAsync_WritesConfiguredCap_ToMiddlewareItems()
    {
        var items = new MiddlewareItems();
        InvocationContext ctx = CreateContext(items);
        var middleware = new OutputTruncationMiddleware(new TruncationOptions { MaxSize = 2048 });

        await middleware.InvokeAsync(ctx, c => Task.CompletedTask);

        await Assert.That(items.Get<long>(TruncationDefaults.MaxBytesPerStreamKey)).IsEqualTo(2048);
    }

    [Test]
    public async Task InvokeAsync_InvokesNext()
    {
        var items = new MiddlewareItems();
        InvocationContext ctx = CreateContext(items);
        var middleware = new OutputTruncationMiddleware(TruncationOptions.Default);
        bool nextCalled = false;

        await middleware.InvokeAsync(ctx, c =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await Assert.That(nextCalled).IsTrue();
    }

    [Test]
    public async Task UseOutputTruncation_RegistersMiddlewareInPipeline()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddCliInvoke(builder => builder.UseOutputTruncation());
        IServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        IProcessInvoker? invoker = scope.ServiceProvider.GetService<IProcessInvoker>();

        await Assert.That(invoker).IsNotNull();
        await Assert.That(invoker).IsTypeOf<ProcessInvoker>();
    }
}
