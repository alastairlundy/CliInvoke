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

public static class ProcessResultHelperExtensions
{
    extension<TProcessResult>(TProcessResult processResult)
        where TProcessResult : ProcessResult
    {
        public void ThrowIfUnsuccessful(IProcessResultValidator<TProcessResult> validator, ProcessConfiguration? configuration = null)
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
        public string GetFirstLine() 
            => processResult.StandardOutput.Split(Environment.NewLine).First();
        
        public bool HasErrors()
            => !string.IsNullOrEmpty(processResult.StandardError) && processResult.StandardError.Length > 0;
    }

    extension(PipedProcessResult processResult)
    {
        /*public bool HasErrors()
        {
            StreamReader streamReader = new(processResult.StandardError);
            
            streamReader.ReadT
        }
        
        public async Task<bool> HasErrorsAsync()
        {
            
        }*/

        public (string standardOutput, string standardError) ReadLines()
        {
            using StreamReader stdOutReader = new StreamReader(processResult.StandardOutput);
            using StreamReader stdErrReader = new StreamReader(processResult.StandardError);
            
            string stdOut = stdOutReader.ReadToEnd();
            string stdError = stdErrReader.ReadToEnd();
            
            return (stdOut, stdError);
        }
        
        public async Task<(string standardOutput, string standardError)> ReadLinesAsync()
        {
            using StreamReader stdOutReader = new StreamReader(processResult.StandardOutput);
            using StreamReader stdErrReader = new StreamReader(processResult.StandardError);
            
            string stdOut = await stdOutReader.ReadToEndAsync();
            string stdError = await stdErrReader.ReadToEndAsync();
            
            return (stdOut, stdError);
        }
        
        /*public string GetFirstLine()
        {
            
        }
        
        public async Task<string> GetFirstLineAsync()
        {
            processResult.StandardOutput.Read
        }*/
    }
}