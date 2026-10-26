/*
    CliInvoke.Extensions.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Text;

using CliInvoke.Core.Middleware;
using CliInvoke.Extensions.Middleware.Retry;

namespace CliInvoke.Tests.Extensions.Middleware.Retry;

/// <summary>
///     Tests for the standard-input replay contract of <see cref="RetryMiddleware"/>: a seekable
///     source is rewound to the beginning before every retry attempt so attempts 2 and later
///     replay the payload instead of an empty stream, a non-seekable source fails loudly at the
///     moment a retry would occur, and the no-retry paths never throw.
/// </summary>
public class RetryStdinReplayTests
{
    /// <summary>
    ///     The exact message <see cref="RetryMiddleware"/> throws for a non-seekable stdin source
    ///     when a retry attempt would need to replay it.
    /// </summary>
    private const string NonSeekableRetryMessage =
        "retrying an invocation with a non-seekable standard input stream cannot replay the input";

    [Test]
    public async Task InvokeAsync_SeekableMemoryStream_ResetsPositionBeforeSecondAttempt()
    {
        byte[] payload = Encoding.UTF8.GetBytes("stdin payload");
        using MemoryStream stdin = new MemoryStream(payload);
        RetryMiddleware middleware = new RetryMiddleware(new AlwaysRetry(), CreateOptions(2));
        InvocationContext context = CreateContext(CreateConfiguration(stdin));

        List<long> positionsAtAttemptStart = new List<long>();
        int attempts = 0;
        Func<InvocationContext, Task> next = c =>
        {
            attempts++;
            positionsAtAttemptStart.Add(c.Configuration.StandardInput!.BaseStream.Position);
            ConsumeStandardInput(c);
            c.Result = MakeResult();
            return Task.CompletedTask;
        };

        await middleware.InvokeAsync(context, next).ConfigureAwait(true);

        // Attempt 1 consumed the payload to the end; the middleware must rewind it before
        // attempt 2, so both attempts observe position 0 when they start.
        await Assert.That(attempts).IsEqualTo(2);
        await Assert.That(positionsAtAttemptStart.Count).IsEqualTo(2);
        await Assert.That(positionsAtAttemptStart[0]).IsEqualTo(0L);
        await Assert.That(positionsAtAttemptStart[1]).IsEqualTo(0L);
        await Assert.That(context.Result).IsNotNull();
        await Assert.That(context.Result!.ExitCode).IsEqualTo(1);
    }

    [Test]
    public async Task InvokeAsync_SeekableFileStream_ResetsPositionBeforeSecondAttempt()
    {
        string path = Path.Combine(Path.GetTempPath(),
            $"cliinvoke-retry-stdin-replay-{Guid.NewGuid():N}.tmp");
        File.WriteAllBytes(path, Encoding.UTF8.GetBytes("file stdin payload"));

        try
        {
            using FileStream stdin = new FileStream(
                path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            RetryMiddleware middleware = new RetryMiddleware(new AlwaysRetry(), CreateOptions(2));
            InvocationContext context = CreateContext(CreateConfiguration(stdin));

            List<long> positionsAtAttemptStart = new List<long>();
            int attempts = 0;
            Func<InvocationContext, Task> next = c =>
            {
                attempts++;
                positionsAtAttemptStart.Add(c.Configuration.StandardInput!.BaseStream.Position);
                ConsumeStandardInput(c);
                c.Result = MakeResult();
                return Task.CompletedTask;
            };

            await middleware.InvokeAsync(context, next).ConfigureAwait(true);

            await Assert.That(attempts).IsEqualTo(2);
            await Assert.That(positionsAtAttemptStart.Count).IsEqualTo(2);
            await Assert.That(positionsAtAttemptStart[0]).IsEqualTo(0L);
            await Assert.That(positionsAtAttemptStart[1]).IsEqualTo(0L);
            await Assert.That(context.Result).IsNotNull();
            await Assert.That(context.Result!.ExitCode).IsEqualTo(1);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task InvokeAsync_NonSeekableStdin_ThrowsWhenRetryWouldOccur()
    {
        InjectableStdinSourceStream source = new("stdin payload");
        RetryMiddleware middleware = new RetryMiddleware(new AlwaysRetry(), CreateOptions(3));
        InvocationContext context = CreateContext(CreateConfiguration(source));

        int attempts = 0;
        Func<InvocationContext, Task> next = c =>
        {
            attempts++;
            ConsumeStandardInput(c);
            c.Result = MakeResult();
            return Task.CompletedTask;
        };

        InvalidOperationException? thrown = null;
        try
        {
            await middleware.InvokeAsync(context, next).ConfigureAwait(true);
        }
        catch (InvalidOperationException exception)
        {
            thrown = exception;
        }

        // The first attempt runs; the throw lands exactly where attempt 2 would start.
        await Assert.That(thrown).IsNotNull();
        await Assert.That(thrown!.Message).IsEqualTo(NonSeekableRetryMessage);
        await Assert.That(attempts).IsEqualTo(1);
    }

    [Test]
    public async Task InvokeAsync_NonSeekableStdin_DoesNotThrow_WhenResultIsNotRetryable()
    {
        InjectableStdinSourceStream source = new("stdin payload");
        RetryMiddleware middleware = new RetryMiddleware(new NeverRetry(), CreateOptions(3));
        InvocationContext context = CreateContext(CreateConfiguration(source));

        int attempts = 0;
        Func<InvocationContext, Task> next = c =>
        {
            attempts++;
            ConsumeStandardInput(c);
            c.Result = MakeResult();
            return Task.CompletedTask;
        };

        await middleware.InvokeAsync(context, next).ConfigureAwait(true);

        await Assert.That(attempts).IsEqualTo(1);
        await Assert.That(context.Result).IsNotNull();
    }

    [Test]
    public async Task InvokeAsync_NonSeekableStdin_DoesNotThrow_WhenMaxAttemptsIsOne()
    {
        InjectableStdinSourceStream source = new("stdin payload");
        RetryMiddleware middleware = new RetryMiddleware(new AlwaysRetry(), CreateOptions(1));
        InvocationContext context = CreateContext(CreateConfiguration(source));

        int attempts = 0;
        Func<InvocationContext, Task> next = c =>
        {
            attempts++;
            ConsumeStandardInput(c);
            c.Result = MakeResult();
            return Task.CompletedTask;
        };

        await middleware.InvokeAsync(context, next).ConfigureAwait(true);

        await Assert.That(attempts).IsEqualTo(1);
        await Assert.That(context.Result).IsNotNull();
    }

    [Test]
    public async Task InvokeAsync_NullStandardInput_RetriesWithoutThrowing()
    {
        ProcessConfiguration configuration = new ProcessConfiguration("dotnet", "--version")
        {
            StandardInput = null
        };
        RetryMiddleware middleware = new RetryMiddleware(new AlwaysRetry(), CreateOptions(2));
        InvocationContext context = CreateContext(configuration);

        int attempts = 0;
        Func<InvocationContext, Task> next = c =>
        {
            attempts++;
            c.Result = MakeResult();
            return Task.CompletedTask;
        };

        await middleware.InvokeAsync(context, next).ConfigureAwait(true);

        await Assert.That(attempts).IsEqualTo(2);
        await Assert.That(context.Result).IsNotNull();
    }

    private static ProcessConfiguration CreateConfiguration(Stream source)
        => new("dotnet", "--version")
        {
            RedirectStandardInput = true,
            StandardInput = new StreamWriter(source)
        };

    private static InvocationContext CreateContext(ProcessConfiguration configuration)
        => new(configuration, ProcessExitConfiguration.CreateGraceful(), InvocationMode.Buffered,
            CancellationToken.None);

    private static RetryOptions CreateOptions(int maxAttempts)
        => new RetryOptions
        {
            MaxAttempts = maxAttempts,
            BaseDelay = TimeSpan.FromMilliseconds(1),
            Strategy = RetryBackoffStrategy.Fixed
        };

    private static ProcessResult MakeResult()
        => new("dummy", 1, 1, DateTime.UtcNow, DateTime.UtcNow, false, null);

    // Drains the source to end-of-stream, modelling the copy PipeStandardInputAsync performs
    // during an attempt (a no-op past end-of-stream, so the read loop always terminates).
    private static void ConsumeStandardInput(InvocationContext context)
    {
        Stream source = context.Configuration.StandardInput!.BaseStream;
        byte[] buffer = new byte[64];

        while (source.Read(buffer, 0, buffer.Length) > 0)
        {
        }
    }

    /// <summary>
    ///     A retry classifier that always returns <c>true</c> (retry).
    /// </summary>
    private sealed class AlwaysRetry : IRetryClassifier
    {
        public bool ShouldRetry(ProcessResult result) => true;
    }

    /// <summary>
    ///     A retry classifier that always returns <c>false</c> (no retry).
    /// </summary>
    private sealed class NeverRetry : IRetryClassifier
    {
        public bool ShouldRetry(ProcessResult result) => false;
    }
}
