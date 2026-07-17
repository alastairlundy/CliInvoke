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

using System.Linq;
using System.Text;

namespace CliInvoke.Internal.Versions;

/// <summary>
/// Provides extension methods for parsing and manipulating version strings.
/// </summary>
internal static class VersionParseExtensions
{
    #region Version Parsing Helpers
    private static (int major, int minor, int build, int revision) ParseChars(ReadOnlySpan<char> chars)
    {
        StringBuilder stringBuilder = new();
        
        foreach (char currentChar in chars)
        {
            if (char.IsDigit(currentChar))
            {
                stringBuilder.Append(currentChar);
            }
            else
            {
                break;
            }
        }

        string result = stringBuilder.ToString();

        if (result.Equals(string.Empty))
            result = "-1";
        
        return (int.Parse(result, CultureInfo.InvariantCulture), -1, -1, -1);
    }
    
    private static string SanitizeInput(string versionString, char separator)
    {
        StringBuilder stringBuilder = new(versionString.Length);

        foreach (char currentChar in versionString)
        {
            if(char.IsDigit(currentChar) || currentChar == separator)
                stringBuilder.Append(currentChar);
        }
            
        return stringBuilder.ToString();
    }

    private static char FindSeparator(string versionString)
    {
        char output = ' ';

        if (versionString.Contains('.', StringComparison.OrdinalIgnoreCase))
            return '.';
            
        foreach (char currentChar in versionString)
        {
            if (char.IsSeparator(currentChar) || char.IsPunctuation(currentChar))
            {
                output = versionString.First(c => char.IsSeparator(c) || char.IsPunctuation(c));
                break;
            }
        }
        
        return output;
    }
    
    private static (int major, int minor, int build, int revision) ParseComponents(StringSegment[] versionComponents)
    {
        int major = -1, minor = -1, build = -1, revision = -1;

        int componentsAdded = 0;
        
        versionComponents = versionComponents.Where(v =>
            {
                int firstNumberIndex = v.IndexOfAny(['0', '1', '2',  '3', '4', '5', '6', '7', '8', '9']);

                return firstNumberIndex != -1;
            })
            .Select(v =>
            {
                int index = v.IndexOfAny(['0', '1', '2',  '3', '4', '5', '6', '7', '8', '9']);

                return v.Subsegment(index);
            })
            .ToArray();


        for (int index = 0; index < versionComponents.Length; index++)
        {
            StringSegment component = versionComponents[index];
            
            if (componentsAdded >= 4)
                break;

            StringBuilder stringBuilder = new(component.Length);

            for (int i = 0; i < component.Length; i++)
            {
                char currentChar = component[i];
                
                if (char.IsDigit(currentChar))
                {
                    stringBuilder.Append(currentChar);
                }
            }

            string componentString = stringBuilder.ToString().TrimEnd('.');
            
            switch (index)
            {
                case 0:
                    major = int.Parse(componentString, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    break;
                case 1:
                    minor = int.Parse(componentString, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    break;
                case 2:
                    build = int.Parse(componentString, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    break;
                case 3:
                    revision = int.Parse(componentString, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    break;
            }
            componentsAdded++;
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

            char separator = FindSeparator(versionString);
            
            string sanitizedInput = SanitizeInput(versionString, separator);
            
            (int major, int minor, int build, int revision) components;
            
            if (sanitizedInput.Contains('.', StringComparison.OrdinalIgnoreCase) && separator != ' ')
            {
                IEnumerable<StringSegment> segments = new StringTokenizer(sanitizedInput, [separator]);
                
                StringSegment[] versionComponents = segments.Take(4).ToArray();
                components = ParseComponents(versionComponents);
            }
            else
            {
                components = ParseChars(sanitizedInput.AsSpan());
            }

            if (components is { major: -1, minor: -1, build: -1, revision: -1 })
            {
                try
                {
                    components = (sanitizedInput.First(c => char.IsDigit(c)), 0, 0, 0);
                }
                catch
                {
                    throw new ArgumentException(string.Format(Resources.Exceptions_VersionParsing_InvalidVersionString, versionString), nameof(versionString));
                }
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