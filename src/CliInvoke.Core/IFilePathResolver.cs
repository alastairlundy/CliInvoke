/*
    AlastairLundy.CliInvoke.Core 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace AlastairLundy.CliInvoke.Core;

/// <summary>
/// 
/// </summary>
public interface IFilePathResolver
{
    /// <summary>
    /// Resolves a file path by checking if the file path exists, or if it's a directory.
    /// </summary>
    /// <param name="filePathToResolve">The file path to resolve.</param>
    /// <returns>The resolved file path if successful, otherwise throws a FileNotFoundException.</returns>
    string ResolveFilePath(string filePathToResolve);
    
    /// <summary>
    /// Tries to resolve a file path and returns true on success, false on failure. The resolved file path is returned through the out parameter.
    /// </summary>
    /// <param name="filePathToResolve">The file path to resolve.</param>
    /// <param name="resolvedFilePath">The resolved file path, or null if the operation failed.</param>
    /// <returns>True if the resolution was successful, false otherwise.</returns>
    bool TryResolveFilePath(string filePathToResolve, out string? resolvedFilePath);
}