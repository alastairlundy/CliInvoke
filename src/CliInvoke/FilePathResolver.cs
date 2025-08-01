/*
    AlastairLundy.CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

#nullable enable

using System.IO;
using System.Linq;

using AlastairLundy.CliInvoke.Core;

namespace AlastairLundy.CliInvoke;

/// <summary>
/// An implementation of IFilePathResolver, a service that resolves file paths.
/// </summary>
public class FilePathResolver : IFilePathResolver
{
    /// <summary>
    /// Resolves a file path by checking if the file path exists, or if it's a directory.
    /// </summary>
    /// <param name="filePathToResolve">The file path to resolve.</param>
    /// <returns>The resolved file path if successful, otherwise throws a FileNotFoundException.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file path does not exist or cannot be located.</exception>
    public string ResolveFilePath(string filePathToResolve)
    {
        if (File.Exists(filePathToResolve))
        {
            return filePathToResolve;
        }

        string[] directories = Directory.GetDirectories(Path.GetFullPath(filePathToResolve),
            "*",
            SearchOption.AllDirectories);

        foreach (string directory in directories)
        {
            string[] files = Directory.GetFiles(directory);

            if (files.Any(x => Path.GetFileName(filePathToResolve).Equals(x)))
            {
                return Path.GetFullPath(files.First(x => Path.GetFileName(filePathToResolve).Equals(x)));
            }
        }
        
        throw new FileNotFoundException(filePathToResolve);
    }

    /// <summary>
    /// Tries to resolve a file path and returns true on success, false on failure. The resolved file path is returned through the out parameter.
    /// </summary>
    /// <param name="filePathToResolve">The file path to resolve.</param>
    /// <param name="resolvedFilePath">The resolved file path, or null if the operation failed.</param>
    /// <returns>True if the resolution was successful, false otherwise.</returns>
    public bool TryResolveFilePath(string filePathToResolve, out string? resolvedFilePath)
    {
        try
        {
            resolvedFilePath = ResolveFilePath(filePathToResolve);
            return true;
        }
        catch
        {
            resolvedFilePath = null;
            return false;
        }
    }
}