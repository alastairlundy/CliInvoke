/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

namespace CliInvoke.Core.Internal;

/// <summary>
///     The single source of truth for TargetFilePath (and other file path string) case
///     normalization: the platform path comparison and the case-normalized key form
///     derived from it.
/// </summary>
/// <remarks>
///     <para>
///         Windows file systems are case-insensitive; Unix-like file systems are not.
///         Every equality, hash, and dictionary-key decision over file path strings must
///         therefore agree on one comparison — <see cref="StringComparison.OrdinalIgnoreCase" />
///         on Windows and <see cref="StringComparison.Ordinal"/> elsewhere — so that equal
///         paths compare equal and hash equal everywhere in the library.
///     </para>
///     <para>
///         <see cref="PathsEqual"/> and <see cref="PathHashCode"/> implement that comparison
///         directly, preserving the <c>Equals</c>/<c>GetHashCode</c> contract for the types that
///         delegate to them (<see cref="ProcessConfiguration"/>,
///         <see cref="ShellInformation"/>, <see cref="ProcessResult"/>,
///         and <see cref="BufferedProcessResult"/>).
///         <see cref="NormalizeCase"/> supplies the case-normalized string form used where a
///         dictionary key (rather than a comparison) is required, e.g. the file-path cache keys
///         of <c>CachingFilePathResolver</c>: equal paths per the platform comparison produce
///         equal normalized keys.
///     </para>
/// </remarks>
internal static class TargetFilePathNormalization
{
    /// <summary>
    ///     The platform path comparison for file paths:
    ///     <see cref="StringComparison.OrdinalIgnoreCase" /> on Windows (where file-system
    ///     paths are case-insensitive) and <see cref="StringComparison.Ordinal" /> elsewhere.
    /// </summary>
    public static StringComparison PlatformPathComparison =>
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    /// <summary>
    ///     Compares two file paths under the platform path comparison
    ///     (<see cref="PlatformPathComparison"/>).
    /// </summary>
    /// <param name="left">The first file path to compare.</param>
    /// <param name="right">The second file path to compare.</param>
    /// <returns>true when both paths are equal under the platform path comparison; otherwise, false.</returns>
    public static bool PathsEqual(string left, string right)
    {
        StringComparison comparison = PlatformPathComparison;

        return string.Equals(left, right, comparison);
    }

    /// <summary>
    ///     Computes a hash code for a file path under the platform path comparison
    ///     (<see cref="PlatformPathComparison"/>), so that paths equal under the platform
    ///     comparison produce equal hash codes.
    /// </summary>
    /// <param name="path">The file path to hash.</param>
    /// <returns>A hash code consistent with <see cref="PathsEqual"/>.</returns>
    public static int PathHashCode(string path)
    {
        StringComparison comparison = PlatformPathComparison;

        return string.GetHashCode(path, comparison);
    }

    /// <summary>
    ///     Returns the case-normalized form of a file path: paths equal under the platform
    ///     path comparison produce identical normalized forms.
    /// </summary>
    /// <param name="path">The file path to normalize.</param>
    /// <returns>
    ///     The uppercased path on Windows and the unchanged path elsewhere, matching the
    ///     case sensitivity of <see cref="PlatformPathComparison"/>.
    /// </returns>
    public static string NormalizeCase(string path)
    {
        return OperatingSystem.IsWindows()
            ? path.ToUpperInvariant()
            : path;
    }
}
