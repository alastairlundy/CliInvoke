/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Helpers;

namespace CliInvoke.Processes.Internal.ControlAdapters;

internal abstract class BaseProcessControlAdapter
{
    internal abstract void ResumeProcess(Process process);
   
    internal abstract void SuspendProcess(Process process);

    internal abstract void SetResourcePolicy(ProcessWrapper process,
        ProcessResourcePolicy? resourcePolicy);
    internal abstract void SetUserCredential(Process process, UserCredential credential);

    internal virtual void ApplyConfiguration(ProcessWrapper process, ProcessConfiguration processConfiguration)
    {
        ArgumentException.ThrowIfNullOrEmpty(processConfiguration.TargetFilePath);

        ProcessStartInfo processStartInfo = new()
        {
            FileName = processConfiguration.TargetFilePath,
            Arguments = string.IsNullOrEmpty(processConfiguration.Arguments)
                ? string.Empty
                : processConfiguration.Arguments,
            WorkingDirectory = processConfiguration.WorkingDirectoryPath,
            UseShellExecute = processConfiguration.UseShellExecution,
            CreateNoWindow = !processConfiguration.WindowCreation,
            RedirectStandardInput =
                processConfiguration.StandardInput is not null
                && processConfiguration.RedirectStandardInput,
            RedirectStandardOutput =  processConfiguration.OutputRedirection,
            RedirectStandardError = processConfiguration.OutputRedirection,
        };

        if (processConfiguration.RequiresAdministrator)
            RequireRunningAsAdmin(process);

#pragma warning disable CA1416
        SetUserCredential(process, processConfiguration.Credential);
#pragma warning restore CA1416

        if (processConfiguration.EnvironmentVariables.Count > 0)
            SetEnvironmentVariables(process, processConfiguration.EnvironmentVariables);

        if (processStartInfo.RedirectStandardInput)
            processStartInfo.StandardInputEncoding = processConfiguration.StandardInputEncoding;

        if (processStartInfo.RedirectStandardOutput)
            processStartInfo.StandardOutputEncoding =
                processConfiguration.StandardOutputEncoding;

        if (processStartInfo.RedirectStandardError)
            processStartInfo.StandardErrorEncoding = processConfiguration.StandardErrorEncoding;

        process.StartInfo = processStartInfo;
    }
    
    private void SetEnvironmentVariables(
        Process process, IReadOnlyDictionary<string, string> environmentVariables)
    {
        if (environmentVariables.Count == 0)
            return;

        foreach (KeyValuePair<string, string> variable in environmentVariables)
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (variable.Value is not null)
                process.StartInfo.Environment[variable.Key] = variable.Value;
    }
   
    internal abstract void RequireRunningAsAdmin(Process process);
   
    internal abstract Task<bool> SendInterruptSignalAsync(Process process,
        CancellationReason cancellationReason, ProcessExitConfiguration exitConfiguration,
        CancellationToken cancellationToken);
}