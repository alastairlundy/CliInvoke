/*
    AlastairLundy.CliInvoke.Core 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace AlastairLundy.CliInvoke.Core.Files;

/// <summary>
/// 
/// </summary>
public interface IFilePathResolver
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePathToResolve"></param>
    /// <returns></returns>
    string ResolveFilePath(string filePathToResolve);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePathToResolve"></param>
    /// <param name="resolvedFilePath"></param>
    /// <returns></returns>
    bool TryResolveFilePath(string filePathToResolve, out string? resolvedFilePath);
}