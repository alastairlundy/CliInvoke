/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Core;

/// <summary>
/// Defines a contract for a service that resolves file paths.
/// </summary>
/// <remarks>
///  <para> Consumers should not depend on concrete implementations. </para>
///  <para>
///   <see cref="ResolveFilePath(string)"/> returns a valid <see cref="FileInfo"/> file or throws <see cref="System.IO.FileNotFoundException"/> if resolution fails.
///   <see cref="TryResolveFilePath(string, out FileInfo?)"/> returns <see langword="true"/> and sets the resolved <see cref="FileInfo"/> on success, or returns <see langword="false"/> and sets <see langword="null"/> on failure without throwing.
///  </para>
///  <para><b>Note for Implementers:</b> Implementations of <see cref="ResolveFilePath(string)"/> should throw <see cref="System.IO.FileNotFoundException"/> when resolution fails, while implementations of <see cref="TryResolveFilePath(string, out FileInfo?)"/> must not throw on failure.</para>
/// </remarks>
public interface IFilePathResolver
{
    /// <summary>
    /// Resolves a file path by checking if the file path exists or if it's a directory.
    /// </summary>
    /// <param name="filePathToResolve">The file path to resolve.</param>
    /// <returns>The resolved file path if successful, otherwise throws a FileNotFoundException.</returns>
    FileInfo ResolveFilePath(string filePathToResolve);

    /// <summary>
    /// Attempts to resolve a file path by checking if the file path exists or if it's a directory, without throwing on failure.
    /// </summary>
    /// <param name="filePathToResolve">The file path to resolve.</param>
    /// <param name="resolvedFilePath">When this method returns, contains the resolved <see cref="FileInfo"/> if the file was found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the file path was resolved successfully; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// On success, <paramref name="resolvedFilePath"/> is set to the resolved <see cref="FileInfo"/> and the method returns <see langword="true"/>.
    /// On failure, <paramref name="resolvedFilePath"/> is set to <see langword="null"/> and the method returns <see langword="false"/>.
    /// This method does not throw when the file cannot be resolved.
    /// </remarks>
    bool TryResolveFilePath(string filePathToResolve, out FileInfo? resolvedFilePath);
}