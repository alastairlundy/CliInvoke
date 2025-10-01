using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AlastairLundy.CliInvoke.Internal.Processes;

internal static class ProcessSetEnvironmentVariablesExtensions
{
    /// <summary>
    /// Sets environment variables for a specified ProcessStartInfo object.
    /// </summary>
    /// <param name="processStartInfo">The ProcessStartInfo object to set environment variables for.</param>
    /// <param name="environmentVariables">A dictionary of environment variable names and their corresponding values.</param>
    internal static void SetEnvironmentVariables(this ProcessStartInfo processStartInfo,
        IDictionary<string, string> environmentVariables)
    {
        // ReSharper disable once RedundantBoolCompare
        if (environmentVariables.Any() == false)
            return;
        
        foreach (KeyValuePair<string, string> variable in environmentVariables)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (variable.Value is not null)
            {
                processStartInfo.Environment[variable.Key] = variable.Value;
            }
        }
    }
}