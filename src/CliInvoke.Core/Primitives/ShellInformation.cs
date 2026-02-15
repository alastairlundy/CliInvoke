/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

namespace CliInvoke.Core;

/// <summary>
/// Provides information about a shell program.
/// </summary>
public class ShellInformation : IEquatable<ShellInformation>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="targetFilePath"></param>
    /// <param name="version"></param>
    public ShellInformation(string name, FileInfo targetFilePath, Version version)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        
        Name = name;
        TargetFilePath = targetFilePath;
        Version = version;
    }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <remarks>
    /// Represents a property that holds the name associated with an instance of the ShellInformation class.
    /// </remarks>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the target file path.
    /// </summary>
    /// <remarks>
    /// Represents the path of a file that is targeted by an instance of the ShellInformation class.
    /// </remarks>
    public FileInfo TargetFilePath { get; }

    /// <summary>
    /// Represents the version of a shell program.
    /// </summary>
    public Version Version { get; }

    /// <summary>
    /// Determines whether this instance and a specified <see cref="ShellInformation"/> have the same value.
    /// </summary>
    /// <param name="other">The <see cref="ShellInformation"/> to compare with the current <see cref="ShellInformation"/>.</param>
    /// <returns>true if the two <see cref="ShellInformation"/> are equal; otherwise, false.</returns>
    public bool Equals(ShellInformation? other)
    {
        if (other is null) return false;

        return Name == other.Name && TargetFilePath.Equals(other.TargetFilePath);
    }
    
    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is null) 
            return false;
        
        if(obj is ShellInformation shellInformation) 
            return Equals(shellInformation);

        return false;
    }
    
    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Version, TargetFilePath);
    }

    /// <summary>
    /// Determines whether two <see cref="ShellInformation"/> objects are equal.
    /// </summary>
    /// <param name="left">The first <see cref="ShellInformation"/> to compare.</param>
    /// <param name="right">The second <see cref="ShellInformation"/> to compare.</param>
    /// <returns>true if the two <see cref="ShellInformation"/> are equal; otherwise, false.</returns>
    public static bool Equals(ShellInformation? left, ShellInformation? right)
    {
        if (left is null || right is null)
            return false;
        
        return left.Equals(right);
    }
    
    /// <summary>
    /// Determines whether two <see cref="ShellInformation"/> objects are equal.
    /// </summary>
    /// <param name="left">The first <see cref="ShellInformation"/> to compare.</param>
    /// <param name="right">The second <see cref="ShellInformation"/> to compare.</param>
    /// <returns>true if the two <see cref="ShellInformation"/> are equal; otherwise, false.</returns>
    public static bool operator ==(ShellInformation? left, ShellInformation? right) => Equals(left, right);

    /// <summary>
    /// Determines whether two <see cref="ShellInformation"/> objects are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="ShellInformation"/> to compare.</param>
    /// <param name="right">The second <see cref="ShellInformation"/> to compare.</param>
    /// <returns>true if the two <see cref="ShellInformation"/> are not equal; otherwise, false.</returns>
    public static bool operator !=(ShellInformation? left, ShellInformation? right) => !Equals(left, right);
}