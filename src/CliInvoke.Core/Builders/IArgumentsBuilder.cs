/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.

     Method signatures and field declarations from CliWrap licensed under the MIT License except where considered Copyright Fair Use by law.
     See THIRD_PARTY_NOTICES.txt for a full copy of the MIT LICENSE.
 */

namespace CliInvoke.Core.Builders;

/// <summary>
///     An interface that defines the fluent builder methods for building arguments and escaping them
///     (as needed).
/// </summary>
public interface IArgumentsBuilder
{
    /// <summary>
    ///     Appends a string value to the arguments builder.
    /// </summary>
    /// <param name="value">The string value to append.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    IArgumentsBuilder Add(string value);

    /// <summary>
    ///     Appends a collection of string values to the arguments builder.
    /// </summary>
    /// <param name="values">The collection of string values to append.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    IArgumentsBuilder AddRange(IEnumerable<string> values);

    /// <summary>
    ///     Appends a formattable value to the arguments builder.
    /// </summary>
    /// <param name="value">The formattable value to append.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    IArgumentsBuilder Add(IFormattable value);

    /// <summary>
    ///     Appends a collection of formattable values to the arguments builder without specifying a
    ///     culture.
    /// </summary>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    /// <summary>
    ///     Escapes special characters in an argument such that it is suitable to be used with a Process.
    /// </summary>
    /// <param name="argument">The argument to escape</param>
    /// <returns>The argument with special characters escaped.</returns>
    string EscapeCharacters(string argument);
    IArgumentsBuilder AddRange(IEnumerable<IFormattable> values);
    
    /// <summary>
    ///     Builds the arguments into a string.
    /// </summary>
    /// <returns>The arguments as a string.</returns>
    string ToString();

    /// <summary>
    ///     Clears the provided argument strings.
    /// </summary>
    void Clear();
}