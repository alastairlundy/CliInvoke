/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.

     Method signatures and field declarations from CliWrap licensed under the MIT License except where considered Copyright Fair Use by law.
     See THIRD_PARTY_NOTICES.txt for a full copy of the MIT LICENSE.
 */

using System.Linq;
using System.Text;

namespace CliInvoke.Builders;

/// <summary>
///     A class that provides a fluent interface style builder for constructing Arguments to provide to
///     a program.
/// </summary>
public class ArgumentsBuilder : IArgumentsBuilder
{
    private readonly IFormatProvider _formatProvider;

    private readonly Func<string, bool> _argumentValidationLogic;

    private readonly StringBuilder _buffer;

    /// <summary>
    ///     Initialises the ArgumentsBuilder.
    /// </summary>
    public ArgumentsBuilder()
    {
        _buffer = new StringBuilder();
        _formatProvider = CultureInfo.InvariantCulture;
        
        _argumentValidationLogic = ArgumentValidationLogic;
        return;

        bool ArgumentValidationLogic(string arg)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(arg);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    ///     Initialises the ArgumentsBuilder with the specified Argument Validation Logic.
    /// </summary>
    /// <param name="argumentValidationLogic">
    ///     The argument validation logic to use to decide whether to
    ///     allow Arguments passed to the builder.
    /// </param>
    public ArgumentsBuilder(Func<string, bool> argumentValidationLogic)
    {
        _buffer = new StringBuilder();
        _argumentValidationLogic = argumentValidationLogic;
        _formatProvider = CultureInfo.InvariantCulture;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="argumentValidationLogic"></param>
    /// <param name="formatProvider"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public ArgumentsBuilder(Func<string, bool> argumentValidationLogic,
        IFormatProvider formatProvider)
    {
        _buffer = new StringBuilder();
        _argumentValidationLogic = argumentValidationLogic;
        ArgumentNullException.ThrowIfNull(formatProvider);
        
        _formatProvider = formatProvider;
    }

    /// <summary>
    ///     Appends a string value to the arguments builder.
    /// </summary>
    /// <param name="value">The string value to append.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    public IArgumentsBuilder Add(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!IsValidArgument(value))
            throw new ArgumentException(
                $"Argument '{value}' not permitted based on validation logic.");

        if (_buffer.Length is > 0 and < int.MaxValue)
            // Add a space if it's missing before adding the new string.
            if (_buffer[^1] != ' ')
                _buffer.Append(' ');

        if (_buffer.Length < _buffer.MaxCapacity && _buffer.Length < int.MaxValue)
            _buffer.Append(value);
        else
            throw new InvalidOperationException(Resources
                .Exceptions_ArgumentBuilder_Buffer_MaximumSize.Replace("{x}",
                    int.MaxValue.ToString()));
        
        return this;
    }

    /// <summary>
    ///     Appends a collection of string values to the arguments builder.
    /// </summary>
    /// <param name="values">The collection of string values to append.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
public IArgumentsBuilder AddRange(IEnumerable<string> values)
{
    ArgumentNullException.ThrowIfNull(values);

    var filteredList = values.Where(x =>  _argumentValidationLogic.Invoke(x)).ToList();
    
    if (filteredList.Count == 0)
        throw new ArgumentException("No valid arguments to add.");

    // Escape each individual argument then join with spaces
    var escapedList = filteredList.Select(EscapeCharacters);
    string joinedEscapedValues = string.Join(" ", escapedList);

    return Add(joinedEscapedValues);
}


    /// <summary>
    ///     Appends a formattable value to the arguments builder.
    /// </summary>
    /// <param name="value">The formattable value to append.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    public IArgumentsBuilder Add(IFormattable value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!IsValidArgument(value, _formatProvider))
            throw new ArgumentException(
                $"Argument '{value.ToString()}' not permitted based on validation logic.");


        string valueActual = value.ToString(null, _formatProvider);

        if (valueActual is null || string.IsNullOrWhiteSpace(valueActual))
            throw new ArgumentNullException(nameof(value));

        return Add(valueActual);    
    }

    /// <summary>
    ///     Appends a collection of formattable values to the argument builder without specifying a
    ///     culture.
    /// </summary>
    /// <param name="values">The collection of formattable values to append.</param>
    /// <returns>A new instance of the IArgumentsBuilder with the updated arguments.</returns>
    public IArgumentsBuilder AddRange(IEnumerable<IFormattable> values)
    { 
        ArgumentNullException.ThrowIfNull(values);

        var valuesList = values.ToList();
        
        if (valuesList.Count == 0)
            throw new ArgumentException("No valid arguments to add.");

        IEnumerable<string> valuesStrings = valuesList.Select(x => x.ToString(null, 
            _formatProvider));

        string value = string.Join(' ', valuesStrings);
        string escapedValue = EscapeCharacters(value);

        return Add(escapedValue);
    }

    /// <summary>
    ///     Escapes characters in a string.
    /// </summary>
    /// <param name="argument">The string to escape.</param>
    /// <returns>The escaped string.</returns>
    [Pure]
    public string EscapeCharacters(string argument)
    {
        ArgumentNullException.ThrowIfNull(argument);

        StringBuilder contentBuilder = new();

        foreach (char c in argument)
            switch (c)
            {
                case '\\': contentBuilder.Append("\\\\"); break;
                case '\"': contentBuilder.Append("\\\""); break;
                case '\n': contentBuilder.Append(@"\n"); break;
                case '\r': contentBuilder.Append(@"\r"); break;
                case '\t': contentBuilder.Append(@"\t"); break;
                case '\b': contentBuilder.Append(@"\b"); break;
                case '\f': contentBuilder.Append(@"\f"); break;
                case '\v': contentBuilder.Append(@"\v"); break;
                case '\a': contentBuilder.Append(@"\a"); break;
                case '\e': contentBuilder.Append(@"\e"); break;
                case '\0': contentBuilder.Append(@"\0"); break;
                default:
                    if (char.IsControl(c))
                    {
                        contentBuilder.AppendFormat(@"\u{0:X4}", (int)c);
                    }
                    else
                    {
                        contentBuilder.Append(c);
                    }
                    break;
            }

        string escapedContent = contentBuilder.ToString();
        
            // Escape and always wrap the result in double quotes
        if (!argument.StartsWith('"') || !argument.EndsWith('"'))
        {
            return $"\"{escapedContent}\"";
        }

        return escapedContent;
    }

    /// <summary>
    ///     Builds the arguments into a string.
    /// </summary>
    /// <returns>The arguments as a string.</returns>
    public new string ToString() => _buffer.ToString();

    /// <summary>
    ///     Clears the provided argument strings.
    /// </summary>
    public void Clear() => _buffer.Clear();
    
    private bool IsValidArgument(IFormattable value, IFormatProvider provider) 
        =>  _argumentValidationLogic.Invoke(value.ToString(null, provider));

    private bool IsValidArgument(string value)
        => _argumentValidationLogic.Invoke(value);
}