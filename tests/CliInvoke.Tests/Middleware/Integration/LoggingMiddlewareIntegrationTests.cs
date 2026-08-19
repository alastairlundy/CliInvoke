/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CliInvoke.Core.Middleware;
using CliInvoke.Extensions.Middleware;
using CliInvoke.Factories;

using Microsoft.Extensions.Logging;

namespace CliInvoke.Tests.Middleware.Integration;

/// <summary>
///     A minimal <see cref="ILogger"/> implementation that captures log entries for assertions.
/// </summary>
internal sealed class CapturingLogger : ILogger
{
    private readonly object _lock = new();
    private readonly List<(LogLevel Level, string Message)> _entries = [];

    public IReadOnlyList<(LogLevel Level, string Message)> Entries
    {
        get { lock (_lock) { return _entries.ToArray(); } }
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
        => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        lock (_lock)
        {
            _entries.Add((logLevel, formatter(state, exception)));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}

public class LoggingMiddlewareIntegrationTests
{
    private static (string FilePath, string Arguments) ResolveEchoCommand()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return ("cmd.exe", "/C echo Hello from CliInvoke");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            return ("/bin/echo", "Hello from CliInvoke");

        throw new PlatformNotSupportedException("Unsupported OS for the echo integration test.");
    }

    [Test]
    public async Task UseLogging_CapturesEntryAndExitLogs_ForRealInvocation()
    {
        CapturingLogger logger = new CapturingLogger();

        MiddlewareItems items = new MiddlewareItems();
        items.Set(LoggingMiddleware.LoggerKey, (ILogger)logger);

        IReadOnlyList<IProcessMiddleware> middlewares = [new LoggingMiddleware()];
        ProcessInvoker invoker = new ProcessInvoker(new ExternalProcessFactory(), middlewares, items);

        (string filePath, string arguments) = ResolveEchoCommand();

        using ProcessConfiguration config = ProcessConfigurationFactory.Create(filePath, arguments);

        BufferedProcessResult result = await invoker.ExecuteBufferedAsync(
            config,
            ProcessExitConfiguration.CreateGraceful());

        await Assert.That(result.ExitCode).IsEqualTo(0);

        // Entry and exit are logged at Information; stdout lines at Debug.
        bool hasEntryLog = logger.Entries.Any(e =>
            e.Level == LogLevel.Information && e.Message.Contains(filePath));
        bool hasExitLog = logger.Entries.Any(e =>
            e.Level == LogLevel.Information && e.Message.Contains("exited with code"));
        bool hasStdoutLog = logger.Entries.Any(e =>
            e.Level == LogLevel.Debug && e.Message.Contains("Hello from CliInvoke"));

        await Assert.That(hasEntryLog).IsTrue();
        await Assert.That(hasExitLog).IsTrue();
        await Assert.That(hasStdoutLog).IsTrue();
    }
}
