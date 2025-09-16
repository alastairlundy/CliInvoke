/*
    AlastairLundy.CliInvoke.Core
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */


using System.Collections.Generic;

namespace AlastairLundy.CliInvoke.Core.Abstractions.Builders;

public interface IEnvironmentVariablesBuilder
{
    /// <summary>
    /// Sets a single environment variable.
    /// </summary>
    /// <param name="name">The name of the environment variable to set.</param>
    /// <param name="value">The value of the environment variable to set.</param>
    /// <returns>A new instance of the IEnvironmentVariablesBuilder with the updated environment variables.</returns>
    IEnvironmentVariablesBuilder Set(string name, string value);

    /// <summary>
    /// Sets multiple environment variables.
    /// </summary>
    /// <param name="variables">The environment variables to set.</param>
    /// <returns>A new instance of the IEnvironmentVariablesBuilder with the updated environment variables.</returns>
    IEnvironmentVariablesBuilder Set(IEnumerable<KeyValuePair<string, string>> variables);

    /// <summary>
    /// Sets multiple environment variables from a read-only dictionary.
    /// </summary>
    /// <param name="variables">The read-only dictionary of environment variables to set.</param>
    /// <returns>A new instance of the IEnvironmentVariablesBuilder with the updated environment variables.</returns>
    IEnvironmentVariablesBuilder Set(IReadOnlyDictionary<string, string> variables);

    /// <summary>
    /// Builds the dictionary of configured environment variables.
    /// </summary>
    /// <returns>A read-only dictionary containing the configured environment variables.</returns>
    IReadOnlyDictionary<string, string> Build();

    /// <summary>
    /// Deletes the environment variable values.
    /// </summary>
    void Clear();
}