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
    private readonly Func<string?, string>? _redactor;

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
    /// <param name="redactor">
    ///     An optional redactor applied to argument strings and captured stdout/stderr lines
    ///     before they are logged. When omitted, a built-in pattern redacts the values following
    ///     the <c>--password</c>, <c>--token</c>, and <c>--api-key</c> flags. Consumers can supply
    ///     Microsoft's <c>Microsoft.Extensions.Compliance.Redaction</c> redactor here to apply the
    ///     standard, centrally-maintained secret taxonomy instead of this heuristic.
    /// </param>
    public LoggingMiddleware(ILogger<LoggingMiddleware> logger, Func<string?, string>? redactor = null)
    {
        _logger = logger ?? NullLogger<LoggingMiddleware>.Instance;
        _redactor = redactor;
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

        string sanitizedArgs = Sanitize(context.Configuration.Arguments);

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
        // where the output is captured as strings. For Raw mode the
        // streams are not read here — use BufferedProcessResult to see per-line logs.
        if (context.Result is BufferedProcessResult bufferedResult)
        {
            if (!string.IsNullOrEmpty(bufferedResult.StandardOutput))
            {
                foreach (string line in bufferedResult.StandardOutput.Split(
                             Environment.NewLine,
                             StringSplitOptions.RemoveEmptyEntries))
                {
                    _logger.LogDebug("STDOUT: {Line}", Sanitize(line));
                }
            }

            if (!string.IsNullOrEmpty(bufferedResult.StandardError))
            {
                foreach (string line in bufferedResult.StandardError.Split(
                             Environment.NewLine,
                             StringSplitOptions.RemoveEmptyEntries))
                {
                    _logger.LogDebug("STDERR: {Line}", Sanitize(line));
                }
            }
        }
    }

    /// <summary>
    ///     Redacts sensitive values from the supplied string before it is logged.
    /// </summary>
    /// <remarks>
    ///     When a custom <see cref="_redactor"/> was supplied it is applied to the whole value.
    ///     Otherwise a built-in heuristic redacts the value following the <c>--password</c>,
    ///     <c>--token</c>, and <c>--api-key</c> flags (both <c>--flag value</c> and
    ///     <c>--flag=value</c> forms, including single-/double-quoted values).
    ///     <para>
    ///         Values carrying no recognizable signal cannot be auto-redacted; callers handling
    ///         such secrets should supply a redactor (e.g. Microsoft's
    ///         <c>Microsoft.Extensions.Compliance.Redaction</c>).
    ///     </para>
    /// </remarks>
    /// <param name="value">The raw string (argument string or captured output line).</param>
    /// <returns>The string with sensitive values replaced by <c>***</c>.</returns>
    private string Sanitize(string? value)
    {
        if (value is null)
            return string.Empty;

        if (_redactor is not null)
            return _redactor(value);

        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return SensitiveArgPattern().Replace(value, "$1$2***");
    }
}
