/*
    MIT License

    Copyright (c) 2025-2026 Alastair Lundy

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

// Derived from DotExtensions (MIT License) - See THIRD_PARTY_NOTICES.txt for full details.

using System.Linq;

namespace CliInvoke.Internal.IO;

/// <summary>
/// Provides utility methods for interacting with the system's PATH environment variable
/// and related path operations.
/// </summary>
internal static class PathEnvironmentVariable
{
    /// <summary>
    /// Represents the character used to separate individual entries in the PATH environment variable.
    /// The value is ';' on Windows and ':' on non-Windows operating systems.
    /// </summary>
    public static char PathContentsSeparatorChar
        => OperatingSystem.IsWindows() ? ';' : ':';

    /// <summary>
    /// Enumerates the directories listed in the system's PATH environment variable.
    /// </summary>
    /// <returns>
    /// An enumerable collection of strings representing the individual directories in the PATH environment variable,
    /// or null if the PATH variable has not been set.
    /// </returns>
    public static IEnumerable<string>? EnumerateDirectories()
    {
        return Environment.GetEnvironmentVariable("PATH")
            ?.Split(PathContentsSeparatorChar, StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(x =>
            {
                x = x.Trim();
                x = Environment.ExpandEnvironmentVariables(x);
                x = x.Trim('"');
                const string homeToken = "$HOME";
                string userProfile = Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile);

                int homeTokenIndex = x.IndexOf(
                    homeToken,
                    StringComparison.CurrentCultureIgnoreCase
                );

                if (x.StartsWith('~', StringComparison.OrdinalIgnoreCase))
                {
                    x = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}{x[1..]}";
                }

                if (homeTokenIndex != -1)
                {
                    return
                        $"{x.Substring(0, homeTokenIndex)}{userProfile}{x[(homeTokenIndex + homeToken.Length)..]}";
                }

                return x.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            });
    }

    /// <summary>
    /// Retrieves the directories listed in the system's PATH environment variable.
    /// </summary>
    /// <returns>
    /// An array of strings representing the individual directories in the PATH environment variable,
    /// or null if the PATH variable has not been set.
    /// </returns>
    internal static string[]? GetDirectories() => EnumerateDirectories()?.ToArray();

    /// <summary>
    /// Enumerates the distinct file extensions specified in the system's PATHEXT environment variable
    /// on Windows systems.
    /// </summary>
    /// <returns>
    /// A sequence of strings representing the file extensions from the PATHEXT environment variable,
    /// or a fallback to standard executable extensions if the variable is not defined. On non-Windows systems,
    /// returns a sequence containing a single empty string.
    /// </returns>
    internal static IEnumerable<string> EnumerateFileExtensions()
    {
        if (!OperatingSystem.IsWindows()) return [""];

        return Environment
                   .GetEnvironmentVariable("PATHEXT")
                   ?.Split(PathContentsSeparatorChar, StringSplitOptions.RemoveEmptyEntries)
                   .Where(p => !string.IsNullOrWhiteSpace(p))
                   .Select(x =>
                   {
                       x = x.Trim();
                       x = x.Trim('"');

                       if (!x.StartsWith('.', StringComparison.OrdinalIgnoreCase))
                           x = x.Insert(0, ".");

                       return x;
                   })
                   .Distinct(StringComparer.OrdinalIgnoreCase)
               ?? [".COM", ".EXE", ".BAT", ".CMD"];
    }
}
