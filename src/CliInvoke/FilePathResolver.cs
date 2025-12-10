/*
    AlastairLundy.CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Collections.Generic;
using System.Linq;

using AlastairLundy.DotExtensions.IO.Permissions;
using AlastairLundy.DotPrimitives.IO.Paths;

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
        ArgumentException.ThrowIfNullOrEmpty(filePathToResolve);
        
        if (Path.IsPathRooted(filePathToResolve))
        {
            if(ExecutableFileCheck(filePathToResolve))
                return filePathToResolve;
        }

        bool resolveFromPath = ResolveFromPathEnvironmentVariable(filePathToResolve, out FileInfo? filePath);

        if (filePath is not null && resolveFromPath)
        {
            if (ExecutableFileCheck(filePathToResolve))
                return filePath.FullName;
        }
        
        return LocateFileFromDirectory(filePathToResolve).FullName;
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    private static bool ExecutableFileCheck(string fileName)
    {
        FileInfo file =  new FileInfo(fileName);

        return file.HasExecutePermission() ? true :
            throw new ArgumentException(Resources.Exceptions_TargetFile_NotExecutable);
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
        out FileInfo? resolvedFilePath)
    {
        if (filePathToResolve.Contains(Path.DirectorySeparatorChar)
            || filePathToResolve.Contains(Path.AltDirectorySeparatorChar))
        {
            bool fileExists =  File.Exists(filePathToResolve);

            resolvedFilePath = fileExists ? new FileInfo(filePathToResolve) : null;
            return fileExists;
        }

        string[] pathExtensions = PathEnvironmentVariable.GetPathFileExtensions();
        IEnumerable<string>? pathContents = PathEnvironmentVariable.EnumerateDirectories();
        
        if(pathContents is null)
        {
            resolvedFilePath = null;
            return false;
        }

        bool fileHasExtension = Path.GetExtension(filePathToResolve) != string.Empty;

        foreach (string pathEntry in pathContents)
        {
            if (!fileHasExtension)
            {
                foreach (string pathExtension in pathExtensions)
                {
                    string filePath =
                        Path.Combine(pathEntry, $"{filePathToResolve}{pathExtension}");

                    if (File.Exists(filePath))
                    {
                        resolvedFilePath = new FileInfo(filePath);
                        return true;
                    }
                }
            }
            else
            {
                string filePath = Path.Combine(pathEntry, filePathToResolve);

                if (File.Exists(filePath))
                {
                    resolvedFilePath = new FileInfo(filePath);
                    return true;
                }
            }
        }
        
        resolvedFilePath = null;
        return false;
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    private static FileInfo LocateFileFromDirectory(string filePathToResolve)
    {
        string fileName = Path.GetFileName(filePathToResolve);

        int index = filePathToResolve.IndexOf(
            fileName,
            StringComparison.InvariantCultureIgnoreCase
        );

        string directoryPath = Path.GetDirectoryName(filePathToResolve)
                               ?? filePathToResolve.Remove(index, fileName.Length);

        DirectoryInfo directory = new(directoryPath);

        FileInfo? file = directory.EnumerateFiles("*", SearchOption.AllDirectories)
            .Where(f => f.Exists)
            .Select(f =>
            {
                if (OperatingSystem.IsWindows())
                {
                    string extension = Path.GetExtension(f.FullName);

                    int extensionIndex =
                        f.FullName.LastIndexOf(extension, StringComparison.Ordinal);

                    // ReSharper disable once InvertIf
                    if (extensionIndex != -1)
                    {
                        string tempF = f.FullName;
                        tempF = tempF.Remove(extensionIndex, extension.Length);
                        tempF = tempF.Insert(extensionIndex, extension.ToLower());

                        f = new FileInfo(tempF);
                    }
                }

                return f;
            })
            .Where(f =>
            {
                if (OperatingSystem.IsWindows())
                {
                    return Path.GetFileName(f.FullName).Equals(fileName,
                        StringComparison.InvariantCultureIgnoreCase);
                }

                return Path.GetFileName(f.FullName).Equals(filePathToResolve,
                    StringComparison.InvariantCulture);
            })
            .FirstOrDefault(f => f.HasExecutePermission());

        return file ?? throw new FileNotFoundException(
            Resources.Exceptions_FileNotFound.Replace(
                "{file}",
                filePathToResolve
            )
        );
    }
}