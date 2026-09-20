/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Core.Exceptions;
using CliInvoke.Core.Validation;

namespace CliInvoke.Core;

/// <summary>
/// Provides extension methods for working with process results.
/// </summary>
public static class ProcessResultHelperExtensions
{
    extension<TProcessResult>(TProcessResult processResult)
        where TProcessResult : ProcessResult
    {
        /// <summary>
        /// Returns <c>true</c> when <see cref="ProcessResult.ExitCode" /> equals zero.
        /// </summary>
        /// <remarks>
        /// This is a literal exit-code check with no <c>Canceled</c> clause.
        /// For a richer caller-stated success policy, use
        /// <see cref="ThrowIfUnsuccessful" />
        /// with an <see cref="IProcessResultValidator{TProcessResult}" /> or the
        /// <c>UsePostExitValidation</c> middleware.
        /// </remarks>
        /// <returns>
        /// <c>true</c> if <see cref="ProcessResult.ExitCode" /> is zero; otherwise, <c>false</c>.
        /// </returns>
        public bool IsExitCodeZero()
            => processResult.ExitCode == 0;

        /// <summary>
        /// Throws <see cref="ProcessNotSuccessfulException{TProcessResult}" /> when
        /// <see cref="IsExitCodeZero" /> returns <c>false</c>.
        /// </summary>
        /// <remarks>
        /// This is a default exit-code heuristic for non-DI callers.
        /// For caller-stated success validation, prefer
        /// <see cref="ThrowIfUnsuccessful" />
        /// or the <c>UsePostExitValidation</c> middleware, which honour an
        /// <see cref="IProcessResultValidator{TProcessResult}" /> supplied by the caller.
        /// </remarks>
        /// <exception cref="ProcessNotSuccessfulException{TProcessResult}">
        /// Thrown when <see cref="ProcessResult.ExitCode" /> is not zero.
        /// </exception>
        public void EnsureExitCodeZero()
        {
            if (processResult.ExitCode != 0)
            {
                throw new ProcessNotSuccessfulException<TProcessResult>(
                    new ProcessExceptionInfo<TProcessResult>(processResult));
            }
        }

        /// <summary>
        /// Throws an exception if the process result is determined to be unsuccessful based on the given validator.
        /// </summary>
        /// <param name="validator">
        /// A validator that performs validation rules on the process result to determine its success or failure.
        /// </param>
        /// <param name="configuration">
        /// Optional process configuration that provides additional context for constructing the exception information.
        /// Defaults to null.
        /// </param>
        /// <exception cref="ProcessNotSuccessfulException{TProcessResult}">
        /// Thrown when the validation indicates the process result is not successful.
        /// </exception>
        public void ThrowIfUnsuccessful(IProcessResultValidator<TProcessResult> validator,
            ProcessConfiguration? configuration = null)
        {
            bool success = validator.Validate(processResult);

            if (!success)
            {
                ProcessExceptionInfo<TProcessResult> exceptionInfo = configuration is not null
                    ? new ProcessExceptionInfo<TProcessResult>(processResult, configuration)
                    : new ProcessExceptionInfo<TProcessResult>(processResult);
                
                throw new ProcessNotSuccessfulException<TProcessResult>(exceptionInfo);
            }
        }
    }
    
    extension(BufferedProcessResult processResult)
    {
        /// <summary>
        /// Retrieves the first line of the standard output from the process result.
        /// </summary>
        /// <returns>
        /// A string representing the first line of the standard output. If the output is empty,
        /// an empty string will be returned.
        /// </returns>
        public string GetFirstOutputLine()
            => GetFirstLineFromSpan(processResult.StandardOutput.AsSpan());

        /// <summary>
        /// Splits the standard output and standard error of the process result into lines.
        /// </summary>
        /// <returns>
        /// A tuple containing two arrays of strings. The first array represents the lines
        /// from the standard output, while the second array represents the lines from the standard error.
        /// </returns>
        public (string[] standardOutputLines, string[] standardErrorLines) GetOutputLines()
        {
            return (processResult.StandardOutput.Split(Environment.NewLine),
                processResult.StandardError.Split(Environment.NewLine));
        }

        /// <summary>
        /// Enumerates the standard output lines lazily, splitting on
        /// <see cref="Environment.NewLine" />.
        /// </summary>
        /// <remarks>
        /// Produces the same results as <c>string.Split(Environment.NewLine)</c>,
        /// including a trailing empty element when the input ends with a newline.
        /// Unlike <c>MemoryExtensions.EnumerateLines</c>, this method treats
        /// only the full <see cref="Environment.NewLine" /> sequence as a
        /// separator, preserving the <c>GetOutputLines()</c> contract.
        /// </remarks>
        /// <returns>A lazy sequence of output lines.</returns>
        public IEnumerable<string> EnumerateOutputLines()
            => EnumerateLines(processResult.StandardOutput);

        /// <summary>
        /// Enumerates the standard error lines lazily, splitting on
        /// <see cref="Environment.NewLine" />.
        /// </summary>
        /// <remarks>
        /// Produces the same results as <c>string.Split(Environment.NewLine)</c>,
        /// including a trailing empty element when the input ends with a newline.
        /// Unlike <c>MemoryExtensions.EnumerateLines</c>, this method treats
        /// only the full <see cref="Environment.NewLine" /> sequence as a
        /// separator, preserving the <c>GetOutputLines()</c> contract.
        /// </remarks>
        /// <returns>A lazy sequence of error lines.</returns>
        public IEnumerable<string> EnumerateErrorLines()
            => EnumerateLines(processResult.StandardError);

        /// <summary>
        /// Determines if the process result contains any errors.
        /// Checks whether the StandardError output is not null or empty
        /// and verifies its length is greater than zero.
        /// </summary>
        /// <returns>
        /// True if the process result has errors; otherwise, false.
        /// </returns>
        public bool HasErrors()
            => !string.IsNullOrEmpty(processResult.StandardError);
    }


    /// <summary>
    ///     Returns the first line of the supplied text without allocating a full line array.
    /// </summary>
    private static string GetFirstLineFromSpan(ReadOnlySpan<char> text)
    {
        foreach (ReadOnlySpan<char> line in text.EnumerateLines())
            return line.ToString();

        return string.Empty;
    }

    /// <summary>
    ///     Lazily yields lines from <paramref name="text" /> by walking consecutive
    ///     <see cref="Environment.NewLine" /> separators.
    /// </summary>
    /// <remarks>
    ///     This produces the same results as <c>text.Split(Environment.NewLine)</c>,
    ///     including a trailing empty element when the input ends with a newline.
    ///     <c>MemoryExtensions.EnumerateLines</c> is deliberately avoided because it
    ///     treats lone <c>\n</c> as a separator, diverging from the pinned contract.
    /// </remarks>
    private static IEnumerable<string> EnumerateLines(string text)
    {
        if (text is null)
        {
            yield break;
        }

        string separator = Environment.NewLine;
        int start = 0;
        int index;

        while ((index = text.IndexOf(separator, start, StringComparison.Ordinal)) >= 0)
        {
            yield return text.Substring(start, index - start);
            start = index + separator.Length;
        }

        yield return text.Substring(start);
    }
}
