/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Linq;
using System.Text;

using CliInvoke.Internal.IO;

namespace CliInvoke;

/// <summary>
/// The default implementation of <see cref="IFilePathResolver"/>, providing
/// the standard PATH-lookup and directory-recursion strategies (PATH first, then
/// directory recursion — see GLOSSARY.md Design Decision 1).
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
    /// Attempts to resolve a file path by delegating to <see cref="ResolveFilePath"/>,
    /// swallowing any exception that escapes and reporting failure via the return value.
    /// </summary>
    /// <param name="filePathToResolve">The file path to resolve.</param>
    /// <param name="resolvedFilePath">When this method returns, contains the resolved <see cref="FileInfo"/> on success; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the file path was resolved successfully; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// This wrapper follows the .NET <c>Try*</c> convention: it catches <see cref="Exception"/>
    /// (not just <see cref="FileNotFoundException"/>) and never propagates an exception upward.
    /// On failure the underlying exception is discarded and <paramref name="resolvedFilePath"/>
    /// is set to <see langword="null"/>; callers that need the failure cause should use
    /// <see cref="ResolveFilePath"/> instead.
    /// </remarks>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public bool TryResolveFilePath(string filePathToResolve, out FileInfo? resolvedFilePath)
    {
        try
        {
            resolvedFilePath = ResolveFilePath(filePathToResolve);
            return true;
        }
        catch (Exception)
        {
            resolvedFilePath = null;
            return false;
        }
    }

    /// <summary>
    /// Enumerates the directories listed in the PATH environment variable.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> of directory paths, or <see langword="null"/>
    /// when the PATH variable is not set.
    /// </returns>
    protected virtual IEnumerable<string>? EnumeratePathDirectories()
        => PathEnvironmentVariable.EnumerateDirectories();

    /// <summary>
    /// Attempts to resolve <paramref name="filePathToResolve"/> by looking it up in the
    /// directories listed in the PATH environment variable.
    /// </summary>
    /// <param name="filePathToResolve">The file path to resolve.</param>
    /// <param name="resolvedFilePath">When this method returns, contains the resolved <see cref="FileInfo"/> on success; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a match was found in PATH; otherwise, <see langword="false"/>.</returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    protected virtual bool ResolveFromPathEnvironmentVariable(string filePathToResolve,
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
                        resolvedFilePath = new FileInfo(filePath);
                        return true;
                    }
                }
            }
            else
            {
                string filePath = Path.Combine(pathEntry, fileName);

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

    /// <summary>
    /// Locates <paramref name="filePathToResolve"/> by recursing into the directory inferred
    /// from the path (or the current working directory when no directory can be inferred).
    /// </summary>
    /// <param name="filePathToResolve">The file path to resolve.</param>
    /// <returns>The located <see cref="FileInfo"/>.</returns>
    /// <exception cref="FileNotFoundException">Thrown when no matching file is found.</exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    protected virtual FileInfo LocateFileFromDirectory(string filePathToResolve)
    {
        string fileName = Path.GetFileName(filePathToResolve);

        int index = filePathToResolve.LastIndexOf(fileName, StringComparison.OrdinalIgnoreCase);

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

        // Limit the enumeration to the immediate directory (no recursion). The previous
        // RecurseSubdirectories:true walked the entire subtree from the inferred base directory,
        // which is a denial-of-service / over-broad-match surface when the base directory is large
        // or attacker-influenced. A single-level lookup is sufficient for the directory-recursion
        // fallback and avoids enumerating unrelated files.
        FileInfo? file = directory.EnumerateFiles("*", new EnumerationOptions
            {
                IgnoreInaccessible = true,
                MatchCasing = OperatingSystem.IsWindows() ? MatchCasing.CaseInsensitive : MatchCasing.CaseSensitive,
                RecurseSubdirectories = false,
            })
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

                        for (int i = 0; i < extension.Length; i++)
                        {
                            sb[extensionIndex + i] = char.ToLowerInvariant(extension[i]);
                        }
                        
                        f = new FileInfo(sb.ToString());
                    }
                }

                return f;
            })
            .FirstOrDefault(f => OperatingSystem.IsWindows() ? f.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase)
                : f.Name.Equals(filePathToResolve, StringComparison.Ordinal));

        if (file is null)
            throw new FileNotFoundException(
                Resources.Exceptions_FileNotFound.Replace(
                    "{file}",
                    filePathToResolve));

        FileInfo refreshed = new FileInfo(file.FullName);

        if (!refreshed.Exists)
            throw new FileNotFoundException(
                Resources.Exceptions_FileNotFound.Replace(
                    "{file}",
                    filePathToResolve));

        return refreshed;
    }

    /// <summary>
    /// Returns the file extensions listed in the PATHEXT environment variable, lowercased
    /// in a single pass before being returned.
    /// </summary>
    /// <returns>A new <see cref="string"/> array containing the lowercased file extensions.</returns>
    /// <remarks>
    /// Subclasses receive a freshly allocated array in which every character has been
    /// converted to its lowercase form. The returned array is safe to mutate without
    /// affecting the underlying PATHEXT environment variable.
    /// </remarks>
    protected virtual string[] GetPathFileExtensions()
    {
        string[] extensions = PathEnvironmentVariable.EnumerateFileExtensions().ToArray();

        for (int i = 0; i < extensions.Length; i++)
        {
            extensions[i] = extensions[i].ToLowerInvariant();
        }

        return extensions;
    }
}
