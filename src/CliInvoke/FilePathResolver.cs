/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Linq;
using System.Text;

using DotExtensions.IO.Directories;
using DotExtensions.IO.Permissions;

using DotPrimitives.IO.Paths;

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
        ArgumentException.ThrowIfNullOrWhiteSpace(filePathToResolve);
        
        if (Path.IsPathRooted(filePathToResolve))
        {
            if(ExecutableFileCheck(filePathToResolve))
                return filePathToResolve;
        }

        bool resolveFromPath = ResolveFromPathEnvironmentVariable(filePathToResolve, out FileInfo? filePath);

        if (filePath is not null && resolveFromPath)
        {
            if (ExecutableFileCheck(filePath.FullName))
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
    private bool ExecutableFileCheck(string fileName)
    {
        FileInfo file =  new(fileName);

        return file.HasExecutePermission() ? true :
            throw new ArgumentException(Resources.Exceptions_TargetFile_NotExecutable);
    }

    protected IEnumerable<string>? GetPathInfo()
        => PathEnvironmentVariable.EnumerateDirectories();
    
    protected string[] GetPathExtensionsInfo()
        => PathEnvironmentVariable.GetPathFileExtensions();

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    protected bool ResolveFromPathEnvironmentVariable(string filePathToResolve,
        out FileInfo? resolvedFilePath)
    {
        if (filePathToResolve.Contains(Path.DirectorySeparatorChar)
            || filePathToResolve.Contains(Path.AltDirectorySeparatorChar))
        {
            bool fileExists =  File.Exists(filePathToResolve);

            resolvedFilePath = fileExists ? new FileInfo(filePathToResolve) : null;
            return fileExists;
        }

        string[] pathExtensions = GetPathExtensionsInfo();
        IEnumerable<string>? pathContents = GetPathInfo();
        
        if(pathContents is null)
        {
            resolvedFilePath = null;
            return false;
        }

        string fileName = Path.GetFileNameWithoutExtension(filePathToResolve);
        
        bool fileHasExtension = Path.GetExtension(fileName) != string.Empty;
        
        foreach (string pathEntry in pathContents)
        {
            if (fileHasExtension)
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

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    protected FileInfo LocateFileFromDirectory(string filePathToResolve)
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

        FileInfo? file = directory.SafelyEnumerateFiles("*", SearchOption.AllDirectories)
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
            .FirstOrDefault(f =>
            {
                bool sameName = OperatingSystem.IsWindows() ? f.Name.Equals(fileName, StringComparison.InvariantCultureIgnoreCase) 
                    : f.Name.Equals(filePathToResolve, StringComparison.InvariantCulture);

                return sameName && f.HasExecutePermission();
            });

        return file ?? throw new FileNotFoundException(
            Resources.Exceptions_FileNotFound.Replace(
                "{file}",
                filePathToResolve));
    }
}