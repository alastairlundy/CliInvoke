/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Linq;
using CliInvoke.Extensions.Middleware.Logging;

using Microsoft.Extensions.Logging;
using LoggingMiddleware = CliInvoke.Extensions.Middleware.Logging.LoggingMiddleware;

namespace CliInvoke.Tests.Middleware.Integration;

/// <summary>
///     Verifies that <see cref="LoggingMiddleware"/> logs a truthful argument string when the
///     caller populated <see cref="ProcessConfiguration.ArgumentList"/> instead of
///     <see cref="ProcessConfiguration.Arguments"/> (the shell-wrapped / ArgumentList path),
///     and that redaction still applies across the joined list.
/// </summary>
public class LoggingArgumentListTests
{
    /// <summary>
    ///     Runs <see cref="LoggingMiddleware"/> against the supplied configuration with a no-op
    ///     terminal delegate (no process is spawned, so only the entry log line is produced) and
    ///     returns the formatted content of that entry log record.
    /// </summary>
    /// <param name="configuration">The configuration under test.</param>
    /// <returns>The formatted entry log message.</returns>
    private static async Task<string> InvokeAndCaptureEntryAsync(ProcessConfiguration configuration)
    {
        CapturingLogger<LoggingMiddleware> logger = new();
        LoggingMiddleware middleware = new(logger);

        InvocationContext context = new(
            configuration,
            ProcessExitConfiguration.CreateGraceful(),
            InvocationMode.Buffered);

        await middleware.InvokeAsync(context, _ => Task.CompletedTask).ConfigureAwait(true);

        (LogLevel Level, string Message) entry = logger.Entries.First(e =>
            e.Level == LogLevel.Information && e.Message.Contains("with arguments"));

        return entry.Message;
    }

    [Test]
    public async Task InvokeAsync_LogsJoinedArgumentList_WhenArgumentsIsWhitespace()
    {
        ProcessConfiguration configuration = new("tool.exe", "   ")
        {
            ArgumentList = ["--flag", "value"],
        };

        string message = await InvokeAndCaptureEntryAsync(configuration).ConfigureAwait(true);

        await Assert.That(message.Contains("--flag value")).IsTrue();
    }

    [Test]
    public async Task InvokeAsync_LogsJoinedArgumentList_WhenArgumentsIsNull()
    {
        ProcessConfiguration configuration = new("tool.exe", string.Empty)
        {
            Arguments = null!,
            ArgumentList = ["--flag", "value"],
        };

        string message = await InvokeAndCaptureEntryAsync(configuration).ConfigureAwait(true);

        await Assert.That(message.Contains("--flag value")).IsTrue();
    }

    [Test]
    public async Task InvokeAsync_RedactsSensitiveValues_JoinedFromArgumentList()
    {
        ProcessConfiguration configuration = new("tool.exe", string.Empty)
        {
            ArgumentList = ["--password", "hunter2"],
        };

        string message = await InvokeAndCaptureEntryAsync(configuration).ConfigureAwait(true);

        await Assert.That(message.Contains("***")).IsTrue();
        await Assert.That(message.Contains("hunter2")).IsFalse();
    }

    [Test]
    public async Task InvokeAsync_LogsSanitizedArguments_WhenArgumentsIsNonEmpty()
    {
        ProcessConfiguration configuration = new("tool.exe", "--password hunter2")
        {
            ArgumentList = ["--ignored", "value"],
        };

        string message = await InvokeAndCaptureEntryAsync(configuration).ConfigureAwait(true);

        await Assert.That(message.Contains("--password ***")).IsTrue();
        await Assert.That(message.Contains("hunter2")).IsFalse();
        await Assert.That(message.Contains("--ignored")).IsFalse();
    }
}
