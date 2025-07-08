/*
    CliInvoke 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.

     Method signatures and field declarations from CliWrap licensed under the MIT License except where considered Copyright Fair Use by law.
     See THIRD_PARTY_NOTICES.txt for a full copy of the MIT LICENSE.
 */

using System;
using System.Collections.Generic;
using System.Globalization;

namespace AlastairLundy.CliInvoke.Core.Builders;

/// <summary>
/// An interface that defines the fluent builder methods all ArgumentsBuilder classes must implement.
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
    IArgumentsBuilder Add(IEnumerable<string> values, bool escapeSpecialChars);

    /// <summary>
    /// Appends a collection of string values to the arguments builder without escaping special characters.
    /// </summary>
    /// <param name="values">The collection of string values to append.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    IArgumentsBuilder Add(IEnumerable<string> values);

    /// <summary>
    /// Appends a formattable value to the arguments builder.
    /// </summary>
    /// <param name="value">The formattable value to append.</param>
    /// <param name="formatProvider">The format provider to use for formatting the value.</param>
    /// <param name="escapeSpecialChars">Whether to escape special characters in the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    IArgumentsBuilder Add(IFormattable value, IFormatProvider formatProvider, bool escapeSpecialChars = true);

    /// <summary>
    /// Appends a formattable value to the arguments builder using the specified culture.
    /// </summary>
    /// <param name="value">The formattable value to append.</param>
    /// <param name="cultureInfo">The culture to use for formatting the value.</param>
    /// <param name="escapeSpecialChars">Whether to escape special characters in the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    IArgumentsBuilder Add(IFormattable value, CultureInfo cultureInfo, bool escapeSpecialChars);

    /// <summary>
    /// Appends a formattable value to the arguments builder using the specified culture without escaping special characters.
    /// </summary>
    /// <param name="value">The formattable value to append.</param>
    /// <param name="cultureInfo">The culture to use for formatting the value.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    IArgumentsBuilder Add(IFormattable value, CultureInfo cultureInfo);

    /// <summary>
    /// Appends a formattable value to the arguments builder.
    /// </summary>
    /// <param name="value">The formattable value to append.</param>
    /// <param name="escapeSpecialChars">Whether to escape special characters in the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    IArgumentsBuilder Add(IFormattable value, bool escapeSpecialChars);

    /// <summary>
    /// Appends a formattable value to the arguments builder without specifying a culture and without escaping special characters.
    /// </summary>
    /// <param name="value">The formattable value to append.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    IArgumentsBuilder Add(IFormattable value);

    /// <summary>
    /// Appends a collection of formattable values to the arguments builder.
    /// </summary>
    /// <param name="values">The collection of formattable values to append.</param>
    /// <param name="formatProvider">The format provider to use for formatting the values.</param>
    /// <param name="escapeSpecialChars">Whether to escape special characters in the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    IArgumentsBuilder Add(IEnumerable<IFormattable> values, IFormatProvider formatProvider, bool escapeSpecialChars = true);

    /// <summary>
    /// Appends a collection of formattable values to the arguments builder using the specified culture.
    /// </summary>
    /// <param name="values">The collection of formattable values to append.</param>
    /// <param name="cultureInfo">The culture to use for formatting the values.</param>
    /// <param name="escapeSpecialChars">Whether to escape special characters in the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    IArgumentsBuilder Add(IEnumerable<IFormattable> values, CultureInfo cultureInfo, bool escapeSpecialChars);

    /// <summary>
    /// Appends a collection of formattable values to the arguments builder using the specified culture without escaping special characters.
    /// </summary>
    /// <param name="values">The collection of formattable values to append.</param>
    /// <param name="cultureInfo">The culture to use for formatting the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    IArgumentsBuilder Add(IEnumerable<IFormattable> values, CultureInfo cultureInfo);

    /// <summary>
    /// Appends a collection of formattable values to the arguments builder without specifying a culture.
    /// </summary>
    /// <param name="values">The collection of formattable values to append.</param>
    /// <param name="escapeSpecialChars">Whether to escape special characters in the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    IArgumentsBuilder Add(IEnumerable<IFormattable> values, bool escapeSpecialChars);

    /// <summary>
    /// Appends a collection of formattable values to the arguments builder without specifying a culture and without escaping special characters.
    /// </summary>
    /// <param name="values">The collection of formattable values to append.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    IArgumentsBuilder Add(IEnumerable<IFormattable> values);

    string EscapeSpecialCharacters(string argument);
    
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