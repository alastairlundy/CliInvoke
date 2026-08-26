/*
    CliInvoke.Extensions
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using CliInvoke.Core.Middleware;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CliInvoke.Extensions.Middleware;

/// <summary>
///     Middleware that logs process invocation entry and exit information.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="ILogger"/> is resolved via constructor injection from
///         the dependency injection container. When no logger is registered,
///         <see cref="NullLogger{T}.Instance"/> is used as a no-op fallback.
///     </para>
///     <para>
///     Sensitive argument values following the flags <c>--password</c>,
///         <c>--token</c>, and <c>--api-key</c> are automatically
///         redacted to <c>***</c> in log output.
///     </para>
/// </remarks>
internal sealed partial class LoggingMiddleware : IProcessMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;

    [GeneratedRegex(@"(--password|--token|--api-key)(\s*=\s*|\s+)(?:""[^""]*""|'[^']*'|[^\s-][^\s]*)",
        RegexOptions.IgnoreCase)]
    private static partial Regex SensitiveArgPattern();

    /// <summary>
    ///     Initialises a new instance of the <see cref="LoggingMiddleware"/> class.
    /// </summary>
    /// <param name="logger">
    ///     The logger used to write process invocation entry and exit information.
    ///     Resolved from the dependency injection container.
    /// </param>
    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger ?? NullLogger<LoggingMiddleware>.Instance;
    }

    /// <summary>
    ///     Executes the middleware pipeline, logging process entry and exit details.
    /// </summary>
    /// <param name="context">The current invocation context.</param>
    /// <param name="next">The delegate to invoke the next middleware or the terminal pipeline.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(InvocationContext context, Func<InvocationContext, Task> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        string sanitizedArgs = SanitizeArguments(context.Configuration.Arguments);

        _logger.LogInformation("Invoking process {TargetFilePath} with arguments {Arguments}",
            context.Configuration.TargetFilePath,
            sanitizedArgs);

        await next(context);

        if (context.Result is null)
            return;

        _logger.LogInformation("Process {TargetFilePath} exited with code {ExitCode}",
            context.Configuration.TargetFilePath,
            context.Result.ExitCode);

        // Stdout/stderr Debug logging is only available for BufferedProcessResult,
        // where the output is captured as strings. For Raw and Piped modes the
        // streams are not read here — use BufferedProcessResult to see per-line logs.
        if (context.Result is BufferedProcessResult bufferedResult)
        {
            if (!string.IsNullOrEmpty(bufferedResult.StandardOutput))
            {
                foreach (string line in bufferedResult.StandardOutput.Split(
                             Environment.NewLine,
                             StringSplitOptions.RemoveEmptyEntries))
                {
                    _logger.LogDebug("STDOUT: {Line}", line);
                }
            }

            if (!string.IsNullOrEmpty(bufferedResult.StandardError))
            {
                foreach (string line in bufferedResult.StandardError.Split(
                             Environment.NewLine,
                             StringSplitOptions.RemoveEmptyEntries))
                {
                    _logger.LogDebug("STDERR: {Line}", line);
                }
            }
        }
    }

    /// <summary>
    ///     Redacts sensitive values from the argument string.
    /// </summary>
    /// <remarks>
    ///     Redacts values following <c>--password</c>, <c>--token</c>,
    ///     and <c>--api-key</c> flags. Both space-separated
    ///     (<c>--password secret</c>) and equals-form (<c>--token=secret</c>) are handled,
    ///     as are single- or double-quoted values that contain spaces
    ///     (e.g. <c>--password "my secret"</c>).
    /// </remarks>
    /// <param name="args">The raw argument string.</param>
    /// <returns>The argument string with sensitive values replaced by <c>***</c>.</returns>
    private static string SanitizeArguments(string? args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return string.Empty;

        return SensitiveArgPattern().Replace(args, "$1$2***");
    }
}
