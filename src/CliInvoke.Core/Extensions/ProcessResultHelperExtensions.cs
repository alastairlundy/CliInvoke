/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Linq;

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
                    ? new(processResult, configuration)
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
            => processResult.StandardOutput.Split(Environment.NewLine).First();

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

    extension(PipedProcessResult processResult)
    {
        /// <summary>
        /// Determines whether the process result contains any errors by analysing the standard error stream.
        /// </summary>
        /// <returns>
        /// A boolean value indicating whether the standard error stream contains any content.
        /// Returns true if errors are present; otherwise, false.
        /// </returns>
        public bool HasErrors()
        {
            using StreamReader stdErrorReader = new(processResult.StandardOutput);

            string stdError = stdErrorReader.ReadToEnd();

            return stdError.Length > 0;
        }

        /// <summary>
        /// Determines asynchronously whether the process result contains any errors
        /// by reading the standard error stream.
        /// </summary>
        /// <param name="cancellationToken">
        /// A token to monitor for cancellation requests during the asynchronous operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result is a boolean value
        /// indicating whether any errors were present in the standard error stream.
        /// </returns>
        public async Task<bool> HasErrorsAsync(CancellationToken cancellationToken = default)
        {
            using StreamReader stdErrorReader = new(processResult.StandardOutput);

            string stdError = await stdErrorReader.ReadToEndAsync(cancellationToken);

            return stdError.Length > 0;
        }

        /// <summary>
        /// Reads the entire standard output and standard error streams from the process result.
        /// Returns the streams as strings within a tuple.
        /// </summary>
        /// <remarks>Use the <see cref="ReadLinesAsync"/> method instead where possible for async support.</remarks>
        /// <returns>
        /// A tuple where the first item is the standard output as a string and
        /// the second item is the standard error as a string.
        /// </returns>
        public (string standardOutput, string standardError) ReadLines()
        {
            using StreamReader stdOutReader = new (processResult.StandardOutput);
            using StreamReader stdErrReader = new (processResult.StandardError);
            
            string stdOut = stdOutReader.ReadToEnd();
            string stdError = stdErrReader.ReadToEnd();
            
            return (stdOut, stdError);
        }

        /// <summary>
        /// Asynchronously reads the entire standard output and standard error streams
        /// from the process result and returns them as strings.
        /// </summary>
        /// <param name="cancellationToken">
        /// A token to monitor for cancellation requests. Defaults to <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains
        /// a tuple where the first item is the standard output as a string and the second
        /// item is the standard error as a string.
        /// </returns>
        public async Task<(string standardOutput, string standardError)> ReadLinesAsync(
            CancellationToken cancellationToken = default)
        {
            using StreamReader stdOutReader = new (processResult.StandardOutput);
            using StreamReader stdErrReader = new (processResult.StandardError);
            
            string stdOut = await stdOutReader.ReadToEndAsync(cancellationToken);
            string stdError = await stdErrReader.ReadToEndAsync(cancellationToken);
            
            return (stdOut, stdError);
        }

        /// <summary>
        /// Retrieves the first line of the standard output from the process result.
        /// </summary>
        /// <returns>
        /// A string representing the first line of the standard output. If the output is empty,
        /// an empty string will be returned.
        /// </returns>
        public string GetFirstLine()
        {
            using StreamReader stdOutReader = new(processResult.StandardOutput);
            
            string stdOut = stdOutReader.ReadToEnd();

            return stdOut.Split(Environment.NewLine).First();
        }

        /// <summary>
        /// Asynchronously retrieves the first line of the standard output from the process result.
        /// </summary>
        /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// A string representing the first line of the standard output. If the output is empty,
        /// an empty string will be returned.
        /// </returns>
        public async Task<string> GetFirstLineAsync(CancellationToken cancellationToken = default)
        {
            using StreamReader stdOutReader = new(processResult.StandardOutput);

            string stdOut = await stdOutReader.ReadToEndAsync(cancellationToken);

            return stdOut.Split(Environment.NewLine).First();
        }
    }
}