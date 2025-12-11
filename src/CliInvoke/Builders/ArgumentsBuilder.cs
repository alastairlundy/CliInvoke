/*
    CliInvoke 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.

     Method signatures and field declarations from CliWrap licensed under the MIT License except where considered Copyright Fair Use by law.
     See THIRD_PARTY_NOTICES.txt for a full copy of the MIT LICENSE.
 */

using System.Globalization;
using System.Linq;
using System.Text;

// ReSharper disable ConvertClosureToMethodGroup

namespace CliInvoke.Builders;

/// <summary>
/// A class that provides a fluent interface style builder for constructing Arguments to provide to a program.
/// </summary>
public class ArgumentsBuilder : IArgumentsBuilder
{
    private static readonly IFormatProvider DefaultFormatProvider = CultureInfo.InvariantCulture;

    private readonly StringBuilder _buffer;

    private readonly Func<string, bool>? _argumentValidationLogic;
    
    /// <summary>
    /// Initializes the ArgumentsBuilder.
    /// </summary>
    public ArgumentsBuilder()
    {
        _buffer = new();
    }

    /// <summary>
    /// Initializes the ArgumentsBuilder with the specified Argument Validation Logic.
    /// </summary>
    /// <param name="argumentValidationLogic">The argument validation logic to use to decide whether to allow Arguments passed to the builder.</param>
    public ArgumentsBuilder(Func<string, bool> argumentValidationLogic)
    {
        _buffer = new();
        _argumentValidationLogic = argumentValidationLogic;   
    }
    
        
    /// <summary>
    /// 
    /// </summary>
    /// <param name="buffer"></param>
    protected ArgumentsBuilder(StringBuilder buffer)
    {
        _buffer = buffer;
    }

    protected ArgumentsBuilder(StringBuilder buffer, Func<string, bool> argumentValidationLogic)
    {
        _buffer = buffer;
        _argumentValidationLogic = argumentValidationLogic;
    }

    /// <summary>
    /// Appends a string value to the arguments builder.
    /// </summary>
    /// <param name="value">The string value to append.</param>
    /// <param name="escapeSpecialCharacters">True to escape special characters in the value, false otherwise.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    [Pure]
    public IArgumentsBuilder Add(string value, bool escapeSpecialCharacters)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!IsValidArgument(value))
        {
            return this;
        }

        if (_buffer.Length is > 0 and < int.MaxValue)
        {
            // Add a space if it's missing before adding the new string.
            if (_buffer[^1] != ' ')
            {
                _buffer.Append(' ');
            }
        }

        if (_buffer.Length < _buffer.MaxCapacity && _buffer.Length < int.MaxValue)
        {
            _buffer.Append(escapeSpecialCharacters ? EscapeCharacters(value) : value);
        }
        else
        {
            throw new InvalidOperationException(Resources
                .Exceptions_ArgumentBuilder_Buffer_MaximumSize.Replace("{x}",
                    int.MaxValue.ToString()));
        }

        if (_argumentValidationLogic is not null)
        {
            return new ArgumentsBuilder(_buffer,
                _argumentValidationLogic);
        }
        else
        {
            return new ArgumentsBuilder(_buffer);
        }
    }

    /// <summary>
    /// Appends a string value to the arguments builder without escaping special characters.
    /// </summary>
    /// <param name="value">The string value to append.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    [Pure]
    public IArgumentsBuilder Add(string value) 
        => IsValidArgument(value) ? Add(value, false) : this;

    /// <summary>
    /// Appends a collection of string values to the arguments builder.
    /// </summary>
    /// <param name="values">The collection of string values to append.</param>
    /// <param name="escapeSpecialChars">Whether to escape special characters in the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    [Pure]
    public IArgumentsBuilder AddEnumerable(IEnumerable<string> values, bool escapeSpecialChars)
    {
        ArgumentNullException.ThrowIfNull(values);

        // Do not escape individual values here when escaping is requested to avoid double-escaping.
        // Instead, join the raw values and perform escaping once at the final Add call.
        IEnumerable<string> filtered = values.Where(x => IsValidArgument(x));

        string joinedValues = string.Join(" ", filtered);

        return Add(joinedValues, escapeSpecialChars);
    }

    /// <summary>
    /// Appends a collection of string values to the arguments builder without escaping special characters.
    /// </summary>
    /// <param name="values">The collection of string values to append.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    [Pure]
    public IArgumentsBuilder AddEnumerable(IEnumerable<string> values) 
        => AddEnumerable(values, true);

    /// <summary>
    /// Appends a formattable value to the arguments builder.
    /// </summary>
    /// <param name="value">The formattable value to append.</param>
    /// <param name="formatProvider">The format provider to use for formatting the value.</param>
    /// <param name="format"></param>
    /// <param name="escapeSpecialChars">Whether to escape special characters in the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    [Pure]
    public IArgumentsBuilder Add(IFormattable value, IFormatProvider formatProvider, string? format = null,
        bool escapeSpecialChars = true)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(formatProvider);
        
        if (!IsValidArgument(value, formatProvider))
            throw new ArgumentNullException(nameof(value));
        
        string valueActual = value.ToString(format, formatProvider);

        if (string.IsNullOrWhiteSpace(valueActual))
        {
            throw new NullReferenceException("IFormatProvider formated the IFormattable {x} which resulted in a null string.".Replace(
                "{x}",
                nameof(value)));
        }
           
        return Add(valueActual, escapeSpecialChars);
    }

    /// <summary>
    /// Appends a formattable value to the arguments builder using the specified culture.
    /// </summary>
    /// <param name="values"></param>
    /// <param name="formatProvider"></param>
    /// <param name="format"></param>
    /// <param name="escapeSpecialChars">Whether to escape special characters in the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    [Pure]
    public IArgumentsBuilder AddEnumerable(IEnumerable<IFormattable> values, IFormatProvider formatProvider,
        string? format = null, bool escapeSpecialChars = true)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(formatProvider);
        
        IEnumerable<string> valuesStrings = values.Select(x => x.ToString(format, formatProvider));
        
        string value = string.Join(' ', valuesStrings);
        
        return Add(value, escapeSpecialChars);
    }
    
    /// <summary>
    /// Appends a formattable value to the arguments builder.
    /// </summary>
    /// <param name="value">The formattable value to append.</param>
    /// <param name="escapeSpecialChars">Whether to escape special characters in the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    [Pure]
    public IArgumentsBuilder Add(IFormattable value, bool escapeSpecialChars) 
        => Add(value, DefaultFormatProvider, null, escapeSpecialChars);

    /// <summary>
    /// Appends a formattable value to the arguments builder without specifying a culture
    /// and without escaping special characters.
    /// </summary>
    /// <param name="value">The formattable value to append.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    [Pure]
    public IArgumentsBuilder Add(IFormattable value) 
        => Add(value, true);

    /// <summary>
    /// Appends a collection of formattable values to the arguments builder without specifying a culture.
    /// </summary>
    /// <param name="values">The collection of formattable values to append.</param>
    /// <param name="escapeSpecialChars">Whether to escape special characters in the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    [Pure]
    public IArgumentsBuilder AddEnumerable(IEnumerable<IFormattable> values, bool escapeSpecialChars) 
        => AddEnumerable(values, CultureInfo.CurrentCulture, null, escapeSpecialChars);

    /// <summary>
    /// Appends a collection of formattable values to the arguments builder without specifying a culture and without escaping special characters.
    /// </summary>
    /// <param name="values">The collection of formattable values to append.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    [Pure]
    public IArgumentsBuilder AddEnumerable(IEnumerable<IFormattable> values) 
        => AddEnumerable(values, true);

    /// <summary>
    /// Escapes characters in a string.
    /// </summary>
    /// <param name="argument">The string to escape.</param>
    /// <returns>The escaped string.</returns>
    [Pure]
    public string EscapeCharacters(string argument)
    {
        ArgumentNullException.ThrowIfNull(argument);
        
        StringBuilder stringBuilder = new();
        
        if(!argument.StartsWith('"'))
            stringBuilder.Append('"');
        
        foreach (char c in argument)
        {
            switch (c)
            {
                case '\\': stringBuilder.Append(@"\\"); break;
                case '\"':  stringBuilder.Append("\\\""); break;
                case '\n': stringBuilder.Append(@"\n");  break;
                case '\r': stringBuilder.Append(@"\r");  break;
                case '\t': stringBuilder.Append(@"\t");  break;
                case '\b': stringBuilder.Append(@"\b");  break;
                case '\f': stringBuilder.Append(@"\f");  break;
                case '\v': stringBuilder.Append(@"\v");  break;
                case '\a': stringBuilder.Append(@"\a");  break;
                case '\e': stringBuilder.Append(@"\e"); break; 
                case '\0': stringBuilder.Append(@"\0");  break;
                default:
                    if (char.IsControl(c))
                        stringBuilder.AppendFormat(@"\u{0:X4}", (int)c);
                    else
                        stringBuilder.Append(c);
                    break;
            }
        }
        
        if(!argument.EndsWith('"'))
            stringBuilder.Append('"');
        
        return stringBuilder.ToString();
    }

    /// <summary>
    /// Builds the arguments into a string.
    /// </summary>
    /// <returns>The arguments as a string.</returns>
    public new string ToString() => _buffer.ToString();

    /// <summary>
    /// Clears the provided argument strings.
    /// </summary>
    public void Clear() => _buffer.Clear();

    private bool IsValidArgument(string value)
    {
        bool output;
        
        if (_argumentValidationLogic is not null)
        {
            output = _argumentValidationLogic.Invoke(value);
        }
        else
        {
            output = !string.IsNullOrEmpty(value);
        }
        
        return output;
    }

    private bool IsValidArgument(IFormattable value, IFormatProvider provider) 
        => IsValidArgument(value.ToString(null, provider));
}