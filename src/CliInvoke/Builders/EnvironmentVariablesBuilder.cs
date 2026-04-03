/*
    CliInvoke

    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.

     Method signatures and field declarations from CliWrap licensed under the MIT License except where considered Copyright Fair Use by law.
     See THIRD_PARTY_NOTICES.txt for a full copy of the MIT LICENSE.
 */

namespace CliInvoke.Builders;

/// <summary>
///     A class that provides builder methods for constructing Environment Variables.
/// </summary>
public class EnvironmentVariablesBuilder : IEnvironmentVariablesBuilder
{
    private readonly Dictionary<string, string> _environmentVariables;
    private readonly StringComparer _stringComparer;
    private readonly bool _throwExceptionIfDuplicateKeyFound;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EnvironmentVariablesBuilder" /> class.
    /// </summary>
    /// <param name="throwExceptionIfDuplicateKeyFound">
    ///     Whether to throw an exception if a duplicate key is
    ///     found or suppress the exception and override the previous value.
    /// </param>
    public EnvironmentVariablesBuilder(bool throwExceptionIfDuplicateKeyFound = true)
    {
        _throwExceptionIfDuplicateKeyFound = throwExceptionIfDuplicateKeyFound;
        _stringComparer = StringComparer.Ordinal;
        _environmentVariables = new Dictionary<string, string>(_stringComparer);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EnvironmentVariablesBuilder" /> class.
    /// </summary>
    /// <param name="stringComparer">The <see cref="StringComparer" /> to use for the internal dictionary.</param>
    /// <param name="throwExceptionIfDuplicateKeyFound">
    ///     Whether to throw an exception if a duplicate key is
    ///     found or suppress the exception and override the previous value.
    /// </param>
    public EnvironmentVariablesBuilder(StringComparer stringComparer,
        bool throwExceptionIfDuplicateKeyFound = true)
    {
        ArgumentNullException.ThrowIfNull(stringComparer);

        _stringComparer = stringComparer;
        _throwExceptionIfDuplicateKeyFound = throwExceptionIfDuplicateKeyFound;
        _environmentVariables = new Dictionary<string, string>(_stringComparer);
    }

    /// <summary>
    ///     Initializes a new instance of the EnvironmentVariablesBuilder class.
    /// </summary>
    /// <param name="vars">The initial environment variables to use.</param>
    /// <param name="stringComparer">The string comparer to use.</param>
    /// <param name="throwExceptionIfDuplicateKeyFound"></param>
    protected EnvironmentVariablesBuilder(
        IDictionary<string, string> vars,
        StringComparer stringComparer,
        bool throwExceptionIfDuplicateKeyFound)
    {
        ArgumentNullException.ThrowIfNull(vars);
        ArgumentNullException.ThrowIfNull(stringComparer);

        _environmentVariables = new Dictionary<string, string>(vars, _stringComparer);
        _stringComparer = stringComparer;
        _throwExceptionIfDuplicateKeyFound = throwExceptionIfDuplicateKeyFound;
    }

    /// <summary>
    ///     Sets a single environment variable.
    /// </summary>
    /// <param name="name">The name of the environment variable to set.</param>
    /// <param name="value">The value of the environment variable to set.</param>
    /// <returns>A new instance of the IEnvironmentVariablesBuilder with the updated environment variables.</returns>
    public IEnvironmentVariablesBuilder SetPair(string name, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(value);

        if (_throwExceptionIfDuplicateKeyFound)
        {
            _environmentVariables.Add(name, value);
        }
        else
        {
            bool result = _environmentVariables.TryAdd(name, value);

            if (!result)
                _environmentVariables[name] = value;
        }

        return this;
    }

    /// <summary>
    ///     Sets multiple environment variables.
    /// </summary>
    /// <param name="variables">The environment variables to set.</param>
    /// <returns>A new instance of the IEnvironmentVariablesBuilder with the updated environment variables.</returns>
    public IEnvironmentVariablesBuilder SetEnumerable(
        IEnumerable<KeyValuePair<string, string>> variables) =>
        SetInternal(variables);

    /// <summary>
    ///     Sets multiple environment variables from a dictionary.
    /// </summary>
    /// <param name="variables">The dictionary of environment variables to set.</param>
    /// <returns>A new instance of the IEnvironmentVariablesBuilder with the updated environment variables.</returns>
    public IEnvironmentVariablesBuilder SetDictionary(IDictionary<string, string> variables) =>
        SetInternal(variables);

    /// <summary>
    ///     Sets multiple environment variables from a read-only dictionary.
    /// </summary>
    /// <param name="variables">The read-only dictionary of environment variables to set.</param>
    /// <returns>A new instance of the IEnvironmentVariablesBuilder with the updated environment variables.</returns>
    public IEnvironmentVariablesBuilder SetReadOnlyDictionary(
        IReadOnlyDictionary<string, string> variables) =>
        SetInternal(variables);

    /// <summary>
    ///     Builds the dictionary of configured environment variables.
    /// </summary>
    /// <returns>A read-only dictionary containing the configured environment variables.</returns>
    public IReadOnlyDictionary<string, string> Build()
    {
        return _environmentVariables;
    }

    /// <summary>
    ///     Deletes the environment variable values.
    /// </summary>
    public void Clear()
    {
        _environmentVariables.Clear();
    }

    private IEnvironmentVariablesBuilder SetInternal(
        IEnumerable<KeyValuePair<string, string>> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);

        foreach (KeyValuePair<string, string> pair in variables)
        {
            ArgumentException.ThrowIfNullOrEmpty(pair.Key);
            ArgumentException.ThrowIfNullOrEmpty(pair.Value);

            if (_throwExceptionIfDuplicateKeyFound)
            {
                _environmentVariables.Add(pair.Key, pair.Value);
            }
            else
            {
                bool result = _environmentVariables.TryAdd(pair.Key, pair.Value);

                if (!result)
                    _environmentVariables[pair.Key] = pair.Value;
            }
        }

        return this;
    }
}