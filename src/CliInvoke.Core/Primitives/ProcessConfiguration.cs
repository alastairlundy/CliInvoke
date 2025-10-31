/*
    AlastairLundy.CliInvoke.Core 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Text;

using AlastairLundy.CliInvoke.Core.Internal.Localizations;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace AlastairLundy.CliInvoke.Core;

/// <summary>
/// A class to store Process configuration information.
/// </summary>
public class ProcessConfiguration : IEquatable<ProcessConfiguration>, IDisposable
{
    /// <summary>
    /// Configures the Command configuration to be wrapped and executed.
    /// </summary>
    /// <param name="targetFilePath">The target file path of the command to be executed.</param>
    /// <param name="arguments">The arguments to pass to the Command upon execution.</param>
    /// <param name="workingDirectoryPath">The working directory to be used.</param>
    /// <param name="requiresAdministrator">Whether to run the Command with administrator privileges.</param>
    /// <param name="environmentVariables">The environment variables to be set (if specified).</param>
    /// <param name="credential">The credential to be used (if specified).</param>
    /// <param name="standardInput">The standard input source to be used (if specified).</param>
    /// <param name="standardOutput">The standard output destination to be used (if specified).</param>
    /// <param name="standardError">The standard error destination to be used (if specified).</param>
    /// <param name="standardInputEncoding">The Standard Input Encoding to be used (if specified).</param>
    /// <param name="standardOutputEncoding">The Standard Output Encoding to be used (if specified).</param>
    /// <param name="standardErrorEncoding">The Standard Error Encoding to be used (if specified).</param>
    /// <param name="processResourcePolicy">The process resource policy to be used (if specified).</param>
    /// <param name="windowCreation">Whether to enable or disable Window Creation of the Command's Process.</param>
    /// <param name="useShellExecution">Whether to enable or disable executing the Command through Shell Execution.</param>
    /// <param name="redirectStandardInput"></param>
    /// <param name="redirectStandardOutput"></param>
    /// <param name="redirectStandardError"></param>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("watchos")]
    [UnsupportedOSPlatform("browser")]
    public ProcessConfiguration(string targetFilePath,
        bool redirectStandardInput, bool redirectStandardOutput, bool redirectStandardError,
        string? arguments = null, string? workingDirectoryPath = null,
        bool requiresAdministrator = false,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        UserCredential? credential = null,
        StreamWriter? standardInput = null,
        StreamReader? standardOutput = null,
        StreamReader? standardError = null,
        Encoding? standardInputEncoding = null,
        Encoding? standardOutputEncoding = null,
        Encoding? standardErrorEncoding = null,
        ProcessResourcePolicy? processResourcePolicy = null,
        bool windowCreation = false,
        bool useShellExecution = false)
    {
        TargetFilePath = targetFilePath;
        RequiresAdministrator = requiresAdministrator;
        Arguments = arguments ?? string.Empty;
        WorkingDirectoryPath = workingDirectoryPath ?? Directory.GetCurrentDirectory();
        EnvironmentVariables = environmentVariables ?? new Dictionary<string, string>();
        Credential = credential ?? UserCredential.Null;
        
        ResourcePolicy = processResourcePolicy ?? ProcessResourcePolicy.Default;
        
        RedirectStandardInput = redirectStandardInput;
        RedirectStandardOutput = redirectStandardOutput;
        RedirectStandardError = redirectStandardError;

        StandardInput = standardInput ?? StreamWriter.Null;
        StandardOutput = standardOutput ?? StreamReader.Null;
        StandardError = standardError ?? StreamReader.Null;
        
        UseShellExecution = useShellExecution;
        WindowCreation = windowCreation;
            
        StandardInputEncoding = standardInputEncoding ?? Encoding.Default;
        StandardOutputEncoding = standardOutputEncoding ?? Encoding.Default;
        StandardErrorEncoding = standardErrorEncoding ?? Encoding.Default;
    }
    
    /// <summary>
    /// Whether administrator privileges should be used when executing the Command.
    /// </summary>
    public bool RequiresAdministrator { get;protected set; }

    /// <summary>
    /// The file path of the executable to be run and wrapped.
    /// </summary>
    public string TargetFilePath { get; set; }

    /// <summary>
    /// The working directory path to be used when executing the Command.
    /// </summary>
    public string WorkingDirectoryPath { get; protected set; }

    /// <summary>
    /// The arguments to be provided to the executable to be run.
    /// </summary>
    public string Arguments { get; protected set;  }

    /// <summary>
    /// Whether to enable window creation or not when the Command's Process is run.
    /// </summary>
    public bool WindowCreation { get; protected set; }
        
    /// <summary>
    /// The environment variables to be set.
    /// </summary>
    public IReadOnlyDictionary<string, string> EnvironmentVariables { get; protected set;  }
        
    /// <summary>
    /// The credential to be used when executing the Command.
    /// </summary>
    public UserCredential Credential { get; protected set;  }

    /// <summary>
    /// Whether to use Shell Execution or not when executing the Command.
    /// </summary>
    /// <remarks>Using Shell Execution whilst also Redirecting Standard Input will throw an Exception. This is a known issue with the System Process class.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandarderror" />
    public bool UseShellExecution { get; protected set; }
        
    /// <summary>
    /// The Standard Input source to redirect Standard Input to if configured.
    /// </summary>
    /// <remarks>Using Shell Execution whilst also Redirecting Standard Input will throw an Exception. This is a known issue with the System Process class.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandarderror" />
    public StreamWriter? StandardInput { get; protected set;  }

    /// <summary>
    /// The Standard Output target to redirect Standard Output to if configured.
    /// </summary>
    public StreamReader? StandardOutput { get; protected set;  }

    /// <summary>
    /// The Standard Error target to redirect Standard Output to if configured.
    /// </summary>
    public StreamReader? StandardError { get; protected set;  }

    /// <summary>
    /// Whether to redirect the Standard Input.
    /// </summary>
    public bool RedirectStandardInput { get; protected set; }
    
    /// <summary>
    /// Whether to redirect the Standard Output.
    /// </summary>
    public bool RedirectStandardOutput { get; protected set; }
    
    /// <summary>
    /// Whether to redirect the Standard Error.
    /// </summary>
    public bool RedirectStandardError { get; protected set; }
    
    /// <summary>
    /// The Process Resource Policy to be used for executing the Command.
    /// </summary>
    /// <remarks>Process Resource Policy objects enable configuring Processor Affinity and other resource settings to be applied to the Command if supported by the currently running operating system.
    /// <para>Not all properties of a Process Resource Policy support all operating systems. Check before configuring a property.</para></remarks>
    public ProcessResourcePolicy ResourcePolicy { get; protected set;  }
    
    /// <summary>
    /// The encoding to use for the Standard Input.
    /// </summary>
    public Encoding StandardInputEncoding { get; protected set;  }
        
    /// <summary>
    /// The encoding to use for the Standard Output.
    /// </summary>
    public Encoding StandardOutputEncoding { get; protected set;  }
        
    /// <summary>
    /// The encoding to use for the Standard Error.
    /// </summary>
    public Encoding StandardErrorEncoding { get; protected set;  }
    
    /// <summary>
    /// Determines if a Process configuration is equal to another Process configuration.
    /// </summary>
    /// <param name="other">The other Process configuration to compare</param>
    /// <returns>True if both are equal to each other; false otherwise.</returns>
    public bool Equals(ProcessConfiguration? other)
    {
        if (other is null)
        {
            return false;
        }
        
#pragma warning disable CS8602 // Dereference of a possibly null reference.

        return TargetFilePath.Equals(other.TargetFilePath) &&
               EnvironmentVariables.Equals(other.EnvironmentVariables) &&
               Arguments.Equals(other.Arguments) &&
               ResourcePolicy.Equals(other.ResourcePolicy) &&
               WorkingDirectoryPath.Equals(other.WorkingDirectoryPath) &&
               UseShellExecution.Equals(other.UseShellExecution) &&
               Credential.Equals(other.Credential) &&
               ResourcePolicy.Equals(other.ResourcePolicy) &&
               StandardInput.Equals(other.StandardInput) &&
               StandardOutput.Equals(other.StandardOutput) &&
               StandardError.Equals(other.StandardError) &&
               RedirectStandardInput.Equals(other.RedirectStandardInput) &&
               RedirectStandardOutput.Equals(other.RedirectStandardOutput) &&
               RedirectStandardError.Equals(other.RedirectStandardError) &&
               StandardInputEncoding.Equals(other.StandardInputEncoding) &&
               StandardOutputEncoding.Equals(other.StandardOutputEncoding) &&
               StandardErrorEncoding.Equals(other.StandardErrorEncoding);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

    }

    /// <summary>
    /// Determines if a Process configuration is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns>True if both are equal to each other; false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
        { 
            return false;
        }

        if (obj is ProcessConfiguration other)
        {
            return Equals(other);
        }

        return false;
    }

    /// <summary>
    /// Returns the hash code for the current ProcessConfiguration.
    /// </summary>
    /// <returns>The hash code for the current ProcessConfiguration.</returns>
    public override int GetHashCode()
    {
        HashCode hashCode = new HashCode();
            
        hashCode.Add(TargetFilePath);
        hashCode.Add(Arguments);
        hashCode.Add(WorkingDirectoryPath);
        hashCode.Add(EnvironmentVariables);

        hashCode.Add(Credential);

        hashCode.Add(StandardInput);
        hashCode.Add(StandardOutput);
        hashCode.Add(StandardError);
        hashCode.Add(ResourcePolicy);
        hashCode.Add(StandardInputEncoding);
        hashCode.Add(StandardOutputEncoding);
        hashCode.Add(StandardErrorEncoding);
            
        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Determines if a Process configuration is equal to another Process configuration.
    /// </summary>
    /// <param name="left">A Process configuration to be compared.</param>
    /// <param name="right">The other Process configuration to be compared.</param>
    /// <returns>True if both Process configurations are equal to each other; false otherwise.</returns>
    public static bool Equals(ProcessConfiguration? left, ProcessConfiguration? right)
    {
        if (left is null || right is null)
        {
            return false;
        }
            
        return left.Equals(right);
    }
        
    /// <summary>
    /// Determines if a Process configuration is equal to another Process configuration.
    /// </summary>
    /// <param name="left">A Process configuration to be compared.</param>
    /// <param name="right">The other Process configuration to be compared.</param>
    /// <returns>True if both Process configurations are equal to each other; false otherwise.</returns>
    public static bool operator ==(ProcessConfiguration? left, ProcessConfiguration? right)
    {
        if (left is null || right is null)
            return false;
            
        return Equals(left, right);
    }

    /// <summary>
    /// Determines if a Process Configuration is not equal to another Process configuration.
    /// </summary>
    /// <param name="left">A Process configuration to be compared.</param>
    /// <param name="right">The other Process configuration to be compared.</param>
    /// <returns>True if both Process configurations are not equal to each other; false otherwise.</returns>
    public static bool operator !=(ProcessConfiguration? left, ProcessConfiguration? right)
    {
        if (left is null || right is null)
            return false;
            
        return Equals(left, right) == false;
    }

    /// <summary>
    /// Disposes of the disposable properties in ProcessConfiguration.
    /// </summary>
    public void Dispose()
    {
        Credential.Dispose();

        StandardInput?.Dispose();
        StandardOutput?.Dispose();
        StandardError?.Dispose();
    }
    
    /// <summary>
    /// Returns a string representation of the Command configuration.
    /// </summary>
    /// <returns>A string representation of the Command configuration.</returns>
    public override string ToString()
    {
        string commandString = $"{TargetFilePath} {Arguments}";
        string workingDirectory = string.IsNullOrEmpty(WorkingDirectoryPath) ? "" : $" ({Resources
            .Labels_ProcessConfiguration_ToString_WorkingDirectory}: {WorkingDirectoryPath})";
        string adminPrivileges = RequiresAdministrator ? $"{Environment.NewLine} {Resources
            .Labels_ProcessConfiguration_ToString_RequiresAdmin}" : "";
        string shellExecution = UseShellExecution ? $"{Environment.NewLine} {Resources
            .Labels_ProcessConfiguration_ToString_ShellExecution}" : "";

        return $"{commandString}{workingDirectory}{adminPrivileges}{shellExecution}";
    }
}