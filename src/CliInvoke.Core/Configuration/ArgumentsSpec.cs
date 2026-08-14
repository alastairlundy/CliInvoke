/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.

     Method signatures and field declarations from CliWrap licensed under the MIT License except where considered Copyright Fair Use by law.
     See THIRD_PARTY_NOTICES.txt for a full copy of the MIT LICENSE.
 */

using System.Globalization;
using System.Linq;
using System.Text;

namespace CliInvoke.Core.Configuration;

/// <summary>
///     A sealed configuration seam for constructing command-line arguments,
///     replacing the former ArgumentsBuilder / IArgumentsBuilder pair.
/// </summary>
public sealed class ArgumentsSpec
{
    private readonly StringBuilder _buffer;
    private readonly Func<string, bool> _argumentValidationLogic;
    private readonly IFormatProvider _formatProvider;

    /// <summary>
    ///     Initialises the <see cref="ArgumentsSpec" /> with default null-check validation.
    /// </summary>
    public ArgumentsSpec()
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
    ///     Initialises the <see cref="ArgumentsSpec" /> with the specified argument validation logic.
    /// </summary>
    /// <param name="argumentValidationLogic">
    ///     The validation logic to use to decide whether to allow arguments passed to the spec.
    /// </param>
    public ArgumentsSpec(Func<string, bool> argumentValidationLogic)
    {
        _buffer = new StringBuilder();
        _argumentValidationLogic = argumentValidationLogic;
        _formatProvider = CultureInfo.InvariantCulture;
    }

    /// <summary>
    ///     Appends a string value to the arguments spec.
    /// </summary>
    /// <param name="value">The string value to append.</param>
    /// <param name="escape">Whether to escape the value before appending.</param>
    /// <returns>The current <see cref="ArgumentsSpec" /> instance.</returns>
    public ArgumentsSpec Add(string value, bool escape)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!IsValidArgument(value))
            throw new ArgumentException(
                $"Argument '{value}' not permitted based on validation logic.");

        string processedValue = escape ? EscapeCharacters(value) : value;

        if (_buffer.Length is > 0 and < int.MaxValue)
            if (_buffer[^1] != ' ')
                _buffer.Append(' ');

        if (_buffer.Length < _buffer.MaxCapacity && _buffer.Length < int.MaxValue)
            _buffer.Append(processedValue);
        else
            throw new InvalidOperationException(
                $"ArgumentBuilder buffer size cannot be added to as it is the maximum size of {int.MaxValue}");

        return this;
    }

    /// <summary>
    ///     Appends a formattable value to the arguments spec.
    /// </summary>
    /// <param name="value">The formattable value to append.</param>
    /// <param name="escape">Whether to escape the value before appending.</param>
    /// <returns>The current <see cref="ArgumentsSpec" /> instance.</returns>
    public ArgumentsSpec Add(IFormattable value, bool escape)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!IsValidArgument(value, _formatProvider))
            throw new ArgumentException(
                $"Argument '{value.ToString()}' not permitted based on validation logic.");

        string valueActual = value.ToString(null, _formatProvider);

        if (valueActual is null || string.IsNullOrWhiteSpace(valueActual))
            throw new ArgumentNullException(nameof(value));

        return Add(valueActual, escape);
    }

    /// <summary>
    ///     Appends a collection of string values to the arguments spec.
    /// </summary>
    /// <param name="values">The collection of string values to append.</param>
    /// <param name="escape">Whether to escape the values before appending.</param>
    /// <returns>The current <see cref="ArgumentsSpec" /> instance.</returns>
    public ArgumentsSpec AddEnumerable(IEnumerable<string> values, bool escape)
    {
        ArgumentNullException.ThrowIfNull(values);

        List<string> filteredList = values.Where(x => _argumentValidationLogic.Invoke(x)).ToList();

        if (filteredList.Count == 0)
            throw new ArgumentException("No valid arguments to add.");

        List<string> processedList = escape
            ? filteredList.Select(v => EscapeCharactersWithoutWrapping(v)).ToList()
            : filteredList;

        string joinedValues = string.Join(" ", processedList);
        string wrappedValue = $"\"{joinedValues}\"";

        if (_buffer.Length is > 0 and < int.MaxValue)
            if (_buffer[^1] != ' ')
                _buffer.Append(' ');

        if (_buffer.Length < _buffer.MaxCapacity && _buffer.Length < int.MaxValue)
            _buffer.Append(wrappedValue);
        else
            throw new InvalidOperationException(
                $"ArgumentBuilder buffer size cannot be added to as it is the maximum size of {int.MaxValue}");

        return this;
    }

    /// <summary>
    ///     Appends a collection of formattable values to the arguments spec.
    /// </summary>
    /// <param name="values">The collection of formattable values to append.</param>
    /// <param name="escape">Whether to escape the values before appending.</param>
    /// <returns>The current <see cref="ArgumentsSpec" /> instance.</returns>
    public ArgumentsSpec AddEnumerable(IEnumerable<IFormattable> values, bool escape)
    {
        ArgumentNullException.ThrowIfNull(values);

        List<IFormattable> valuesList = values.ToList();

        if (valuesList.Count == 0)
            throw new ArgumentException("No valid arguments to add.");

        IEnumerable<string> valuesStrings = valuesList.Select(x => x.ToString(null,
            _formatProvider));

        string value = string.Join(' ', valuesStrings);
        string processedValue = escape ? EscapeCharactersWithoutWrapping(value) : value;
        string wrappedValue = $"\"{processedValue}\"";

        if (_buffer.Length is > 0 and < int.MaxValue)
            if (_buffer[^1] != ' ')
                _buffer.Append(' ');

        if (_buffer.Length < _buffer.MaxCapacity && _buffer.Length < int.MaxValue)
            _buffer.Append(wrappedValue);
        else
            throw new InvalidOperationException(
                $"ArgumentBuilder buffer size cannot be added to as it is the maximum size of {int.MaxValue}");

        return this;
    }

    /// <summary>
    ///     Builds the arguments into a string.
    /// </summary>
    /// <returns>The accumulated arguments as a string.</returns>
    public string Build() => _buffer.ToString();

    /// <summary>
    ///     Escapes characters in a string without wrapping in quotes.
    /// </summary>
    private string EscapeCharactersWithoutWrapping(string argument)
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

        return contentBuilder.ToString();
    }

    private string EscapeCharacters(string argument)
    {
        ArgumentNullException.ThrowIfNull(argument);

        string escapedContent = EscapeCharactersWithoutWrapping(argument);

        if (argument.StartsWith('"') && argument.EndsWith('"'))
        {
            return escapedContent;
        }

        if (escapedContent.EndsWith("\\\""))
        {
            return $"\"{escapedContent}";
        }

        return $"\"{escapedContent}\"";
    }

    private bool IsValidArgument(IFormattable value, IFormatProvider provider)
        => _argumentValidationLogic.Invoke(value.ToString(null, provider));

    private bool IsValidArgument(string value)
        => _argumentValidationLogic.Invoke(value);
}
