/*
    AlastairLundy.CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

#nullable enable

using System;
using System.IO;
using System.Linq;

using AlastairLundy.Resyslib.IO.Core.Files;

namespace AlastairLundy.CliInvoke;

/// <summary>
/// An implementation of IFilePathResolver, a service that resolves file paths.
/// </summary>
public class FilePathResolver : IFilePathResolver
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="inputFilePath"></param>
    /// <param name="outputFilePath"></param>
    /// <exception cref="FileNotFoundException"></exception>
    public void ResolveFilePath(string inputFilePath, out string outputFilePath)
    {
        if (File.Exists(inputFilePath))
        {
            outputFilePath = inputFilePath;
        }

        //   int directoryStart = inputFilePath.IndexOf(Path.DirectorySeparatorChar);
        int directoryEnd = inputFilePath.LastIndexOf(Path.DirectorySeparatorChar);
        
      //  string fileName = inputFilePath.Substring(inputFilePath.LastIndexOf(Path.DirectorySeparatorChar) + 1);
        
        string[] directories = Directory.GetDirectories(inputFilePath.Remove(directoryEnd + 1),
            "*",
            SearchOption.AllDirectories);

        foreach (string directory in directories)
        {
            string[] files = Directory.GetFiles(directory);

            if (files.Any(x => x == Path.GetFullPath(inputFilePath)))
            {
                outputFilePath = Path.GetFullPath(files.First(x => Path.GetFileName(inputFilePath).Equals(x)));
            }
        }
        
        throw new FileNotFoundException(inputFilePath);
    }
}