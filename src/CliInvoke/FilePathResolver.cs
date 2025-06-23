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
/// /
/// </summary>
public class FilePathResolver : IFilePathResolver
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePathToResolve"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
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
    /// 
    /// </summary>
    /// <param name="filePathToResolve"></param>
    /// <param name="resolvedFilePath"></param>
    /// <returns></returns>
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