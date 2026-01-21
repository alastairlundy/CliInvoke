/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

namespace CliInvoke.Core;

/// <summary>
/// 
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
    /// 
    /// </summary>
    public Version Version { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(ShellInformation? other)
    {
        if (other is null) return false;
        
        return Name == other.Name && TargetFilePath.Equals(other.TargetFilePath);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj)
    {
        if (obj is null) 
            return false;
        
        if(obj is ShellInformation shellInformation) 
            return Equals(shellInformation);

        return false;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Name, TargetFilePath);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool Equals(ShellInformation? left, ShellInformation? right)
    {
        if (left is null || right is null)
            return false;
        
        return left.Equals(right);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(ShellInformation? left, ShellInformation? right) => Equals(left, right);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(ShellInformation? left, ShellInformation? right) => !Equals(left, right);
}