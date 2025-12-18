/*
    AlastairLundy.CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

using CliInvoke.Core;

// ReSharper disable ConvertClosureToMethodGroup

namespace CliInvoke;

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
        
        if (Path.IsPathRooted(filePathToResolve))
            return filePathToResolve;
        
        bool resolveFromPath = ResolveFromPathEnvironmentVariable(filePathToResolve, out string? filePath);

        if (resolveFromPath && filePath is not null)
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
    protected static bool ResolveFromPathEnvironmentVariable(string filePathToResolve,
        out string? resolvedFilePath)
    {
        if (filePathToResolve.Contains(Path.DirectorySeparatorChar)
            || filePathToResolve.Contains(Path.AltDirectorySeparatorChar))
        {
            
            bool fileExists =  File.Exists(filePathToResolve);

            resolvedFilePath = fileExists ? filePathToResolve : null;
            return fileExists;
        }

        string[] pathExtensions;
        string[] pathContents;
        
        if (GetPathInfo(out string[]? pathExtensionsInfo,
                out string[]? pathContentsInfo) && pathExtensionsInfo is not null &&
            pathContentsInfo is not null)
        {
            pathContents = pathContentsInfo;
            pathExtensions = pathExtensionsInfo;
        }
        else
        {
            resolvedFilePath = null;
            return false;
        }

        bool fileHasExtension = Path.GetExtension(filePathToResolve) != string.Empty;

        foreach (string pathEntry in pathContents)
        {
            if (fileHasExtension)
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

    protected static bool GetPathInfo(out string[]? pathExtensions, out string[]? pathContents)
    {
        char pathSeparator;
        string? pathContentsStr;
        
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
                .ToArray() ?? [".com", ".exe", ".bat", ".cmd"];

        }
        else if(!OperatingSystem.IsIOS() && !OperatingSystem.IsBrowser() && !OperatingSystem.IsTvOS())
        {
            pathSeparator = ':';
            pathContentsStr = Environment.GetEnvironmentVariable("PATH");
            pathExtensions = ["", ".sh"];
        }
        else
        {
            throw new PlatformNotSupportedException();
        }

        if (pathContentsStr is null)
        {
            pathContents = null;
            return false;
        }

        pathContents = pathContentsStr
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
        return true;
    }

    private static string LocateFileFromDirectory(string filePathToResolve)
    {
        string fileName = Path.GetFileName(filePathToResolve);

        int index = filePathToResolve.IndexOf(
            fileName,
            StringComparison.InvariantCultureIgnoreCase
        );

        string directoryPath = Path.GetDirectoryName(filePathToResolve)
                               ?? filePathToResolve.Remove(index, fileName.Length);

        IEnumerable<string> directories = Directory.EnumerateDirectories(
            directoryPath,
            "*",
            SearchOption.AllDirectories
        );

        IEnumerable<string> files = directories.SelectMany(x => Directory.EnumerateFiles(x))
            .Where(f =>
                Path.GetFileName(f).Equals(fileName, StringComparison.InvariantCultureIgnoreCase))
            .Where(f =>
                (string.IsNullOrEmpty(Path.GetExtension(f)) && OperatingSystem.IsWindows()) ||
                !OperatingSystem.IsWindows())
            .Select(f =>
            {
                string extension = Path.GetExtension(f);

                int extensionIndex = f.LastIndexOf(extension, StringComparison.Ordinal);

                // ReSharper disable once InvertIf
                if (extensionIndex != -1)
                {
                    f = f.Remove(extensionIndex, extension.Length);
                    f = f.Insert(extensionIndex, extension.ToLower());
                }
                
                return f;
            });
       
        foreach (string file in files)
        {
            if (Path.GetFileName(file).Equals(filePathToResolve, StringComparison.InvariantCulture))
                return file;
        }

        throw new FileNotFoundException(filePathToResolve);
    }
}