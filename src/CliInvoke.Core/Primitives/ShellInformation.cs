/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

namespace CliInvoke.Core;

/// <summary>
///     Provides information about a shell program.
/// </summary>
public class ShellInformation : IEquatable<ShellInformation>
{
    /// <summary>
    /// Represents information about a shell, including its name, file path, and version.
    /// </summary>
    /// <param name="name">The name of the shell program.</param>
    /// <param name="targetFilePath">The file path of the shell program executable.</param>
    /// <param name="version">The installed version of the shell program.</param>
    public ShellInformation(string name, FileInfo targetFilePath, Version version)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        Name = name;
        TargetFilePath = targetFilePath;
        Version = version;
    }

    /// <summary>
    ///     Gets or sets the name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets or sets the target file path.
    /// </summary>
    public FileInfo TargetFilePath { get; }

    /// <summary>
    ///     Represents the version of a shell program.
    /// </summary>
    public Version Version { get; }

    /// <summary>
    ///     Determines whether this instance and a specified <see cref="ShellInformation" /> have the same
    ///     value.
    /// </summary>
    /// <param name="other">
    ///     The <see cref="ShellInformation" /> to compare with the current
    ///     <see cref="ShellInformation" />.
    /// </param>
    /// <returns>true if the two <see cref="ShellInformation" /> are equal; otherwise, false.</returns>
    /// <remarks>
    ///     Shell file paths are compared with the platform path comparison:
    ///     <see cref="StringComparison.OrdinalIgnoreCase" /> on Windows and
    ///     <see cref="StringComparison.Ordinal" /> elsewhere, matching
    ///     <see cref="ProcessConfiguration" /> and <see cref="ProcessResult" />.
    /// </remarks>
    public bool Equals(ShellInformation? other)
    {
        if (other is null) return false;

        return string.Equals(Name, other.Name, StringComparison.Ordinal) &&
               Version == other.Version &&
               PathsEqual(TargetFilePath.FullName, other.TargetFilePath.FullName);
    }

    /// <summary>
    ///     Compares two file paths using <see cref="StringComparison.OrdinalIgnoreCase"/> on
    ///     Windows and <see cref="StringComparison.Ordinal"/> elsewhere (case-sensitive
    ///     file systems), matching <see cref="ProcessConfiguration"/>, so shell paths equal
    ///     under the platform path comparison produce equal <see cref="ShellInformation"/>
    ///     values; see <see cref="Internal.TargetFilePathNormalization.PathsEqual"/>.
    /// </summary>
    /// <param name="left">The first file path to compare.</param>
    /// <param name="right">The second file path to compare.</param>
    /// <returns>true if the two paths are equal under the platform path comparison; otherwise, false.</returns>
    private static bool PathsEqual(string left, string right)
        => Internal.TargetFilePathNormalization.PathsEqual(left, right);

    /// <summary>
    ///     Computes a hash code for a file path under the same platform-aware comparison as
    ///     <see cref="PathsEqual"/>, so that equal paths (per platform comparison) produce
    ///     equal hash codes and the <see cref="Equals(ShellInformation?)"/> /
    ///     <see cref="GetHashCode"/> contract is preserved; see
    ///     <see cref="Internal.TargetFilePathNormalization.PathHashCode"/>.
    /// </summary>
    /// <param name="path">The file path to hash.</param>
    /// <returns>A hash code consistent with <see cref="PathsEqual"/>.</returns>
    private static int PathHashCode(string path)
        => Internal.TargetFilePathNormalization.PathHashCode(path);

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (obj is ShellInformation shellInformation)
            return Equals(shellInformation);

        return false;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Version, PathHashCode(TargetFilePath.FullName));
    }

    /// <summary>
    ///     Determines whether two <see cref="ShellInformation" /> objects are equal.
    /// </summary>
    /// <param name="left">The first <see cref="ShellInformation" /> to compare.</param>
    /// <param name="right">The second <see cref="ShellInformation" /> to compare.</param>
    /// <returns>true if the two <see cref="ShellInformation" /> are equal; otherwise, false.</returns>
    /// <remarks>
    ///     Returns <see langword="false" /> when either operand is <see langword="null" />,
    ///     including when both operands are <see langword="null" /> — the <c>==</c> operator
    ///     shares this behavior because it delegates to this method.
    /// </remarks>
    public static bool Equals(ShellInformation? left, ShellInformation? right)
    {
        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    /// <summary>
    ///     Determines whether two <see cref="ShellInformation" /> objects are equal.
    /// </summary>
    /// <param name="left">The first <see cref="ShellInformation" /> to compare.</param>
    /// <param name="right">The second <see cref="ShellInformation" /> to compare.</param>
    /// <returns>true if the two <see cref="ShellInformation" /> are equal; otherwise, false.</returns>
    public static bool operator ==(ShellInformation? left, ShellInformation? right) => Equals(left, right);

    /// <summary>
    ///     Determines whether two <see cref="ShellInformation" /> objects are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="ShellInformation" /> to compare.</param>
    /// <param name="right">The second <see cref="ShellInformation" /> to compare.</param>
    /// <returns>true if the two <see cref="ShellInformation" /> are not equal; otherwise, false.</returns>
    public static bool operator !=(ShellInformation? left, ShellInformation? right) => !Equals(left, right);
}