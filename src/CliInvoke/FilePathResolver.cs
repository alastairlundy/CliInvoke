/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Linq;
using System.Text;

using CliInvoke.Core.Internal.IO;

namespace CliInvoke;

/// <summary>
/// The default implementation of <see cref="IFilePathResolver"/>, providing
/// the standard PATH-lookup and directory-recursion strategies on top of
/// the shared algorithm in <see cref="FilePathResolverBase"/>.
/// </summary>
public class FilePathResolver : FilePathResolverBase
{
    /// <summary>
    /// Resolves a file path by checking if the file path exists or if it's a directory.
    /// </summary>
    /// <param name="filePathToResolve">The file path to resolve.</param>
    /// <returns>The resolved file path if successful, otherwise throws a FileNotFoundException.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file path does not exist or cannot be located.</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown if run on an unsupported platform.</exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public FileInfo ResolveFilePath(string filePathToResolve)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePathToResolve);
        
        if (Path.IsPathRooted(filePathToResolve))
        {
            return new FileInfo(filePathToResolve);
        }

        bool resolveFromPath = ResolveFromPathEnvironmentVariable(filePathToResolve, out FileInfo? filePath);

        if (filePath is not null && resolveFromPath)
        {
            return filePath;
        }
        
        return LocateFileFromDirectory(filePathToResolve);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    protected override IEnumerable<string>? EnumeratePathDirectories()
        => PathEnvironmentVariable.EnumerateDirectories();
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    protected override string[] GetPathFileExtensions()
    {
        string[] pathExtensions = PathEnvironmentVariable.EnumerateFileExtensions().ToArray();

        for (int i = 0; i < pathExtensions.Length; i++)
        {
            pathExtensions[i] = pathExtensions[i].ToLowerInvariant();
        }

        return pathExtensions;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePathToResolve"></param>
    /// <param name="resolvedFilePath"></param>
    /// <returns></returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    protected override bool ResolveFromPathEnvironmentVariable(string filePathToResolve,
        out FileInfo? resolvedFilePath)
    {
        if (filePathToResolve.Contains(Path.DirectorySeparatorChar)
            || filePathToResolve.Contains(Path.AltDirectorySeparatorChar))
        {
            bool fileExists =  File.Exists(filePathToResolve);

            resolvedFilePath = fileExists ? new FileInfo(filePathToResolve) : null;
            return fileExists;
        }

        string[] pathExtensions = GetPathFileExtensions();
        IEnumerable<string>? pathContents = EnumeratePathDirectories();
        
        if(pathContents is null)
        {
            resolvedFilePath = null;
            return false;
        }
        
        bool fileHasExtension = Path.GetExtension(filePathToResolve) != string.Empty;

        string fileName = Path.GetFileName(filePathToResolve);

        bool lookForExtension = !fileHasExtension && (OperatingSystem.IsWindows() ||
                                                      OperatingSystem.IsMacOS() ||
                                                      OperatingSystem.IsMacCatalyst());
        
        foreach (string pathEntry in pathContents)
        {
            if (lookForExtension)
            {
                foreach (string pathExtension in pathExtensions)
                {
                    string filePath =
                        Path.Combine(pathEntry, $"{fileName}{pathExtension}");

                    if (File.Exists(filePath))
                    {
                        resolvedFilePath = new(filePath);
                        return true;
                    }
                }
            }
            else
            {
                string filePath = Path.Combine(pathEntry, fileName);

                if (File.Exists(filePath))
                {
                    resolvedFilePath = new(filePath);
                    return true;
                }
            }
        }
        
        resolvedFilePath = null;
        return false;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePathToResolve"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    /// <exception cref="FileNotFoundException"></exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    protected override FileInfo LocateFileFromDirectory(string filePathToResolve)
    {
        string fileName = Path.GetFileName(filePathToResolve);

        int index = filePathToResolve.LastIndexOf(fileName, StringComparison.InvariantCultureIgnoreCase);

        string directoryPath;
        
        try
        {
            directoryPath = Path.GetDirectoryName(filePathToResolve) ??
                            filePathToResolve.Remove(index, fileName.Length);

            if (directoryPath.Length == 0)
                throw new Exception();
        }
        catch
        {
            directoryPath = Environment.CurrentDirectory;
        }
        
        DirectoryInfo directory = new(directoryPath);

        FileInfo? file = directory.EnumerateFiles("*", new EnumerationOptions
            {
                IgnoreInaccessible = true,
                MatchCasing = OperatingSystem.IsWindows() ? MatchCasing.CaseInsensitive : MatchCasing.CaseSensitive,
                RecurseSubdirectories = true,
            })
            .Where(f => f.Exists)
            .Select(f =>
            {
                if (OperatingSystem.IsWindows())
                {
                    string extension = Path.GetExtension(f.FullName);

                    int extensionIndex = f.FullName.LastIndexOf(extension, StringComparison.Ordinal);

                    // ReSharper disable once InvertIf
                    if (extensionIndex != -1)
                    {
                        StringBuilder sb = new StringBuilder(f.FullName);

                        string lowerCasedExtension = extension.ToLower();

                        for (int i = 0; i < extension.Length; i++)
                        {
                            sb[extensionIndex + i] = lowerCasedExtension[i];
                        }
                        
                        f = new FileInfo(sb.ToString());
                    }
                }

                return f;
            })
            .FirstOrDefault(f => OperatingSystem.IsWindows() ? f.Name.Equals(fileName, StringComparison.InvariantCultureIgnoreCase) 
                : f.Name.Equals(filePathToResolve, StringComparison.InvariantCulture));

        return file ?? throw new FileNotFoundException(
            Resources.Exceptions_FileNotFound.Replace(
                "{file}",
                filePathToResolve));
    }
}
