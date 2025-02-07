﻿/*
    CliRunner 
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
using System.Globalization;
using System.Linq;
using System.Text;
using CliRunner.Builders.Abstractions;
using CliRunner.Extensions;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable RedundantBoolCompare

#nullable enable

namespace CliRunner.Builders;

/// <summary>
/// A class that provides a fluent interface style builder for constructing Arguments to provide to a program.
/// </summary>
public class ArgumentsBuilder : IArgumentsBuilder
{
    private static readonly IFormatProvider DefaultFormatProvider = CultureInfo.InvariantCulture;

    private readonly StringBuilder _buffer;

    private readonly Func<string, bool>? _argumentValidationLogic;
    
    /// <summary>
    /// Initialises the ArgumentsBuilder.
    /// </summary>
    public ArgumentsBuilder()
    {
        _buffer = new StringBuilder();
    }

    /// <summary>
    /// Initialises the ArgumentsBuilder with the specified Argument Validation Logic.
    /// </summary>
    /// <param name="argumentValidationLogic">The argument validation logic to use to decide whether to allow Arguments passed to the builder.</param>
    public ArgumentsBuilder(Func<string, bool> argumentValidationLogic)
    {
        _buffer = new StringBuilder();
        _argumentValidationLogic = argumentValidationLogic;   
    }
    
        
    /// <summary>
    /// 
    /// </summary>
    /// <param name="buffer"></param>
    private ArgumentsBuilder(StringBuilder buffer)
    {
        _buffer = buffer;
    }

    private ArgumentsBuilder(StringBuilder buffer, Func<string, bool> argumentValidationLogic)
    {
        _buffer = buffer;
        _argumentValidationLogic = argumentValidationLogic;
    }

    /// <summary>
    /// Appends a string value to the arguments builder.
    /// </summary>
    /// <param name="value">The string value to append.</param>
    /// <param name="escape">True to escape special characters in the value, false otherwise.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    [Pure]
    public IArgumentsBuilder Add(string value, bool escape)
    {
        if (IsValidArgument(value) == true)
        {
            _buffer.Append(value);

            if (escape)
            {
                _buffer.Append(EscapeSpecialChars(value));
            }
            
            if (_argumentValidationLogic != null)
            {
                return new ArgumentsBuilder(_buffer, _argumentValidationLogic);
            }
            else
            {
                return new ArgumentsBuilder(_buffer);   
            }
        }
        else
        {
            return this;   
        }
    }

    /// <summary>
    /// Appends a string value to the arguments builder without escaping special characters.
    /// </summary>
    /// <param name="value">The string value to append.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    [Pure]
    public IArgumentsBuilder Add(string value)
    {
        if (IsValidArgument(value) == true )
        {
            return Add(value, false);
        }
        else
        {
            return this;
        }
    }

    /// <summary>
    /// Appends a collection of string values to the arguments builder.
    /// </summary>
    /// <param name="values">The collection of string values to append.</param>
    /// <param name="escapeSpecialChars">Whether to escape special characters in the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    [Pure]
    public IArgumentsBuilder Add(IEnumerable<string> values, bool escapeSpecialChars)
    {
        string[] enumerable = values as string[] ?? values.ToArray();
        
            for(int index = 0; index < enumerable.Length; index++)
            {
                if (IsValidArgument(enumerable[index]) == true)
                {
                    _buffer.Append(enumerable[index]);
            
                    if (escapeSpecialChars)
                    {
                        _buffer.Append(EscapeSpecialChars(enumerable[index]));
                    }
                }
            }

            if (_argumentValidationLogic != null)
            {
                return new ArgumentsBuilder(_buffer, _argumentValidationLogic);
            }
            else
            {
                return new ArgumentsBuilder(_buffer);   
            }
    }

    /// <summary>
    /// Appends a collection of string values to the arguments builder without escaping special characters.
    /// </summary>
    /// <param name="values">The collection of string values to append.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    [Pure]
    public IArgumentsBuilder Add(IEnumerable<string> values)
    {
        return Add(values, false);
    }

    /// <summary>
    /// Appends a formattable value to the arguments builder.
    /// </summary>
    /// <param name="value">The formattable value to append.</param>
    /// <param name="formatProvider">The format provider to use for formatting the value.</param>
    /// <param name="escapeSpecialChars">Whether to escape special characters in the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    [Pure]
    public IArgumentsBuilder Add(IFormattable value, IFormatProvider formatProvider, bool escapeSpecialChars = true)
    {
        if (IsValidArgument(value) == true)
        {
            string val = (string)formatProvider.GetFormat(value.GetType())!;
           
            return Add(value.ToString(val, formatProvider), escapeSpecialChars);
        }
        else
        {
            return this;
        }
    }

    /// <summary>
    /// Appends a formattable value to the arguments builder using the specified culture.
    /// </summary>
    /// <param name="value">The formattable value to append.</param>
    /// <param name="cultureInfo">The culture to use for formatting the value.</param>
    /// <param name="escapeSpecialChars">Whether to escape special characters in the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    [Pure]
    public IArgumentsBuilder Add(IFormattable value, CultureInfo cultureInfo, bool escapeSpecialChars)
    {
        if (IsValidArgument(value) == true)
        {
            return Add(value.ToString((string)cultureInfo.GetFormat(value.GetType())!,
                    DefaultFormatProvider),
                escapeSpecialChars);
        }
        else
        {
            return this;
        }
    }
        
    /// <summary>
    /// Appends a formattable value to the arguments builder using the specified culture without escaping special characters.
    /// </summary>
    /// <param name="value">The formattable value to append.</param>
    /// <param name="cultureInfo">The culture to use for formatting the value.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    [Pure]
    public IArgumentsBuilder Add(IFormattable value, CultureInfo cultureInfo)
    {
        return Add(value, cultureInfo, false);
    }

    /// <summary>
    /// Appends a formattable value to the arguments builder.
    /// </summary>
    /// <param name="value">The formattable value to append.</param>
    /// <param name="escapeSpecialChars">Whether to escape special characters in the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    [Pure]
    public IArgumentsBuilder Add(IFormattable value, bool escapeSpecialChars)
    {
        return Add(value, CultureInfo.CurrentCulture, escapeSpecialChars);
    }

    /// <summary>
    /// Appends a formattable value to the arguments builder without specifying a culture and without escaping special characters.
    /// </summary>
    /// <param name="value">The formattable value to append.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    [Pure]
    public IArgumentsBuilder Add(IFormattable value)
    {
        return Add(value, false);
    }
        
    /// <summary>
    /// Appends a collection of formattable values to the arguments builder.
    /// </summary>
    /// <param name="values">The collection of formattable values to append.</param>
    /// <param name="formatProvider">The format provider to use for formatting the values.</param>
    /// <param name="escapeSpecialChars">Whether to escape special characters in the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    [Pure]
    public IArgumentsBuilder Add(IEnumerable<IFormattable> values, IFormatProvider formatProvider, bool escapeSpecialChars = true)
    {
        IFormattable[] formattable = values as IFormattable[] ?? values.ToArray();
        
            foreach (IFormattable val in formattable)
            {
                string newVal = (string)formatProvider.GetFormat(val.GetType())!;
           
                newVal = val.ToString(newVal, formatProvider);

                if (IsValidArgument(newVal) == true)
                {
                    _buffer.Append(newVal);

                    if (escapeSpecialChars)
                    {
                        _buffer.Append(EscapeSpecialChars(newVal));
                    }
                }
            }

            if (_argumentValidationLogic != null)
            {
                return new ArgumentsBuilder(_buffer, _argumentValidationLogic); 
            }
            else
            {
                return new ArgumentsBuilder(_buffer);
            }
               
    }

    /// <summary>
    /// Appends a collection of formattable values to the arguments builder using the specified culture.
    /// </summary>
    /// <param name="values">The collection of formattable values to append.</param>
    /// <param name="cultureInfo">The culture to use for formatting the values.</param>
    /// <param name="escapeSpecialChars">Whether to escape special characters in the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    [Pure]
    public IArgumentsBuilder Add(IEnumerable<IFormattable> values, CultureInfo cultureInfo, bool escapeSpecialChars)
    {
        foreach (IFormattable val in values)
        {
            string newVal = val.ToString((string)cultureInfo.GetFormat(val.GetType())!, DefaultFormatProvider);

            if (IsValidArgument(newVal) == true)
            {
                _buffer.Append(newVal);

                if (escapeSpecialChars)
                {
                    _buffer.Append(EscapeSpecialChars(newVal));
                }
            }
        }

        if (_argumentValidationLogic != null)
        {
            return new ArgumentsBuilder(_buffer, _argumentValidationLogic);
        }
        else
        {
            return new ArgumentsBuilder(_buffer);   
        }
    }

    /// <summary>
    /// Appends a collection of formattable values to the arguments builder using the specified culture without escaping special characters.
    /// </summary>
    /// <param name="values">The collection of formattable values to append.</param>
    /// <param name="cultureInfo">The culture to use for formatting the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    [Pure]
    public IArgumentsBuilder Add(IEnumerable<IFormattable> values, CultureInfo cultureInfo)
    {
        return Add(values, cultureInfo, false);
    }
        
    /// <summary>
    /// Appends a collection of formattable values to the arguments builder without specifying a culture.
    /// </summary>
    /// <param name="values">The collection of formattable values to append.</param>
    /// <param name="escapeSpecialChars">Whether to escape special characters in the values.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    [Pure]
    public IArgumentsBuilder Add(IEnumerable<IFormattable> values, bool escapeSpecialChars)
    {
        return Add(values, CultureInfo.CurrentCulture, escapeSpecialChars);
    }

    /// <summary>
    /// Appends a collection of formattable values to the arguments builder without specifying a culture and without escaping special characters.
    /// </summary>
    /// <param name="values">The collection of formattable values to append.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    [Pure]
    public IArgumentsBuilder Add(IEnumerable<IFormattable> values)
    {
        return Add(values, false);
    }

    /// <summary>
    /// Builds the arguments into a string.
    /// </summary>
    /// <returns>The arguments as a string.</returns>
    public string Build()
    {
        return _buffer.ToString();
    }

    /// <summary>
    /// Escapes special characters in a string.
    /// </summary>
    /// <param name="argument">The string to escape.</param>
    /// <returns>The escaped string.</returns>
    [Pure]
    public static string EscapeSpecialChars(string argument)
    {
        return argument.Replace("\\", "\\\\")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t")
            .Replace("\r", "\\r")
            .Replace("\"", "\\\"")
            .Replace("'", "\\'");
    }

    /// <summary>
    /// Clears the provided argument strings.
    /// </summary>
    public void Clear()
    {
        _buffer.Clear();
    }

    private bool IsValidArgument(string value)
    {
        bool output;
        
        if (_argumentValidationLogic != null)
        {
            output = _argumentValidationLogic.Invoke(value);
        }
        else
        {
            output = (string.IsNullOrEmpty(value) == true) == false;
        }
        
        return output;
    }

    private bool IsValidArgument(IFormattable value)
    {
        string s = FormattableToStringExtensions.ToString(value);
        
        return IsValidArgument(s);
    }
}