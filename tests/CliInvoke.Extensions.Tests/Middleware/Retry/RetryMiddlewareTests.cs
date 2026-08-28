/*
    CliInvoke.Extensions.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Core.Middleware;
using CliInvoke.Core.Validation;
using CliInvoke.Extensions.Middleware.Retry;
using CliInvoke.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace CliInvoke.Extensions.Tests.Middleware.Retry;

/// <summary>
///     Tests for <see cref="RetryMiddleware"/> and the <c>UseRetryPolicy</c> registration extension.
/// </summary>
public class RetryMiddlewareTests
{
    private static InvocationContext CreateContext()
    {
        ProcessConfiguration config = ProcessConfigurationFactory.Create("cmd.exe", "/C echo hi");
        return new InvocationContext(config, ProcessExitConfiguration.CreateGraceful(), InvocationMode.Buffered,
            CancellationToken.None);
    }

    private static IProcessResultValidator<ProcessResult> AlwaysRetry()
        => new ProcessResultValidator<ProcessResult>([_ => true]);

    private static IProcessResultValidator<ProcessResult> NeverRetry()
        => new ProcessResultValidator<ProcessResult>([_ => false]);

    private static ProcessResult MakeResult()
        => new("dummy", 1, 1, DateTime.UtcNow, DateTime.UtcNow, false, null);

    [Test]
    public async Task InvokeAsync_RetriesUntilMaxAttempts_WhenAlwaysRetryable()
    {
        var options = new RetryOptions
        {
            MaxAttempts = 3,
            BaseDelay = TimeSpan.FromMilliseconds(1),
            Strategy = RetryBackoffStrategy.Fixed
        };
        var middleware = new RetryMiddleware(AlwaysRetry(), options);
        InvocationContext ctx = CreateContext();

        int attempts = 0;
        Func<InvocationContext, Task> next = c =>
        {
            attempts++;
            c.Result = MakeResult();
            return Task.CompletedTask;
        };

        await middleware.InvokeAsync(ctx, next);

        await Assert.That(attempts).IsEqualTo(3);
    }

    [Test]
    public async Task InvokeAsync_DoesNotRetry_WhenNotRetryable()
    {
        var options = new RetryOptions
        {
            MaxAttempts = 3,
            BaseDelay = TimeSpan.FromMilliseconds(1)
        };
        var middleware = new RetryMiddleware(NeverRetry(), options);
        InvocationContext ctx = CreateContext();

        int attempts = 0;
        Func<InvocationContext, Task> next = c =>
        {
            attempts++;
            c.Result = MakeResult();
            return Task.CompletedTask;
        };

        await middleware.InvokeAsync(ctx, next);

        await Assert.That(attempts).IsEqualTo(1);
    }

    [Test]
    public async Task InvokeAsync_StopsWhenResultIsNull()
    {
        var options = new RetryOptions
        {
            MaxAttempts = 3,
            BaseDelay = TimeSpan.FromMilliseconds(1)
        };
        var middleware = new RetryMiddleware(AlwaysRetry(), options);
        InvocationContext ctx = CreateContext();

        int attempts = 0;
        Func<InvocationContext, Task> next = c =>
        {
            attempts++;
            c.Result = null;
            return Task.CompletedTask;
        };

        await middleware.InvokeAsync(ctx, next);

        await Assert.That(attempts).IsEqualTo(1);
    }

    [Test]
    public async Task UseRetryPolicy_RegistersConfiguredInvoker()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddCliInvoke(builder => builder.UseRetryPolicy());
        IServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        IProcessInvoker? invoker = scope.ServiceProvider.GetService<IProcessInvoker>();

        await Assert.That(invoker).IsNotNull();
        await Assert.That(invoker).IsTypeOf<ProcessInvoker>();
    }
}
