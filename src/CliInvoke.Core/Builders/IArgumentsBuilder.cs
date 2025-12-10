/*
    CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.

     Method signatures and field declarations from CliWrap licensed under the MIT License except where considered Copyright Fair Use by law.
     See THIRD_PARTY_NOTICES.txt for a full copy of the MIT LICENSE.
 */

using System.Collections.Generic;

namespace CliInvoke.Core.Builders;

/// <summary>
/// An interface that defines the fluent builder methods for building arguments and escaping them (as needed).
/// </summary>
public interface IArgumentsBuilder
{
    /// <summary>
    /// Appends a string value to the arguments builder.
    /// </summary>
    /// <param name="value">The string value to append.</param>
    /// <param name="escape">True to escape special characters in the value, false otherwise.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    IArgumentsBuilder Add(string value, bool escape);

    /// <summary>
    /// Appends a string value to the arguments builder without escaping special characters.
    /// </summary>
    /// <param name="value">The string value to append.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    IArgumentsBuilder Add(string value);

    /// <summary>
    /// Appends a collection of string values to the arguments builder.
    /// </summary>
    /// <param name="values">The collection of string values to append.</param>
    /// <param name="escapeSpecialChars">Whether to escape special characters in the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    IArgumentsBuilder AddEnumerable(IEnumerable<string> values, bool escapeSpecialChars);

    /// <summary>
    /// Appends a collection of string values to the arguments builder without escaping special characters.
    /// </summary>
    /// <param name="values">The collection of string values to append.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    IArgumentsBuilder AddEnumerable(IEnumerable<string> values);

    /// <summary>
    /// Appends a formattable value to the arguments builder without specifying a culture and without escaping special characters.
    /// </summary>
    /// <param name="value">The formattable value to append.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    IArgumentsBuilder Add(IFormattable value);

    /// <summary>
    /// Appends a formattable value to the arguments builder.
    /// </summary>
    /// <param name="value">The formattable value to append.</param>
    /// <param name="escapeSpecialChars">Whether to escape special characters in the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    IArgumentsBuilder Add(IFormattable value, bool escapeSpecialChars);

    /// <summary>
    /// Appends a formattable value to the arguments builder.
    /// </summary>
    /// <param name="value">The formattable value to append.</param>
    /// <param name="formatProvider">The format provider to use for formatting the value.</param>
    /// <param name="format"></param>
    /// <param name="escapeSpecialChars">Whether to escape special characters in the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    IArgumentsBuilder Add(
        IFormattable value,
        IFormatProvider formatProvider,
        string? format = null,
        bool escapeSpecialChars = true
    );

    /// <summary>
    /// Appends a collection of formattable values to the arguments builder without specifying a culture and without escaping special characters.
    /// </summary>
    /// <param name="values">The collection of formattable values to append.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    IArgumentsBuilder AddEnumerable(IEnumerable<IFormattable> values);

    /// <summary>
    /// Appends a collection of formattable values to the arguments builder without specifying a culture.
    /// </summary>
    /// <param name="values">The collection of formattable values to append.</param>
    /// <param name="escapeSpecialChars">Whether to escape special characters in the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    IArgumentsBuilder AddEnumerable(IEnumerable<IFormattable> values, bool escapeSpecialChars);

    /// <summary>
    /// Appends a collection of formattable values to the arguments builder.
    /// </summary>
    /// <param name="values">The collection of formattable values to append.</param>
    /// <param name="formatProvider">The format provider to use for formatting the values.</param>
    /// <param name="format"></param>
    /// <param name="escapeSpecialChars">Whether to escape special characters in the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    IArgumentsBuilder AddEnumerable(
        IEnumerable<IFormattable> values,
        IFormatProvider formatProvider,
        string? format = null,
        bool escapeSpecialChars = true
    );

    /// <summary>
    /// Escapes special characters in an argument such that it is suitable to be used with a Process.
    /// </summary>
    /// <param name="argument">The argument to escape</param>
    /// <returns>The argument with special characters escaped.</returns>
    string EscapeCharacters(string argument);

    /// <summary>
    /// Builds the arguments into a string.
    /// </summary>
    /// <returns>The arguments as a string.</returns>
    string ToString();

    /// <summary>
    /// Clears the provided argument strings.
    /// </summary>
    void Clear();
}
