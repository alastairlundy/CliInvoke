/*
        MIT License

       Copyright (c) 2020-2026 Alastair Lundy

       Permission is hereby granted, free of charge, to any person obtaining a copy
       of this software and associated documentation files (the "Software"), to deal
       in the Software without restriction, including without limitation the rights
       to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
       copies of the Software, and to permit persons to whom the Software is
       furnished to do so, subject to the following conditions:

       The above copyright notice and this permission notice shall be included in all
       copies or substantial portions of the Software.

       THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
       IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
       FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
       AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
       LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
       OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
       SOFTWARE.
   */

namespace CliInvoke.Internal.Versions;

/// <summary>
/// Provides extension methods for parsing and manipulating version strings.
/// </summary>
internal static class VersionParseExtensions
{
    #region Version Parsing Helpers

    /// <summary>
    /// Parses a span of characters into the first version component.
    /// </summary>
    /// <param name="chars">The character span to parse.</param>
    /// <returns>
    /// A tuple containing the parsed major version and <c>-1</c> sentinels for
    /// the minor, build, and revision components. If no leading digit is found,
    /// the major is <c>-1</c>.
    /// </returns>
    /// <remarks>
    /// Preprocessing is not required: this function locates the leading digit
    /// run and parses it directly. Any non-digit characters preceding the first
    /// digit are skipped via the iteration, and parsing stops at the first
    /// non-digit.
    /// </remarks>
    private static (int major, int minor, int build, int revision) ParseChars(ReadOnlySpan<char> chars)
    {
        int end = 0;

        while (end < chars.Length && char.IsDigit(chars[end]))
        {
            end++;
        }

        if (end == 0)
        {
            return (-1, -1, -1, -1);
        }

        return (int.Parse(chars[..end], NumberStyles.Integer, CultureInfo.InvariantCulture), -1, -1, -1);
    }

    /// <summary>
    /// Detects the first separator character present in the input.
    /// </summary>
    /// <param name="input">The character span to inspect.</param>
    /// <returns>
    /// <c>'.'</c> if the input contains a dot; otherwise the first separator
    /// or punctuation character encountered; otherwise a single space.
    /// </returns>
    /// <remarks>
    /// Code review concern: the <c>currentChar</c> capture inside the loop
    /// directly assigns the matching character to <c>output</c>. This avoids
    /// the previous re-walk bug where a LINQ <c>First()</c> was used inside
    /// the loop to re-scan the entire input on every match.
    /// </remarks>
    private static char FindSeparator(ReadOnlySpan<char> input)
    {
        char output = ' ';

        if (input.IndexOf('.') != -1)
        {
            return '.';
        }

        foreach (char currentChar in input)
        {
            if (char.IsSeparator(currentChar) || char.IsPunctuation(currentChar))
            {
                output = currentChar;
                break;
            }
        }

        return output;
    }

    /// <summary>
    /// Splits a version string on the supplied separator and parses up to four
    /// numeric components.
    /// </summary>
    /// <param name="input">The character span to parse.</param>
    /// <param name="separator">The component separator character.</param>
    /// <returns>
    /// A tuple containing the parsed major, minor, build, and revision
    /// components. Missing components are <c>-1</c>.
    /// </returns>
    /// <remarks>
    /// Preprocessing is not required: this function handles non-digit
    /// characters within a segment by finding the first digit and reading
    /// digits only. Empty segments (such as those produced by repeated
    /// separators) are filtered out.
    /// <para>
    /// Code review concern: the 4-component cap is enforced by the size of
    /// the stack-allocated <c>Range</c> buffer. If the cap grows above 4, the
    /// stack buffer must be resized accordingly — otherwise the function
    /// silently truncates components beyond the cap.
    /// </para>
    /// </remarks>
    private static (int major, int minor, int build, int revision) ParseComponents(ReadOnlySpan<char> input, char separator)
    {
        Span<Range> components = stackalloc Range[4];
        int count = 0;
        int remainingStart = 0;

        while (remainingStart <= input.Length && count < 4)
        {
            int sepIndex = input[remainingStart..].IndexOf(separator);
            int segmentEnd = sepIndex == -1 ? input.Length : remainingStart + sepIndex;

            if (segmentEnd > remainingStart)
            {
                ReadOnlySpan<char> segment = input[remainingStart..segmentEnd];
                int firstDigit = segment.IndexOfAny(['0', '1', '2', '3', '4', '5', '6', '7', '8', '9']);

                if (firstDigit != -1)
                {
                    int digitEnd = firstDigit;
                    while (digitEnd < segment.Length && char.IsDigit(segment[digitEnd]))
                    {
                        digitEnd++;
                    }

                    if (digitEnd > firstDigit)
                    {
                        int absStart = remainingStart + firstDigit;
                        int absEnd = remainingStart + digitEnd;
                        components[count++] = absStart..absEnd;
                    }
                }
            }

            if (sepIndex == -1)
            {
                break;
            }

            remainingStart = segmentEnd + 1;
        }

        int major = -1, minor = -1, build = -1, revision = -1;

        if (count > 0)
        {
            major = int.Parse(input[components[0]], NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        if (count > 1)
        {
            minor = int.Parse(input[components[1]], NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        if (count > 2)
        {
            build = int.Parse(input[components[2]], NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        if (count > 3)
        {
            revision = int.Parse(input[components[3]], NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        return (major, minor, build, revision);
    }
    #endregion

    extension(Version)
    {
        /// <summary>
        /// Gracefully parses a version string into a <see cref="Version"/> object.
        /// </summary>
        /// <param name="versionString">The version string to parse into a <see cref="Version"/> object.</param>
        /// <returns>Returns a gracefully parsed version.</returns>
        /// <exception cref="ArgumentException">Thrown if the provided <paramref name="versionString"/>
        /// string is null or empty or contains no digits.</exception>
        internal static Version GracefulParse(string versionString)
        {
            ArgumentException.ThrowIfNullOrEmpty(versionString);
            ArgumentException.ThrowIfNullOrWhiteSpace(versionString);

            char separator = FindSeparator(versionString.AsSpan());

            (int major, int minor, int build, int revision) components;

            if (versionString.Contains('.') && separator != ' ')
            {
                components = ParseComponents(versionString.AsSpan(), separator);
            }
            else
            {
                components = ParseChars(versionString.AsSpan());
            }

            if (components is { major: -1, minor: -1, build: -1, revision: -1 })
            {
                char firstDigit = '\0';

                foreach (char currentChar in versionString)
                {
                    if (char.IsDigit(currentChar))
                    {
                        firstDigit = currentChar;
                        break;
                    }
                }

                if (firstDigit == '\0')
                {
                    throw new ArgumentException(string.Format(Resources.Exceptions_VersionParsing_InvalidVersionString, versionString), nameof(versionString));
                }

                components = (firstDigit - '0', -1, -1, -1);
            }

            if (components.build != -1)
            {
                return components.revision != -1
                    ? new Version(components.major, components.minor, components.build, components.revision)
                    : new Version(components.major, components.minor, components.build);
            }

            return components.minor == -1 ? new Version(components.major, 0) :
                new Version(components.major, components.minor);
        }
    }
}
