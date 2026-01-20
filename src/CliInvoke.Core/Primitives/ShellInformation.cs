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
public class ShellInformation
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="targetFilePath"></param>
    /// <param name="version"></param>
    public ShellInformation(string name, FileInfo targetFilePath,
        Version version)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(version);
        
        Name = name;
        TargetFilePath = targetFilePath;
        Version = version;
    }

    public string Name { get; private set; }
    
    public FileInfo TargetFilePath { get; private set; }
    
    public Version Version { get; private set; }
}