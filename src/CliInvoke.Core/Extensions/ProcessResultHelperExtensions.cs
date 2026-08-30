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
        /// Throws an exception if the process result is determined to be unsuccessful based on the given validator.
        /// </summary>
        /// <typeparam name="TProcessResult">
        /// The type of the process result being validated. Must inherit from the <see cref="ProcessResult"/> class.
        /// </typeparam>
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
}
