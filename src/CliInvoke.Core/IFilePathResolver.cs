/*
    AlastairLundy.CliInvoke.Core 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace AlastairLundy.CliInvoke.Core;

/// <summary>
/// Defines a contract for a service that resolves file paths.
/// </summary>
public interface IFilePathResolver
{
    /// <summary>
    /// Resolves a file path by checking if the file path exists, or if it's a directory.
    /// </summary>
    /// <param name="filePathToResolve">The file path to resolve.</param>
    /// <returns>The resolved file path if successful, otherwise throws a FileNotFoundException.</returns>
    string ResolveFilePath(string filePathToResolve);
}