/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.

     Method signatures and field declarations from CliWrap licensed under the MIT License except where considered Copyright Fair Use by law.
     See THIRD_PARTY_NOTICES.txt for a full copy of the MIT LICENSE.
 */

namespace CliInvoke.Core.Configuration;

/// <summary>
///     A sealed configuration seam for constructing environment variables,
///     replacing the former EnvironmentVariablesBuilder / IEnvironmentVariablesBuilder pair.
/// </summary>
public sealed class EnvironmentVariablesSpec
{
    private readonly Dictionary<string, string> _environmentVariables;
    private readonly StringComparer _stringComparer;
    private readonly bool _throwExceptionIfDuplicateKeyFound;

    /// <summary>
    ///     Initialises a new instance of the <see cref="EnvironmentVariablesSpec" /> class
    ///     with default settings.
    /// </summary>
    public EnvironmentVariablesSpec()
    {
        _throwExceptionIfDuplicateKeyFound = true;
        _stringComparer = StringComparer.Ordinal;
        _environmentVariables = new Dictionary<string, string>(_stringComparer);
    }

    /// <summary>
    ///     Initialises a new instance of the <see cref="EnvironmentVariablesSpec" /> class.
    /// </summary>
    /// <param name="stringComparer">The <see cref="StringComparer" /> to use for the internal dictionary.</param>
    /// <param name="throwExceptionIfDuplicateKeyFound">
    ///     Whether to throw an exception if a duplicate key is
    ///     found or suppress the exception and override the previous value.
    /// </param>
    public EnvironmentVariablesSpec(StringComparer stringComparer,
        bool throwExceptionIfDuplicateKeyFound = true)
    {
        ArgumentNullException.ThrowIfNull(stringComparer);

        _stringComparer = stringComparer;
        _throwExceptionIfDuplicateKeyFound = throwExceptionIfDuplicateKeyFound;
        _environmentVariables = new Dictionary<string, string>(_stringComparer);
    }

    /// <summary>
    ///     Sets multiple environment variables.
    /// </summary>
    /// <param name="variables">The environment variables to set.</param>
    /// <returns>The current <see cref="EnvironmentVariablesSpec" /> instance.</returns>
    public EnvironmentVariablesSpec SetEnumerable(
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
}
