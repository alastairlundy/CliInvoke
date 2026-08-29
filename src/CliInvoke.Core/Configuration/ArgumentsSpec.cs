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
using System.Text;
using CliInvoke.Core.Internal;

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
    ///     Removes all argument values previously appended to the spec.
    /// </summary>
    public void Clear()
    {
        _buffer.Clear();
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

        StringBuilder? joined = null;
        int validCount = 0;

        foreach (var item in values)
        {
            if (item is null || !_argumentValidationLogic.Invoke(item))
                continue;

            if (joined is null)
                joined = new StringBuilder();
            else
                joined.Append(' ');

            joined.Append(escape ? EscapeCharactersWithoutWrapping(item) : item);
            validCount++;
        }

        if (validCount == 0)
            throw new ArgumentException("No valid arguments to add.");

        string wrappedValue = $"\"{joined}\"";

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

        StringBuilder? joined = null;
        int validCount = 0;

        foreach (var item in values)
        {
            string? str = item.ToString(null, _formatProvider);

            if (str is null || !_argumentValidationLogic.Invoke(str))
                continue;

            if (joined is null)
                joined = new StringBuilder();
            else
                joined.Append(' ');

            joined.Append(str);
            validCount++;
        }

        if (validCount == 0)
            throw new ArgumentException("No valid arguments to add.");

        string value = joined!.ToString();
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
    /// <remarks>
    ///     Delegates to <see cref="ArgumentEscaper.EscapeInner"/> so argument escaping
    ///     is platform-aware and consistent with the underlying C-runtime / POSIX shell
    ///     argument parser. The previous JSON-style escaping (<c>\\</c>, <c>\"</c>,
    ///     <c>\n</c>, ...) was unsafe for command lines and is no longer used.
    /// </remarks>
    private string EscapeCharactersWithoutWrapping(string argument)
    {
        ArgumentNullException.ThrowIfNull(argument);

        return ArgumentEscaper.EscapeInner(argument);
    }

    private string EscapeCharacters(string argument)
    {
        ArgumentNullException.ThrowIfNull(argument);

        string escapedContent = ArgumentEscaper.EscapeInner(argument);

        return ArgumentEscaper.NeedsQuoting(argument) ? $"\"{escapedContent}\"" : escapedContent;
    }

    private bool IsValidArgument(IFormattable value, IFormatProvider provider)
        => _argumentValidationLogic.Invoke(value.ToString(null, provider));

    private bool IsValidArgument(string value)
        => _argumentValidationLogic.Invoke(value);
}
