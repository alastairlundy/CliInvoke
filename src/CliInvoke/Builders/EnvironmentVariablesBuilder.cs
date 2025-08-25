/*
    AlastairLundy.CliInvoke
     
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.

     Method signatures and field declarations from CliWrap licensed under the MIT License except where considered Copyright Fair Use by law.
     See THIRD_PARTY_NOTICES.txt for a full copy of the MIT LICENSE.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

using AlastairLundy.CliInvoke.Core.Abstractions.Builders;

#if NET8_0_OR_GREATER
using AlastairLundy.DotExtensions.Collections.Dictionaries;
#else
using AlastairLundy.DotExtensions.Collections.Generic.Dictionaries;
#endif

// ReSharper disable ArrangeObjectCreationWhenTypeEvident

namespace AlastairLundy.CliInvoke.Builders;

/// <summary>
/// A class that provides builder methods for constructing Environment Variables.
/// </summary>
public class EnvironmentVariablesBuilder : IEnvironmentVariablesBuilder
{
    private readonly Dictionary<string, string> _environmentVariables;

    /// <summary>
    /// Initializes a new instance of the EnvironmentVariablesBuilder class.
    /// </summary>
    public EnvironmentVariablesBuilder()
    {
      _environmentVariables  = new Dictionary<string, string>(StringComparer.Ordinal);
    }
        
    /// <summary>
    /// Initializes a new instance of the EnvironmentVariablesBuilder class.
    /// </summary>
    /// <param name="vars">The initial environment variables to use.</param>
    protected EnvironmentVariablesBuilder(IDictionary<string, string> vars)
    {
        _environmentVariables = new Dictionary<string, string>(vars, StringComparer.Ordinal);
    }
        
    /// <summary>
    /// Sets a single environment variable.
    /// </summary>
    /// <param name="name">The name of the environment variable to set.</param>
    /// <param name="value">The value of the environment variable to set.</param>
    /// <returns>A new instance of the IEnvironmentVariablesBuilder with the updated environment variables.</returns>
    [Pure]
    public IEnvironmentVariablesBuilder Set(string name, string value){
        Dictionary<string, string> output = new Dictionary<string, string>(_environmentVariables, StringComparer.Ordinal) { { name, value } };

        return new EnvironmentVariablesBuilder(output);
    }

    /// <summary>
    /// Sets multiple environment variables.
    /// </summary>
    /// <param name="variables">The environment variables to set.</param>
    /// <returns>A new instance of the IEnvironmentVariablesBuilder with the updated environment variables.</returns>
    [Pure]
    public IEnvironmentVariablesBuilder Set(IEnumerable<KeyValuePair<string, string>> variables)
    {
        Dictionary<string, string> output = new Dictionary<string, string>(_environmentVariables, StringComparer.Ordinal);
        output.AddRange(variables);
        
        return new EnvironmentVariablesBuilder(output);
    }

    /// <summary>
    /// Sets multiple environment variables from a read-only dictionary.
    /// </summary>
    /// <param name="variables">The read-only dictionary of environment variables to set.</param>
    /// <returns>A new instance of the IEnvironmentVariablesBuilder with the updated environment variables.</returns>
    [Pure]
    public IEnvironmentVariablesBuilder Set(IReadOnlyDictionary<string, string> variables)
    {
        Dictionary<string, string> output = new Dictionary<string, string>(_environmentVariables, StringComparer.Ordinal);
        output.AddRange(variables);

        return new EnvironmentVariablesBuilder(output);
    }

    /// <summary>
    /// Builds the dictionary of configured environment variables.
    /// </summary>
    /// <returns>A read-only dictionary containing the configured environment variables.</returns>
    public IReadOnlyDictionary<string, string> Build() => _environmentVariables;

    /// <summary>
    /// Deletes the environment variable values.
    /// </summary>
    public void Clear() => _environmentVariables.Clear();
}