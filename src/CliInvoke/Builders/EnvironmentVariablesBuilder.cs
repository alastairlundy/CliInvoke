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

using AlastairLundy.CliInvoke.Core.Builders;

// ReSharper disable ArrangeObjectCreationWhenTypeEvident
// ReSharper disable RedundantExplicitArrayCreation

namespace AlastairLundy.CliInvoke.Builders;

/// <summary>
/// A class that provides builder methods for constructing Environment Variables.
/// </summary>
public class EnvironmentVariablesBuilder : IEnvironmentVariablesBuilder
{
    private readonly StringComparer _stringComparer;
    private readonly bool _throwExceptionIfDuplicateKeyFound;
    private readonly Dictionary<string, string> _environmentVariables;
    

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentVariablesBuilder"/> class.
    /// </summary>
    /// <param name="throwExceptionIfDuplicateKeyFound">Whether to throw an exception if a duplicate key is found or suppress the exception and override the previous value.</param>
    public EnvironmentVariablesBuilder(bool throwExceptionIfDuplicateKeyFound = true)
    {
        _throwExceptionIfDuplicateKeyFound = throwExceptionIfDuplicateKeyFound;
        _stringComparer = StringComparer.Ordinal;
        _environmentVariables  = new Dictionary<string, string>(_stringComparer);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentVariablesBuilder"/> class.
    /// </summary>
    /// <param name="stringComparer"></param>
    /// <param name="throwExceptionIfDuplicateKeyFound">Whether to throw an exception if a duplicate key is found or suppress the exception and override the previous value.</param>
    public EnvironmentVariablesBuilder(StringComparer stringComparer, bool throwExceptionIfDuplicateKeyFound = true)
    {
        _stringComparer = stringComparer;
        _throwExceptionIfDuplicateKeyFound = throwExceptionIfDuplicateKeyFound;
        _environmentVariables  = new Dictionary<string, string>(_stringComparer);
    }

    /// <summary>
    /// Initializes a new instance of the EnvironmentVariablesBuilder class.
    /// </summary>
    /// <param name="vars">The initial environment variables to use.</param>
    /// <param name="stringComparer">The string comparer to use.</param>
    /// <param name="throwExceptionIfDuplicateKeyFound"></param>
    protected EnvironmentVariablesBuilder(IDictionary<string, string> vars, StringComparer stringComparer, bool throwExceptionIfDuplicateKeyFound)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(vars, nameof(vars));
#endif
        
        _environmentVariables = new Dictionary<string, string>(vars,
            _stringComparer);
        _stringComparer = stringComparer;
        _throwExceptionIfDuplicateKeyFound = throwExceptionIfDuplicateKeyFound;
    }
        
    /// <summary>
    /// Sets a single environment variable.
    /// </summary>
    /// <param name="name">The name of the environment variable to set.</param>
    /// <param name="value">The value of the environment variable to set.</param>
    /// <returns>A new instance of the IEnvironmentVariablesBuilder with the updated environment variables.</returns>
    [Pure]
    public IEnvironmentVariablesBuilder SetPair(string name, string value)
    {
        Dictionary<string, string> output = new(_environmentVariables);

        if (_throwExceptionIfDuplicateKeyFound)
        {
            output.Add(name, value);
        }
        else
        {
           bool result = output.TryAdd(name, value);

           if (result == false)
               output[name] = value;
        }

        return new EnvironmentVariablesBuilder(output,_stringComparer, _throwExceptionIfDuplicateKeyFound);
    }

    protected IEnvironmentVariablesBuilder SetInternal(IEnumerable<KeyValuePair<string, string>> variables)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(variables, nameof(variables));
#endif
        
        Dictionary<string, string> output = new Dictionary<string, string>(_environmentVariables,
            StringComparer.Ordinal);
        
        foreach (KeyValuePair<string, string> pair in variables)
        {
            if (_throwExceptionIfDuplicateKeyFound)
            {
                output.Add(pair.Key, pair.Value);
            }
            else
            {
                bool result = output.TryAdd(pair.Key, pair.Value);

                if (result == false)
                    output[pair.Key] = pair.Value;
            }
        }
        
        return new EnvironmentVariablesBuilder(output, _stringComparer, _throwExceptionIfDuplicateKeyFound);
    }

    /// <summary>
    /// Sets multiple environment variables.
    /// </summary>
    /// <param name="variables">The environment variables to set.</param>
    /// <returns>A new instance of the IEnvironmentVariablesBuilder with the updated environment variables.</returns>
    [Pure]
    public IEnvironmentVariablesBuilder SetEnumerable(IEnumerable<KeyValuePair<string, string>> variables)
        => SetInternal(variables);

    /// <summary>
    /// Sets multiple environment variables from a dictionary.
    /// </summary>
    /// <param name="variables">The dictionary of environment variables to set.</param>
    /// <returns>A new instance of the IEnvironmentVariablesBuilder with the updated environment variables.</returns>
    [Pure]
    public IEnvironmentVariablesBuilder SetDictionary(IDictionary<string, string> variables)
        => SetInternal(variables);

    /// <summary>
    /// Sets multiple environment variables from a read-only dictionary.
    /// </summary>
    /// <param name="variables">The read-only dictionary of environment variables to set.</param>
    /// <returns>A new instance of the IEnvironmentVariablesBuilder with the updated environment variables.</returns>
    [Pure]
    public IEnvironmentVariablesBuilder SetReadOnlyDictionary(IReadOnlyDictionary<string, string> variables)
        =>  SetInternal(variables);

    /// <summary>
    /// Builds the dictionary of configured environment variables.
    /// </summary>
    /// <returns>A read-only dictionary containing the configured environment variables.</returns>
    public IReadOnlyDictionary<string, string> Build() 
        => _environmentVariables;

    /// <summary>
    /// Deletes the environment variable values.
    /// </summary>
    public void Clear() 
        => _environmentVariables.Clear();
}