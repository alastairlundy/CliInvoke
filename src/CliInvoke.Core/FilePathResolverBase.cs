/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using DotExtensions.IO;

namespace CliInvoke.Core;

/// <summary>
/// An abstract base class for <see cref="IFilePathResolver"/> implementations that captures
/// the shared public resolution algorithm and the <c>Try*</c> wrapper, leaving the
/// PATH-lookup and directory-recursion strategies to subclasses.
/// </summary>
/// <remarks>
/// Subclasses provide the two <see langword="abstract"/> strategies
/// (<see cref="ResolveFromPathEnvironmentVariable"/> and <see cref="LocateFileFromDirectory"/>)
/// and may override the two <see langword="virtual"/> data accessors
/// (<see cref="EnumeratePathDirectories"/> and <see cref="GetPathFileExtensions"/>)
/// to customise how PATH-related data is sourced.
/// </remarks>
public abstract class FilePathResolverBase : IFilePathResolver
{
    /// <summary>
    /// Resolves a file path by short-circuiting rooted paths and otherwise delegating to
    /// <see cref="ResolveFromPathEnvironmentVariable"/> first, then falling back to
    /// <see cref="LocateFileFromDirectory"/>.
    /// </summary>
    /// <param name="filePathToResolve">The file path to resolve.</param>
    /// <returns>The resolved <see cref="FileInfo"/>.</returns>
    public FileInfo ResolveFilePath(string filePathToResolve)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePathToResolve);

        if (Path.IsPathRooted(filePathToResolve))
        {
            return new FileInfo(filePathToResolve);
        }

        // PATH first, then directory recursion — see CONTEXT.md
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
    /// When overridden in a derived class, attempts to resolve <paramref name="filePathToResolve"/>
    /// by looking it up in the directories listed in the PATH environment variable.
    /// </summary>
    /// <param name="filePathToResolve">The file path to resolve.</param>
    /// <param name="resolvedFilePath">When this method returns, contains the resolved <see cref="FileInfo"/> on success; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a match was found in PATH; otherwise, <see langword="false"/>.</returns>
    protected abstract bool ResolveFromPathEnvironmentVariable(string filePathToResolve, out FileInfo? resolvedFilePath);

    /// <summary>
    /// When overridden in a derived class, locates <paramref name="filePathToResolve"/> by
    /// recursing into the directory inferred from the path (or the current working directory
    /// when no directory can be inferred).
    /// </summary>
    /// <param name="filePathToResolve">The file path to resolve.</param>
    /// <returns>The located <see cref="FileInfo"/>.</returns>
    /// <exception cref="FileNotFoundException">Thrown when no matching file is found.</exception>
    protected abstract FileInfo LocateFileFromDirectory(string filePathToResolve);

    /// <summary>
    /// Enumerates the directories listed in the PATH environment variable.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> of directory paths, or <see langword="null"/>
    /// when the PATH variable is not set.
    /// </returns>
    /// <remarks>
    /// The <c>Enumerate*</c> prefix is used to signal an <see cref="IEnumerable{T}"/> return,
    /// following the project's data-accessor naming convention.
    /// </remarks>
    protected virtual IEnumerable<string>? EnumeratePathDirectories()
        => PathEnvironmentVariable.EnumerateDirectories();

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
        string[] extensions = PathEnvironmentVariable.GetPathFileExtensions();

        for (int i = 0; i < extensions.Length; i++)
        {
            extensions[i] = extensions[i].ToLowerInvariant();
        }

        return extensions;
    }
}
