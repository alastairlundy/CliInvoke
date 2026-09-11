/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace CliInvoke.Core;

/// <summary>
///     A class to store Process configuration information.
/// </summary>
/// <remarks>
///     The type is directly constructible via an object initialiser, with init-only
///     properties and a required <see cref="TargetFilePath"/>. Direct
///     construction carries the same semantics as the convenience constructor: every
///     property defaults to the value documented on the property, and init-time validation
///     runs on <see cref="TargetFilePath"/> and <see cref="WorkingDirectoryPath"/>.
/// </remarks>
public class ProcessConfiguration : IEquatable<ProcessConfiguration>
{
    /// <summary>
    ///     Initialises a new instance of the <see cref="ProcessConfiguration" /> class with
    ///     all properties taking the defaults documented on the corresponding properties.
    /// </summary>
    /// <remarks>
    ///     <see cref="TargetFilePath"/> is required and must be set via the object initialiser.
    /// </remarks>
    public ProcessConfiguration()
    {
    }

    /// <summary>
    ///     Initialises a new instance of the <see cref="ProcessConfiguration" /> class
    ///     with the target file path, arguments, and output redirection setting; all other
    ///     properties take the defaults documented on the corresponding properties.
    /// </summary>
    /// <param name="targetFilePath">The file path of the executable to be run.</param>
    /// <param name="arguments">The arguments to pass to the executable.</param>
    /// <param name="outputRedirection">Whether to redirect standard output and error.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="targetFilePath" /> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="arguments" /> is null.</exception>
    [SetsRequiredMembers]
    public ProcessConfiguration(string targetFilePath, string arguments = "",
        bool outputRedirection = true)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        TargetFilePath = targetFilePath;
        Arguments = arguments;
        OutputRedirection = outputRedirection;
    }

    /// <summary>
    ///     Whether administrator privileges should be used when executing the Command.
    /// </summary>
    public bool RequiresAdministrator { get; init; }

    /// <summary>
    ///     The file path of the executable to be run and wrapped.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if set to null or empty.</exception>
    /// <remarks>
    ///     Not mutated after construction; for the resolved file path, see the result.
    ///     <see cref="ProcessResult.ExecutedFilePath"/>.
    /// </remarks>
    public required string TargetFilePath { get; init; }

    /// <summary>
    ///     The working directory path to be used when executing the Command.
    /// </summary>
    /// <remarks>
    ///     Defaults to <see cref="Directory.GetCurrentDirectory()"/> when not set.
    /// </remarks>
    /// <exception cref="DirectoryNotFoundException">Thrown if set to a directory that does not exist.</exception>
    public string WorkingDirectoryPath
    {
        get;
        init
        {
            if (!Directory.Exists(value))
                throw new DirectoryNotFoundException(string.Format(
                    Resources.Exceptions_DirectoryNotFound_WorkingDirectory, value));

            field = value;
        }
    } = Directory.GetCurrentDirectory();

    /// <summary>
    ///     The arguments to be provided to the executable to be run.
    /// </summary>
    /// <remarks>
    ///     When <see cref="ArgumentList"/> is non-empty, the control adapter ignores this
    ///     property and emits <see cref="ArgumentList"/> via
    ///     <see cref="System.Diagnostics.ProcessStartInfo.ArgumentList"/> instead. This
    ///     prevents the double-parse command-injection vector where a re-tokenised
    ///     <c>Arguments</c> string is re-interpreted by a wrapped shell.
    /// </remarks>
    public string Arguments { get; init; } = string.Empty;

    /// <summary>
    ///     An optional verbatim argument list. When non-empty, the control adapter emits these via
    ///     <see cref="System.Diagnostics.ProcessStartInfo.ArgumentList"/> instead of the single
    ///     <see cref="Arguments"/> string, so the operating-system command-line parser passes each
    ///     entry to the child process unmodified. <b>If both <see cref="Arguments"/> and
    ///     <see cref="ArgumentList"/> are set, <see cref="ArgumentList"/> takes precedence and
    ///     <see cref="Arguments"/> is ignored.</b> This is the safe path for shell wrappers
    ///     (PowerShell/cmd), whose own parser would otherwise re-interpret a single re-tokenized
    ///     <see cref="Arguments"/> string — a command-injection vector.
    /// </summary>
    /// <remarks>
    ///     Any supplied list is captured as a snapshot; later mutations made to the caller's
    ///     original collection are not reflected in the configuration.
    /// </remarks>
    public IReadOnlyList<string> ArgumentList { get; init; } = [];

    /// <summary>
    ///     Whether to enable window creation or not when the Command's Process is run.
    /// </summary>
    public bool WindowCreation { get; init; }

    /// <summary>
    ///     The environment variables to be set.
    /// </summary>
    /// <remarks>
    ///     Any supplied dictionary is captured as an immutable snapshot sorted by key using
    ///     ordinal comparison. This normalisation makes <see cref="Equals(ProcessConfiguration)"/>
    ///     and <see cref="GetHashCode"/> order-independent (two configurations carrying the same
    ///     variables in a different insertion order are considered equal) and avoids re-sorting
    ///     on every hash code computation. Because environment variable ordering is irrelevant
    ///     to the spawned process, this has no effect on process execution. The snapshot also
    ///     isolates the configuration from later mutations made to the caller's original
    ///     dictionary.
    /// </remarks>
    public IReadOnlyDictionary<string, string> EnvironmentVariables
    {
        get;
        init => field = ImmutableSortedDictionary.CreateRange(StringComparer.Ordinal, value);
    } = ImmutableSortedDictionary<string, string>.Empty;

    /// <summary>
    ///     The credential to be used when executing the Command.
    /// </summary>
    public UserCredential Credential { get; init; } = UserCredential.Null;

    /// <summary>
    ///     Whether to use Shell Execution or not when executing the Command.
    /// </summary>
    /// <remarks>
    ///     Using Shell Execution whilst also Redirecting Standard Input will throw an Exception. This
    ///     is a known issue with the System Process class.
    /// </remarks>
    /// <seealso
    ///     href="https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandarderror" />
    public bool UseShellExecution { get; init; }

    /// <summary>
    ///     The Standard Input source to redirect Standard Input to if configured.
    /// </summary>
    /// <remarks>
    ///     Using Shell Execution whilst also Redirecting Standard Input will throw an Exception. This
    ///     is a known issue with the System Process class.
    /// </remarks>
    /// <seealso
    ///     href="https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandarderror" />
    public StreamWriter? StandardInput { get; init; } = StreamWriter.Null;

    /// <summary>
    ///     Whether to redirect the Standard Input.
    /// </summary>
    public bool RedirectStandardInput { get; init; }

    /// <summary>
    /// Whether to redirect process Standard Output and Error.
    /// </summary>
    public bool OutputRedirection { get; init; }

    /// <summary>
    ///     The Process Resource Policy to be used for executing the Command.
    /// </summary>
    /// <remarks>
    ///     Process Resource Policy objects enable configuring Processor Affinity and other resource settings
    /// to be applied to the Command if supported by the currently running operating system.
    ///     <para>
    ///         Not all properties of a Process Resource Policy support all operating systems. Check
    ///         before configuring a property.
    ///     </para>
    /// </remarks>
    public ProcessResourcePolicy ResourcePolicy { get; init; } = ProcessResourcePolicy.Default;

    /// <summary>
    ///     The encoding to use for the Standard Input.
    /// </summary>
    public Encoding StandardInputEncoding { get; init; } = Encoding.Default;

    /// <summary>
    ///     The encoding to use for the Standard Output.
    /// </summary>
    public Encoding StandardOutputEncoding { get; init; } = Encoding.Default;

    /// <summary>
    ///     The encoding to use for the Standard Error.
    /// </summary>
    public Encoding StandardErrorEncoding { get; init; } = Encoding.Default;

    /// <summary>
    ///     Determines if a Process configuration is equal to another Process configuration.
    /// </summary>
    /// <param name="other">The other Process configuration to compare</param>
    /// <returns>True if both are equal to each other; false otherwise.</returns>
    public bool Equals(ProcessConfiguration? other)
    {
        if (other is null) return false;
        
        return TargetFilePath.Equals(other.TargetFilePath)
               && EnvironmentVariables.Count == other.EnvironmentVariables.Count
               && EnvironmentVariables.SequenceEqual(other.EnvironmentVariables)
               && Arguments.Equals(other.Arguments)
               && ArgumentList.SequenceEqual(other.ArgumentList)
               && ResourcePolicy.Equals(other.ResourcePolicy)
               && WorkingDirectoryPath.Equals(other.WorkingDirectoryPath)
               && UseShellExecution.Equals(other.UseShellExecution)
               && Credential.Equals(other.Credential)
               && RequiresAdministrator == other.RequiresAdministrator
               && WindowCreation == other.WindowCreation
               && ReferenceEquals(StandardInput?.BaseStream, other.StandardInput?.BaseStream)
               && RedirectStandardInput.Equals(other.RedirectStandardInput)
               && OutputRedirection == other.OutputRedirection
               && StandardInputEncoding.Equals(other.StandardInputEncoding)
               && StandardOutputEncoding.Equals(other.StandardOutputEncoding)
               && StandardErrorEncoding.Equals(other.StandardErrorEncoding);
    }

    /// <summary>
    ///     Determines if a Process configuration is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns>True if both are equal to each other; false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;

        if (obj is ProcessConfiguration other) return Equals(other);

        return false;
    }

    /// <summary>
    ///     Returns the hash code for the current ProcessConfiguration.
    /// </summary>
    /// <returns>The hash code for the current ProcessConfiguration.</returns>
    public override int GetHashCode()
    {
        HashCode hashCode = new HashCode();

        hashCode.Add(TargetFilePath);
        hashCode.Add(Arguments);
        foreach (string arg in ArgumentList)
            hashCode.Add(arg);
        hashCode.Add(WorkingDirectoryPath);
        foreach (KeyValuePair<string, string> kvp in EnvironmentVariables)
        {
            hashCode.Add(kvp.Key);
            hashCode.Add(kvp.Value);
        }

        hashCode.Add(Credential);
        hashCode.Add(RequiresAdministrator);
        hashCode.Add(WindowCreation);

        hashCode.Add(StandardInput?.BaseStream);
        hashCode.Add(ResourcePolicy);
        hashCode.Add(StandardInputEncoding);
        hashCode.Add(StandardOutputEncoding);
        hashCode.Add(StandardErrorEncoding);

        return hashCode.ToHashCode();
    }

    /// <summary>
    ///     Determines if a Process configuration is equal to another Process configuration.
    /// </summary>
    /// <param name="left">A Process configuration to be compared.</param>
    /// <param name="right">The other Process configuration to be compared.</param>
    /// <returns>True if both Process configurations are equal to each other; false otherwise.</returns>
    public static bool Equals(ProcessConfiguration? left, ProcessConfiguration? right)
    {
        if (left is null || right is null) return false;

        return left.Equals(right);
    }

    /// <summary>
    ///     Determines if a Process configuration is equal to another Process configuration.
    /// </summary>
    /// <param name="left">A Process configuration to be compared.</param>
    /// <param name="right">The other Process configuration to be compared.</param>
    /// <returns>True if both Process configurations are equal to each other; false otherwise.</returns>
    public static bool operator ==(ProcessConfiguration? left, ProcessConfiguration? right)
    {
        if (left is null)
            return right is null;

        return left.Equals(right);
    }

    /// <summary>
    ///     Determines if a Process Configuration is not equal to another Process configuration.
    /// </summary>
    /// <param name="left">A Process configuration to be compared.</param>
    /// <param name="right">The other Process configuration to be compared.</param>
    /// <returns>True if both Process configurations are not equal to each other; false otherwise.</returns>
    public static bool operator !=(ProcessConfiguration? left, ProcessConfiguration? right)
    {
        if (left is null)
            return right is not null;

        return !left.Equals(right);
    }

    /// <summary>
    ///     Returns a string representation of the Command configuration.
    /// </summary>
    /// <returns>A string representation of the Command configuration.</returns>
    public override string ToString()
    {
        StringBuilder stringBuilder = new();

        stringBuilder.Append($"{TargetFilePath} {Arguments}");
        
        if (!string.IsNullOrEmpty(WorkingDirectoryPath))
        {
            stringBuilder.Append($" ({Resources
                .Labels_ProcessConfiguration_ToString_WorkingDirectory}: {WorkingDirectoryPath})");
        }
        
        if (RequiresAdministrator)
        {
            stringBuilder.Append($"{Environment.NewLine} {Resources
                .Labels_ProcessConfiguration_ToString_RequiresAdmin}");
        }

        if (UseShellExecution)
        {
            stringBuilder.Append($"{Environment.NewLine} {Resources
                .Labels_ProcessConfiguration_ToString_ShellExecution}");
        }
        
        return stringBuilder.ToString();
    }
}