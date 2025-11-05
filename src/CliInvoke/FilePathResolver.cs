/*
    AlastairLundy.CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

using AlastairLundy.CliInvoke.Core;

namespace AlastairLundy.CliInvoke;

#if NETSTANDARD2_0
using OperatingSystem = OperatingSystemPolyfill;
#endif

/// <summary>
/// An implementation of IFilePathResolver, a service that resolves file paths.
/// </summary>
public class FilePathResolver : IFilePathResolver
{
    /// <summary>
    /// Resolves a file path by checking if the file path exists or if it's a directory.
    /// </summary>
    /// <param name="filePathToResolve">The file path to resolve.</param>
    /// <returns>The resolved file path if successful, otherwise throws a FileNotFoundException.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file path does not exist or cannot be located.</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown if run on an unsupported platform.</exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public string ResolveFilePath(string filePathToResolve)
    {
        #if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrEmpty(filePathToResolve,  nameof(filePathToResolve));
        #endif
        
        if (File.Exists(filePathToResolve))
        {
            return filePathToResolve;
        }
        
        bool resolveFromFilePath = ResolveFromPathEnvironmentVariable(filePathToResolve, out string? filePath);

        if (resolveFromFilePath && filePath is not null)
            return filePath;
        
        return LocateFileFromDirectory(filePathToResolve);
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    private static bool ResolveFromPathEnvironmentVariable(string filePathToResolve,
        out string? resolvedFilePath)
    {
        if (Path.IsPathRooted(filePathToResolve)
            || filePathToResolve.Contains(Path.DirectorySeparatorChar)
            || filePathToResolve.Contains(Path.AltDirectorySeparatorChar))
        {
            
            bool fileExists =  File.Exists(filePathToResolve);

            resolvedFilePath = fileExists ? filePathToResolve : null;
            return fileExists;
        }

        char pathSeparator;
        string? pathContentsStr;
        string[]? pathExtensions;
        
        if (OperatingSystem.IsWindows())
        {
            pathSeparator = ';';
            pathContentsStr = Environment.GetEnvironmentVariable("PATH");
            pathExtensions = Environment.GetEnvironmentVariable("PATHEXT")?
                .Split(pathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(ext =>
                {
                    ext = ext.Trim();
                    ext = ext.Trim('"');

                    if (ext.StartsWith('.') == false)
                        ext = ext.Insert(0, ".");

                    return ext;
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? [".COM", ".EXE", ".BAT", ".CMD"];

        }
        else if(!OperatingSystem.IsIOS() && !OperatingSystem.IsBrowser() && !OperatingSystem.IsTvOS())
        {
            pathSeparator = ':';
            pathContentsStr = Environment.GetEnvironmentVariable("PATH");
            pathExtensions = [".sh", ""];
        }
        else
        {
            throw new PlatformNotSupportedException();
        }

        if (pathContentsStr is null)
        {
            resolvedFilePath = null;
            return false;
        }

        string[] pathContents = pathContentsStr
            .Split(pathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(p =>
            {
                p = p.Trim();
                p = Environment.ExpandEnvironmentVariables(p);
                p = p.Trim('"');

                return p;
            })
            .ToArray();

        bool fileHasExtension = Path.GetExtension(filePathToResolve) != string.Empty;

        foreach (string pathEntry in pathContents)
        {
            if (fileHasExtension == false)
            {
                foreach (string pathExtension in pathExtensions)
                {
                    string filePath =
                        Path.Combine(pathEntry, $"{filePathToResolve}{pathExtension}");

                    if (File.Exists(filePath))
                    {
                        resolvedFilePath = filePath;
                        return true;
                    }
                }
            }
            else
            {
                string filePath = Path.Combine(pathEntry, filePathToResolve);

                if (File.Exists(filePath))
                {
                    resolvedFilePath = filePath;
                    return true;
                }
            }
        }
        
        resolvedFilePath = null;
        return false;
    }

    private static string LocateFileFromDirectory(string filePathToResolve)
    {
        string fileName = Path.GetFileName(filePathToResolve);

        int index = filePathToResolve.IndexOf(
            fileName,
            StringComparison.InvariantCultureIgnoreCase
        );

        filePathToResolve = filePathToResolve.Remove(index, fileName.Length);

        string[] directories = Directory.GetDirectories(
            filePathToResolve,
            "*",
            SearchOption.AllDirectories
        );

        foreach (string directory in directories)
        {
            string[] files = Directory.GetFiles(directory);

            if (files.Any(x => Path.GetFileName(filePathToResolve).Equals(x)))
            {
                return Path.GetFullPath(
                    files.First(x => Path.GetFileName(filePathToResolve).Equals(x))
                );
            }
        }

        throw new FileNotFoundException(filePathToResolve);
    }
}
