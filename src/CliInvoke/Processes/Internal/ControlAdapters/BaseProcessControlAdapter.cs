/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.ComponentModel;
using System.Runtime.InteropServices;

using CliInvoke.Internal;
using CliInvoke.Processes.Internal.Cancellation;

namespace CliInvoke.Processes.Internal.ControlAdapters;

internal abstract class BaseProcessControlAdapter
{
    internal abstract void ResumeProcess(Process process);
   
    internal abstract void SuspendProcess(Process process);

    internal abstract void SetResourcePolicy(ProcessWrapper process,
        ProcessResourcePolicy? resourcePolicy);
    internal abstract void SetUserCredential(Process process, UserCredential credential);

    /// <summary>
    ///     How long, in milliseconds, to wait for a child's exit signal when deciding whether a
    ///     failed resource-policy setter is a benign exit race or a genuine permission failure.
    /// </summary>
    /// <remarks>
    ///     Measured pre-signal termination windows on Windows are about a millisecond; this
    ///     budget gives several times that margin while keeping the failure-path cost trivial
    ///     against a full spawn.
    /// </remarks>
    private const int ExitRaceSettleMilliseconds = 5;

    /// <summary>
    ///     Applies a single resource-policy setter to the child, swallowing the exception the
    ///     operating system raises when the child terminated between the caller's exited-check
    ///     and the setter call (a benign exit race: resource policy on a terminated child is
    ///     meaningless, matching the exit-race handling in the
    ///     <see cref="ResumeProcess(Process)"/> and <see cref="SuspendProcess(Process)"/>
    ///     implementations). Failures on a child that is verifiably still alive keep surfacing.
    /// </summary>
    /// <param name="process">The wrapper around the child process the policy applies to.</param>
    /// <param name="apply">The setter to invoke.</param>
    /// <remarks>
    ///     On Windows, <see cref="Process.ProcessorAffinity"/>, <see cref="Process.PriorityBoostEnabled"/>,
    ///     and the working-set setters fail with <c>ERROR_ACCESS_DENIED</c> (5) while termination
    ///     is in progress. The BCL's own exited-check inside those setters waits on the process
    ///     handle with a zero timeout, so during the pre-signal termination window the runtime
    ///     considers the child alive, calls the underlying Win32 API, and gets
    ///     <c>ACCESS_DENIED</c>, which surfaces as <see cref="Win32Exception"/>.
    ///     <para>
    ///     On Unix-like systems the same window maps to errno <c>ESRCH</c> (3), which
    ///     <see cref="ProcessWrapper.OnStarted"/> also treats as benign. Other native error
    ///     codes never match, so unrelated access failures keep surfacing.
    ///     </para>
    /// </remarks>
    internal static void ApplyPolicySetting(ProcessWrapper process, Action apply)
    {
        try
        {
            apply();
        }
        catch (Win32Exception exception) when (IsBenignResourcePolicyExitRace(process, exception))
        {
        }
    }

    /// <summary>
    ///     Determines whether a <see cref="Win32Exception"/> from a resource-policy setter means
    ///     the child exited mid-application: Windows reports <c>ERROR_ACCESS_DENIED</c> (5) for
    ///     a terminating child whose handle is not yet signaled, while Unix-like systems map
    ///     errno <c>ESRCH</c> (3) to the same condition. The Windows check deliberately never
    ///     matches Unix error numbers (3 is <c>ERROR_PATH_NOT_FOUND</c> there), and the Unix
    ///     check deliberately never matches errno 5 (<c>EIO</c>).
    /// </summary>
    private static bool IsBenignResourcePolicyExitRace(
        ProcessWrapper process, Win32Exception exception)
    {
        int benignErrorCode = OperatingSystem.IsWindows()
            ?
            // ERROR_ACCESS_DENIED: rejected while termination is in progress.
            5
            :
            // ESRCH: the child is gone.
            3;

        return exception.NativeErrorCode == benignErrorCode && SettledExited(process);
    }

    /// <summary>
    ///     Determines whether the child's exit has settled by waiting briefly for the exit
    ///     signal, closing the gap between a failed setter and the process handle becoming
    ///     signaled. A live child that is genuinely denied (for example a protected process)
    ///     stays unsignaled, so the wait elapses and the original exception surfaces.
    /// </summary>
    private static bool SettledExited(ProcessWrapper process)
    {
        try
        {
            return process.WaitForExit(ExitRaceSettleMilliseconds);
        }
        catch (Exception)
        {
            // Disposed or invalid process handle: the process is gone.
            return true;
        }
    }


    /// <summary>
    ///     Resolves the POSIX signal that terminated a process from its exit code, using the
    ///     best-effort <c>ExitCode &gt; 128</c> heuristic. Returns <c>null</c> on platforms where
    ///     signals are not represented this way.
    /// </summary>
    internal abstract PosixSignal? GetTerminatingSignal(int exitCode);

    internal virtual void ApplyConfiguration(ProcessWrapper process, ProcessConfiguration processConfiguration)
    {
        ArgumentException.ThrowIfNullOrEmpty(processConfiguration.TargetFilePath);

        // Tokenized argument lists are checked before a child is started when the target
        // parses its own command line. The raw preformatted Arguments string is left to
        // the caller, who owns its quoting and composition.
        if (processConfiguration.ArgumentList.Count > 0
            && CmdCompositionArgumentValidator.IsCmdOrBatchTarget(processConfiguration.TargetFilePath))
        {
            CmdCompositionArgumentValidator.Validate(
                processConfiguration.ArgumentList,
                nameof(ProcessConfiguration.ArgumentList));
        }

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

        // When a verbatim argument list is supplied (shell wrappers), emit it via
        // ArgumentList so the OS command-line parser passes each entry to the child
        // unmodified. This prevents the double-parse command-injection vector where a
        // single re-tokenized Arguments string is re-interpreted by the wrapped shell.
        // The read-only ArgumentList is the canonical source.
        IReadOnlyList<string> effectiveArgumentList = processConfiguration.ArgumentList;

        if (effectiveArgumentList.Count > 0)
        {
            processStartInfo.Arguments = string.Empty;
            foreach (string arg in effectiveArgumentList)
                processStartInfo.ArgumentList.Add(arg);
        }

        if (processStartInfo.RedirectStandardInput)
            processStartInfo.StandardInputEncoding = processConfiguration.StandardInputEncoding;

        if (processStartInfo.RedirectStandardOutput)
            processStartInfo.StandardOutputEncoding =
                processConfiguration.StandardOutputEncoding;

        if (processStartInfo.RedirectStandardError)
            processStartInfo.StandardErrorEncoding = processConfiguration.StandardErrorEncoding;

        // Assign the assembled ProcessStartInfo BEFORE applying platform-specific mutations
        // below. RequireRunningAsAdmin, SetUserCredential, and SetEnvironmentVariables mutate
        // process.StartInfo in place, so they must run after this assignment to land on the
        // instance that will actually be used to start the process. Mutating before the
        // assignment silently drops environment variables, credentials, and the admin verb.
        process.StartInfo = processStartInfo;

        if (processConfiguration.RequiresAdministrator)
            RequireRunningAsAdmin(process);

#pragma warning disable CA1416
        SetUserCredential(process, processConfiguration.Credential);
#pragma warning restore CA1416

        if (processConfiguration.EnvironmentVariables.Count > 0)
            SetEnvironmentVariables(process, processConfiguration.EnvironmentVariables);
    }
    
    /// <summary>
    ///     Validates the assembled command-line length against the Windows
    ///     <c>CreateProcess</c> limit of approximately 32,767 characters.
    /// </summary>
    /// <remarks>
    ///     Must be called after <see cref="ProcessStartInfo.FileName"/> has been set to the
    ///     <em>resolved</em> executable path. A short <c>TargetFilePath</c> such as
    ///     <c>"dotnet.exe"</c> can resolve via PATH lookup to a much longer absolute path,
    ///     so validating the unresolved name would under-measure the real command line.
    ///     <para>
    ///     The calculation mirrors the command line the .NET runtime actually serializes for
    ///     <c>CreateProcess</c> when <see cref="ProcessStartInfo.UseShellExecute"/> is
    ///     <c>false</c>: the instance name is wrapped in quotes unless it is already fully
    ///     quoted (<see cref="ProcessStartInfo.FileName"/> + 2 chars, after trimming), each
    ///     <see cref="ProcessStartInfo.ArgumentList"/> entry is measured using the runtime's
    ///     argument quoting rules (quotes, escaped quotes, and doubled
    ///     backslash runs) separated by single spaces, and a verbatim
    ///     <see cref="ProcessStartInfo.Arguments"/> string is measured after one leading
    ///     separator. Skipped when <see cref="ProcessStartInfo.UseShellExecute"/> is
    ///     <c>true</c>, where the <c>CreateProcess</c> limit does not apply the same way.
    ///     </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when the measured command-line length is equal to or greater than the Windows limit.
    /// </exception>
    internal static void ValidateCommandLineLength(ProcessStartInfo processStartInfo)
    {
        if (processStartInfo.UseShellExecute)
            return;

        const int windowsCommandLineLengthLimit = 32_767;

        int totalLength = GetQuotedFileNameLength(processStartInfo.FileName);

        IReadOnlyList<string> argumentList = processStartInfo.ArgumentList;
        if (argumentList.Count > 0)
        {
            for (int i = 0; i < argumentList.Count; i++)
            {
                if (i > 0)
                    totalLength += 1;
                totalLength += GetSerializedArgumentLength(argumentList[i]);
            }
        }
        else if (!string.IsNullOrEmpty(processStartInfo.Arguments))
        {
            totalLength += 1;
            totalLength += processStartInfo.Arguments.Length;
        }

        if (totalLength >= windowsCommandLineLengthLimit)
        {
            throw new ArgumentException(
                $"The assembled command line for '{processStartInfo.FileName}' is approximately " +
                $"{totalLength} characters long, which is at or above the Windows CreateProcess " +
                $"limit of approximately {windowsCommandLineLengthLimit} characters. " +
                $"Note that this limit is approximate and includes the null terminator.");
        }
    }

    /// <summary>
    ///     Measures how the .NET runtime serializes <see cref="ProcessStartInfo"/> into the
    ///     <c>lpCommandLine</c> string passed to <c>CreateProcess</c>: the trimmed
    ///     <see cref="ProcessStartInfo.FileName"/> wrapped in opening and closing quotes,
    ///     unless it already starts and ends with a quote.
    /// </summary>
    private static int GetQuotedFileNameLength(string fileName)
    {
        ReadOnlySpan<char> trimmed = fileName.AsSpan().Trim();
        bool alreadyFullyQuoted =
            trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[trimmed.Length - 1] == '"';

        int length = trimmed.Length;
        return alreadyFullyQuoted ? length : length + 2;
    }

    /// <summary>
    ///     Measures how the .NET runtime serializes a <see cref="ProcessStartInfo.ArgumentList"/> single
    ///     <see cref="ProcessStartInfo.ArgumentList"/> entry: bare when it contains no
    ///     whitespace or quotes; otherwise wrapped in quotes with quote characters escaped
    ///     as <c>\"</c> and backslash runs doubled when adjacent to a quote.
    /// </summary>
    private static int GetSerializedArgumentLength(string argument)
    {
        if (argument.Length != 0 && ContainsNoWhitespaceOrQuotes(argument))
            return argument.Length;

        int length = 2;
        int idx = 0;
        while (idx < argument.Length)
        {
            if (argument[idx] == '\\')
            {
                int backslashRun = 0;
                while (idx < argument.Length && argument[idx] == '\\')
                {
                    idx++;
                    backslashRun++;
                }

                if (idx == argument.Length)
                {
                    // Closing quote follows, so the runtime doubles the run.
                    length += backslashRun * 2;
                }
                else if (argument[idx] == '"')
                {
                    // Doubled run plus escaping backslash + escaped quote.
                    length += backslashRun * 2 + 2;
                    idx++;
                }
                else
                {
                    length += backslashRun;
                }
            }
            else
            {
                length += argument[idx] == '"' ? 2 : 1;
                idx++;
            }
        }

        return length;
    }

    private static bool ContainsNoWhitespaceOrQuotes(string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            char current = value[i];
            if (char.IsWhiteSpace(current) || current == '"')
                return false;
        }

        return true;
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