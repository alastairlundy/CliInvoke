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
using FsCheck;
using FsCheck.Fluent;

namespace CliInvoke.Extensions.Tests.Middleware.Retry;

/// <summary>
///     Property-based fuzz tests for <see cref="RetryMiddleware"/> dispatch: how many times the inner
///     pipeline is invoked for retryable, non-retryable, and rejected-attempt-count cases.
/// </summary>
public class RetryDispatchFuzzTests
{
    private sealed class AlwaysRetry : IRetryClassifier
    {
        public bool ShouldRetry(ProcessResult result) => true;
    }

    private sealed class NeverRetry : IRetryClassifier
    {
        public bool ShouldRetry(ProcessResult result) => false;
    }

    // Retries iff the result's exit code is non-zero, so a generated exit code drives the iff-property.
    private sealed class ExitCodeNonZeroRetry : IRetryClassifier
    {
        public bool ShouldRetry(ProcessResult result) => result.ExitCode != 0;
    }

    [Test]
    public void RetryMiddleware_RetriesFailedResult_IffClassifierReturnsTrue()
    {
        // FsCheck 3.4 exposes a Func<T, Task<bool>> ForAll overload, so the middleware is awaited
        // directly inside the property. BaseDelay is 1ms + Fixed so the property stays fast.
        Prop.ForAll<int, int>(async (maxAttempts, exitCode) =>
            {
                if (maxAttempts < 1 || maxAttempts > 8) return true;

                RetryOptions options = new RetryOptions
                {
                    MaxAttempts = maxAttempts,
                    BaseDelay = TimeSpan.FromMilliseconds(1),
                    Strategy = RetryBackoffStrategy.Fixed
                };

                RetryMiddleware middleware = new RetryMiddleware(new ExitCodeNonZeroRetry(), options);

                InvocationContext context = new InvocationContext(
                    ProcessConfigurationFactory.Create("dotnet", "--version"),
                    ProcessExitConfiguration.CreateGraceful(),
                    InvocationMode.Buffered,
                    CancellationToken.None);

                int attempts = 0;
                Func<InvocationContext, Task> next = c =>
                {
                    attempts++;
                    c.Result = new ProcessResult("dummy", exitCode, 1, DateTime.UtcNow, DateTime.UtcNow, false, null);
                    return Task.CompletedTask;
                };

                await middleware.InvokeAsync(context, next);

                return attempts == (exitCode != 0 ? maxAttempts : 1);
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void RetryMiddleware_NeverRetry_DispatchesOnce()
    {
        Prop.ForAll<int>(async maxAttempts =>
            {
                if (maxAttempts < 1 || maxAttempts > 8) return true;

                RetryOptions options = new RetryOptions
                {
                    MaxAttempts = maxAttempts,
                    BaseDelay = TimeSpan.FromMilliseconds(1),
                    Strategy = RetryBackoffStrategy.Fixed
                };

                RetryMiddleware middleware = new RetryMiddleware(new NeverRetry(), options);

                InvocationContext context = new InvocationContext(
                    ProcessConfigurationFactory.Create("dotnet", "--version"),
                    ProcessExitConfiguration.CreateGraceful(),
                    InvocationMode.Buffered,
                    CancellationToken.None);

                int attempts = 0;
                Func<InvocationContext, Task> next = c =>
                {
                    attempts++;
                    c.Result = new ProcessResult("dummy", 1, 1, DateTime.UtcNow, DateTime.UtcNow, false, null);
                    return Task.CompletedTask;
                };

                await middleware.InvokeAsync(context, next);

                return attempts == 1;
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void RetryMiddleware_MaxAttemptsBelowOne_ThrowsArgumentOutOfRangeException()
    {
        Prop.ForAll<int>(maxAttempts =>
            {
                if (maxAttempts >= 1) return true;

                try
                {
                    _ = new RetryMiddleware(new AlwaysRetry(), new RetryOptions { MaxAttempts = maxAttempts });
                    return false;
                }
                catch (ArgumentOutOfRangeException)
                {
                    return true;
                }
            })
            .QuickCheckThrowOnFailure();
    }
}
