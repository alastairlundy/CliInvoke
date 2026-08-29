/*
    CliInvoke.Extensions.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Core.Factories;
using CliInvoke.Core.Middleware;
using CliInvoke.Core.Processes;
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
        => new ProcessResultValidator<ProcessResult>([_ => false]);

    private static IProcessResultValidator<ProcessResult> NeverRetry()
        => new ProcessResultValidator<ProcessResult>([_ => true]);

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

    private static IProcessResultValidator<ProcessResult> RequiresExitCodeZeroValidator()
        => new ProcessResultValidator<ProcessResult>(
            [CommonValidationRules<ProcessResult>.RequiresExitCodeZero]);

    [Test]
    public async Task InvokeAsync_DoesNotRetry_WhenDefaultValidatorResultSucceeds()
    {
        // The default policy uses RequiresExitCodeZero, so a zero-exit (validated) result must stop
        // after the first attempt and must not repeat process side effects.
        var options = new RetryOptions
        {
            MaxAttempts = 3,
            BaseDelay = TimeSpan.FromMilliseconds(1)
        };
        var middleware = new RetryMiddleware(RequiresExitCodeZeroValidator(), options);
        InvocationContext ctx = CreateContext();

        int attempts = 0;
        Func<InvocationContext, Task> next = c =>
        {
            attempts++;
            c.Result = new ProcessResult("dummy", 0, 1, DateTime.UtcNow, DateTime.UtcNow, false, null);
            return Task.CompletedTask;
        };

        await middleware.InvokeAsync(ctx, next);

        await Assert.That(attempts).IsEqualTo(1);
    }

    [Test]
    public async Task Constructor_Throws_WhenMaxAttemptsIsLessThanOne()
    {
        var options = new RetryOptions { MaxAttempts = 0 };

        await Assert.That(() => new RetryMiddleware(AlwaysRetry(), options))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Constructor_Throws_WhenBaseDelayIsNegative()
    {
        var options = new RetryOptions { BaseDelay = TimeSpan.FromMilliseconds(-1) };

        await Assert.That(() => new RetryMiddleware(AlwaysRetry(), options))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task InvokeAsync_PerformsExactlyOneAttempt_WhenMaxAttemptsIsOne()
    {
        var options = new RetryOptions
        {
            MaxAttempts = 1,
            BaseDelay = TimeSpan.FromMilliseconds(1)
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

        await Assert.That(attempts).IsEqualTo(1);
    }

    [Test]
    public async Task UseRetryPolicy_RegistersConfiguredInvoker()
    {
        IServiceCollection services = new ServiceCollection();
        // Register UseRetryPolicy() first, then replace the validator and process factory with stubs.
        // Registering after AddCliInvoke ensures our singleton registrations win over the ones it adds.
        services.AddCliInvoke(builder => builder.UseRetryPolicy());

        // A validator that always classifies the result as retryable, plus a stub process factory that
        // avoids spawning real processes. Executing through the resolved IProcessInvoker (not a
        // manually-built ProcessInvoker) proves the registration actually adds the middleware: if
        // UseRetryPolicy() stopped registering it, the stub would run once and the retry count would
        // not reach MaxAttempts.
        var validator = new CountingRetryValidator();
        var factory = new StubExternalProcessFactory();
        services.AddSingleton<IProcessResultValidator<ProcessResult>>(validator);
        services.AddSingleton<IExternalProcessFactory>(factory);

        IServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        IProcessInvoker? invoker = scope.ServiceProvider.GetService<IProcessInvoker>();

        // Preserve the original service-resolution assertions.
        await Assert.That(invoker).IsNotNull();
        await Assert.That(invoker).IsTypeOf<ProcessInvoker>();

        // The retry middleware is registered and active: the retryable result is attempted
        // MaxAttempts times rather than once, and the validator is consulted once per attempt.
        using ProcessConfiguration config = ProcessConfigurationFactory.Create("cmd.exe", "/C echo hi");
        await invoker.ExecuteBufferedAsync(config, ProcessExitConfiguration.CreateGraceful());

        await Assert.That(validator.Calls).IsEqualTo(RetryOptions.Default.MaxAttempts);
    }

    [Test]
    public async Task ComputeDelay_Linear_GrowsByBaseDelayPerAttempt()
    {
        var options = new RetryOptions
        {
            BaseDelay = TimeSpan.FromMilliseconds(10),
            Strategy = RetryBackoffStrategy.Linear
        };

        await Assert.That(RetryMiddleware.ComputeDelay(1, options).Ticks)
            .IsEqualTo(TimeSpan.FromMilliseconds(10).Ticks);
        await Assert.That(RetryMiddleware.ComputeDelay(2, options).Ticks)
            .IsEqualTo(TimeSpan.FromMilliseconds(20).Ticks);
        await Assert.That(RetryMiddleware.ComputeDelay(3, options).Ticks)
            .IsEqualTo(TimeSpan.FromMilliseconds(30).Ticks);
    }

    [Test]
    public async Task ComputeDelay_Exponential_GrowsByPowerOfTwo_ForValidSettings()
    {
        var options = new RetryOptions
        {
            BaseDelay = TimeSpan.FromMilliseconds(10),
            Strategy = RetryBackoffStrategy.Exponential
        };

        await Assert.That(RetryMiddleware.ComputeDelay(1, options).Ticks)
            .IsEqualTo(TimeSpan.FromMilliseconds(10).Ticks);
        await Assert.That(RetryMiddleware.ComputeDelay(2, options).Ticks)
            .IsEqualTo(TimeSpan.FromMilliseconds(20).Ticks);
        await Assert.That(RetryMiddleware.ComputeDelay(3, options).Ticks)
            .IsEqualTo(TimeSpan.FromMilliseconds(40).Ticks);
    }

    [Test]
    public async Task ComputeDelay_ClampsToTaskDelayMax_ForExponentialOverflow()
    {
        // base at the Task.Delay maximum: attempt 2 doubles it, which exceeds the limit, so it must be clamped.
        TimeSpan maxDelay = TimeSpan.FromMilliseconds(int.MaxValue);
        var options = new RetryOptions
        {
            BaseDelay = maxDelay,
            Strategy = RetryBackoffStrategy.Exponential,
            MaxAttempts = 3
        };

        await Assert.That(RetryMiddleware.ComputeDelay(1, options).Ticks).IsEqualTo(maxDelay.Ticks);
        await Assert.That(RetryMiddleware.ComputeDelay(2, options).Ticks).IsEqualTo(maxDelay.Ticks);
    }

    /// <summary>
    ///     A validator that always classifies the result as retryable (Validate returns false, so the
    ///     default <c>ShouldRetry => !Validate</c> returns true) and records how many times it is consulted.
    /// </summary>
    private sealed class CountingRetryValidator : IProcessResultValidator<ProcessResult>
    {
        public int Calls;

        public Func<ProcessResult, bool>[] ValidationRules => [_ => false];

        public bool Validate(ProcessResult result)
        {
            Calls++;
            return false;
        }

        public ValidationFailure<ProcessResult>[] GetValidationFailures(ProcessResult result) => [];
    }

    /// <summary>
    ///     A factory returning a <see cref="StubExternalProcess"/> so retry behaviour can be exercised
    ///     without starting a real process.
    /// </summary>
    private sealed class StubExternalProcessFactory : IExternalProcessFactory
    {
        public IExternalProcess CreateExternalProcess(ProcessConfiguration configuration)
            => new StubExternalProcess(configuration);

        public IExternalProcess CreateExternalProcess(
            ProcessConfiguration configuration,
            ProcessExitConfiguration exitConfiguration)
            => new StubExternalProcess(configuration, exitConfiguration);
    }

    /// <summary>
    ///     A no-op <see cref="IExternalProcess"/> that returns sentinel results, used to drive the retry loop.
    /// </summary>
    private sealed class StubExternalProcess : IExternalProcess
    {
        public StubExternalProcess(ProcessConfiguration configuration)
            : this(configuration, ProcessExitConfiguration.CreateGraceful())
        {
        }

        public StubExternalProcess(ProcessConfiguration configuration, ProcessExitConfiguration exitConfiguration)
        {
            Configuration = configuration;
            ExitConfiguration = exitConfiguration;
        }

        public ProcessConfiguration Configuration { get; init; }

        public ProcessExitConfiguration ExitConfiguration { get; }

        public bool HasExited => true;

        public bool HasStarted { get; private set; }

        public event EventHandler? Started;

        public event EventHandler? Exited;

        public int Start()
        {
            HasStarted = true;
            Started?.Invoke(this, EventArgs.Empty);
            return 0;
        }

        public Task StartAsync(CancellationToken cancellationToken)
            => StartAsync(Configuration, cancellationToken);

        public Task StartAsync(ProcessConfiguration configuration, CancellationToken cancellationToken)
        {
            HasStarted = true;
            Started?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public Task<ProcessResult> WaitForExitOrTimeoutAsync(CancellationToken cancellationToken)
        {
            DateTime now = DateTime.UtcNow;
            return Task.FromResult(new ProcessResult(
                Configuration.TargetFilePath, 0, 0, now, now, false, null));
        }

        public Task<BufferedProcessResult> CaptureBufferedResultAsync(
            CancellationToken cancellationToken,
            long? maxStandardOutputBytes = null,
            long? maxStandardErrorBytes = null)
        {
            DateTime now = DateTime.UtcNow;
            return Task.FromResult(new BufferedProcessResult(
                Configuration.TargetFilePath, 0, 0, string.Empty, string.Empty,
                now, now, false, null, false));
        }

        public Task<PipedProcessResult> CapturePipedResultAsync(CancellationToken cancellationToken)
        {
            DateTime now = DateTime.UtcNow;
            return Task.FromResult(new PipedProcessResult(
                Configuration.TargetFilePath, 0, 0, now, now, Stream.Null, Stream.Null, false, null));
        }

        public Task Kill() => Task.CompletedTask;

        public void Dispose()
        {
        }
    }
}
