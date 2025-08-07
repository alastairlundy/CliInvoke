/*
    AlastairLundy.CliInvoke.Core 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

using System.Text;

using AlastairLundy.CliInvoke.Core.Internal;

// ReSharper disable MemberCanBePrivate.Global

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace AlastairLundy.CliInvoke.Core.Primitives;

/// <summary>
/// A class to store Process configuration information.
/// </summary>
public class ProcessConfiguration : IEquatable<ProcessConfiguration>, IDisposable
{
    /// <summary>
    /// Configures this Command configuration with the specified Command configuration.
    /// </summary>
    /// <param name="processConfiguration">The command configuration to be used to configure the Command to be run.</param>
#if NET5_0_OR_GREATER
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
#endif
    public ProcessConfiguration(ProcessConfiguration processConfiguration)
    {
        TargetFilePath = processConfiguration.TargetFilePath;
        Arguments = processConfiguration.Arguments; 
        WorkingDirectoryPath = processConfiguration.WorkingDirectoryPath;
        RequiresAdministrator = processConfiguration.RequiresAdministrator;
        EnvironmentVariables = processConfiguration.EnvironmentVariables;
        Credential = processConfiguration.Credential ?? UserCredential.Null;
        StandardInput = processConfiguration.StandardInput ?? StreamWriter.Null;
        StandardOutput = processConfiguration.StandardOutput ?? StreamReader.Null;
        StandardError = processConfiguration.StandardError ?? StreamReader.Null;
            
        StandardInputEncoding = processConfiguration.StandardInputEncoding;
        StandardOutputEncoding = processConfiguration.StandardOutputEncoding;
        StandardErrorEncoding = processConfiguration.StandardErrorEncoding;
            
        ResourcePolicy = processConfiguration.ResourcePolicy ?? ProcessResourcePolicy.Default;
        
        WindowCreation = processConfiguration.WindowCreation;
        UseShellExecution = processConfiguration.UseShellExecution;
    }

    /// <summary>
    /// Configures the Command configuration to be wrapped and executed.
    /// </summary>
    /// <param name="targetFilePath">The target file path of the command to be executed.</param>
    /// <param name="arguments">The arguments to pass to the Command upon execution.</param>
    /// <param name="workingDirectoryPath">The working directory to be used.</param>
    /// <param name="requiresAdministrator">Whether to run the Command with administrator privileges.</param>
    /// <param name="environmentVariables">The environment variables to be set (if specified).</param>
    /// <param name="credential">The credential to be used (if specified).</param>
    /// <param name="commandResultValidation">Whether to perform Result Validation and exception throwing if the Command exits with an exit code other than 0.</param>
    /// <param name="standardInput">The standard input source to be used (if specified).</param>
    /// <param name="standardOutput">The standard output destination to be used (if specified).</param>
    /// <param name="standardError">The standard error destination to be used (if specified).</param>
    /// <param name="standardInputEncoding">The Standard Input Encoding to be used (if specified).</param>
    /// <param name="standardOutputEncoding">The Standard Output Encoding to be used (if specified).</param>
    /// <param name="standardErrorEncoding">The Standard Error Encoding to be used (if specified).</param>
    /// <param name="processResourcePolicy">The process resource policy to be used (if specified).</param>
    /// <param name="processTimeoutPolicy"></param>
    /// <param name="windowCreation">Whether to enable or disable Window Creation of the Command's Process.</param>
    /// <param name="useShellExecution">Whether to enable or disable executing the Command through Shell Execution.</param>
#if NET5_0_OR_GREATER
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
#endif
    public ProcessConfiguration(string targetFilePath,
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
    /// Instantiates the Process Configuration class with a ProcessStartInfo and other optional parameters.
    /// </summary>
    /// <param name="processStartInfo"></param>
    /// <param name="environmentVariables">The environment variables to be set (if specified).</param>
    /// <param name="credential">The credential to be used (if specified).</param>
    /// <param name="resultValidation">Whether to perform Result Validation and exception throwing if the Command exits with an exit code other than 0.</param>
    /// <param name="standardInput">The standard input source to be used (if specified).</param>
    /// <param name="standardOutput">The standard output destination to be used (if specified).</param>
    /// <param name="standardError">The standard error destination to be used (if specified).</param>
    /// <param name="processResourcePolicy">The process resource policy to be used (if specified).</param>
    /// <param name="processTimeoutPolicy"></param>
#if NET5_0_OR_GREATER
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
#endif
    public ProcessConfiguration(ProcessStartInfo processStartInfo,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        UserCredential? credential = null,
        StreamWriter? standardInput = null,
        StreamReader? standardOutput = null,
        StreamReader? standardError = null,
        ProcessResourcePolicy? processResourcePolicy = null)
    {
        EnvironmentVariables = environmentVariables ?? new Dictionary<string, string>();
                
        Credential = credential ?? UserCredential.Null;
            
        ResourcePolicy = processResourcePolicy ?? ProcessResourcePolicy.Default;
        
        StandardInput = standardInput ?? StreamWriter.Null;
        StandardOutput = standardOutput ?? StreamReader.Null;
        StandardError = standardError ?? StreamReader.Null;
        
        StandardInputEncoding = Encoding.Default;
        StandardOutputEncoding = Encoding.Default;
        StandardErrorEncoding = Encoding.Default;
            
        TargetFilePath = processStartInfo.FileName;
        Arguments = processStartInfo.Arguments;
        WorkingDirectoryPath = processStartInfo.WorkingDirectory;
    }
                
    /// <summary>
    /// Whether administrator privileges should be used when executing the Command.
    /// </summary>
    public bool RequiresAdministrator { get;protected set; }

    /// <summary>
    /// The file path of the executable to be run and wrapped.
    /// </summary>
    public string TargetFilePath { get; protected set; }

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
    public UserCredential? Credential { get; protected set;  }

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
    /// The Process Resource Policy to be used for executing the Command.
    /// </summary>
    /// <remarks>Process Resource Policy objects enable configuring Processor Affinity and other resource settings to be applied to the Command if supported by the currently running operating system.
    /// <para>Not all properties of a Process Resource Policy support all operating systems. Check before configuring a property.</para></remarks>
    public ProcessResourcePolicy? ResourcePolicy { get; protected set;  }
    
    /// <summary>
    /// The encoding to use for the Standard Input.
    /// </summary>
    /// <remarks>This is ignored on .NET Standard 2.0 as it is unsupported on that Target Framework's Process class.</remarks>
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

        if (Credential is not null &&
            other.Credential is not null &&
            TargetFilePath != other.TargetFilePath &&
            ResourcePolicy is not null)
        {

            if (StandardOutput is not null && StandardError is not null)
            {
                return TargetFilePath.Equals(other.TargetFilePath) &&
                       EnvironmentVariables.Equals(other.EnvironmentVariables) &&
                       Credential.Equals(other.Credential) &&
                       ResourcePolicy.Equals(other.ResourcePolicy) &&
                       StandardOutput.Equals(other.StandardOutput) &&
                       StandardError.Equals(other.StandardError) &&
                       StandardInputEncoding.Equals(other.StandardInputEncoding) &&
                       StandardOutputEncoding.Equals(other.StandardOutputEncoding) &&
                       StandardErrorEncoding.Equals(other.StandardErrorEncoding);   
            }
            else
            {
                return TargetFilePath.Equals(other.TargetFilePath) &&
                       EnvironmentVariables.Equals(other.EnvironmentVariables) &&
                       Credential.Equals(other.Credential) &&
                       ResourcePolicy.Equals(other.ResourcePolicy) &&
                       StandardInputEncoding.Equals(other.StandardInputEncoding) &&
                       StandardOutputEncoding.Equals(other.StandardOutputEncoding) &&
                       StandardErrorEncoding.Equals(other.StandardErrorEncoding);
            }
                                
        }
        else
        {
            return TargetFilePath.Equals(other.TargetFilePath) &&
                   EnvironmentVariables.Equals(other.EnvironmentVariables) &&
                   StandardInputEncoding.Equals(other.StandardInputEncoding) &&
                   StandardOutputEncoding.Equals(other.StandardOutputEncoding) &&
                   StandardErrorEncoding.Equals(other.StandardErrorEncoding);
        }
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
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Returns the hash code for the current ProcessConfiguration.
    /// </summary>
    /// <returns>The hash code for the current ProcessConfiguration.</returns>
    public override int GetHashCode()
    {
        HashCode hashCode = new HashCode();
            
        hashCode.Add(TargetFilePath);
        hashCode.Add(EnvironmentVariables);

        if (Credential is not null)
        {
            hashCode.Add(Credential);
        }
            
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
        {
            return false;
        }
            
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
        {
            return false;
        }
            
        return Equals(left, right) == false;
    }

    /// <summary>
    /// Disposes of the disposable properties in ProcessConfiguration.
    /// </summary>
    public void Dispose()
    {
        if (Credential is not null)
        {
            Credential.Dispose();
        }

        if (StandardInput is not null)
        { 
            StandardInput.Dispose();
        }

        if (StandardOutput is not null)
        {
            StandardOutput.Dispose();
        }

        if (StandardError is not null)
        {
            StandardError.Dispose();
        }
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
        
    /// <summary>
    /// Creates Process Start Information based on this Process configuration's values.
    /// </summary>
    /// <returns>A new ProcessStartInfo object configured with the specified Process object values.</returns>
    /// <exception cref="ArgumentException">Thrown if the Target File Path is null or empty.</exception>
#if NET5_0_OR_GREATER
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
#endif
    public ProcessStartInfo ToProcessStartInfo()
    {
        bool redirectStandardInput = StandardInput is not null && StandardInput != StreamWriter.Null;
        bool redirectStandardError = StandardError is not null && StandardOutput != StreamReader.Null;
        bool redirectStandardOutput = StandardOutput is not null && StandardError != StreamReader.Null;
            
        return ToProcessStartInfo(redirectStandardInput, redirectStandardOutput, redirectStandardError);
    }

    /// <summary>
    /// Creates Process Start Information based on specified parameters and Process configuration object values.
    /// </summary>
    /// <param name="redirectStandardInput"></param>
    /// <param name="redirectStandardOutput">Whether to redirect the Standard Output.</param>
    /// <param name="redirectStandardError">Whether to redirect the Standard Error.</param>
    /// <returns>A new ProcessStartInfo object configured with the specified parameters and Process object values.</returns>
    /// <exception cref="ArgumentException">Thrown if the process configuration's Target File Path is null or empty.</exception>
#if NET5_0_OR_GREATER
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
#endif
    public ProcessStartInfo ToProcessStartInfo(bool redirectStandardInput, bool redirectStandardOutput, bool redirectStandardError)
    {
        if (string.IsNullOrEmpty(TargetFilePath))
        {
            throw new ArgumentException(Resources.Exceptions_ProcessConfiguration_TargetFilePath_Empty);
        }
            
        ProcessStartInfo output = new ProcessStartInfo()
        {
            FileName = TargetFilePath,
            WorkingDirectory = WorkingDirectoryPath,
            UseShellExecute = UseShellExecution,
            CreateNoWindow = WindowCreation,
            RedirectStandardInput = redirectStandardInput,
            RedirectStandardOutput = redirectStandardOutput,
            RedirectStandardError = redirectStandardError,
        };

        if (string.IsNullOrEmpty(Arguments) == false)
        {
            output.Arguments = Arguments;
        }
            
        if (RequiresAdministrator)
        {
            output.RunAsAdministrator();
        }

        if (Credential is not null)
        {
            output.TryApplyUserCredential(Credential);
        }

        if (EnvironmentVariables.Any())
        {
            output.ApplyEnvironmentVariables(EnvironmentVariables);
        }
            
        if (output.RedirectStandardInput)
        {
#if NETSTANDARD2_1 || NET5_0_OR_GREATER
            output.StandardInputEncoding = StandardInputEncoding;
#endif
        }

        output.StandardOutputEncoding = StandardOutputEncoding;
        output.StandardErrorEncoding = StandardErrorEncoding;
            
        return output;
    }
}