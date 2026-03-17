/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Core.Factories;
using CliInvoke.Core.Processes;
using CliInvoke.Processes;

namespace CliInvoke.Factories;

/// <summary>
/// Represents a factory for creating instances of the <see cref="ExternalProcess"/> class.
/// </summary>
public class ExternalProcessFactory : IExternalProcessFactory
{
    private readonly IFilePathResolver _filePathResolver;
    private readonly IProcessPipeHandler _processPipeHandler;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePathResolver"></param>
    /// <param name="processPipeHandler"></param>
    public ExternalProcessFactory(IFilePathResolver filePathResolver, IProcessPipeHandler processPipeHandler)
    {
        _filePathResolver = filePathResolver;
        _processPipeHandler = processPipeHandler;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="ExternalProcess"/> class.
    /// </summary>
    /// <param name="configuration">The configuration for the external process.</param>
    /// <returns>An <see cref="IExternalProcess"/> instance representing the created external process.</returns>
    [Pure]
    public IExternalProcess CreateExternalProcess(ProcessConfiguration configuration) =>
        new ExternalProcess(_filePathResolver, _processPipeHandler, configuration);

    /// <summary>
    /// Creates a new instance of the <see cref="ExternalProcess"/> class.
    /// </summary>
    /// <param name="configuration">The configuration for the external process.</param>
    /// <param name="exitConfiguration">The process exit configuration details for configuring process exit behaviour.</param>
    /// <returns>An <see cref="IExternalProcess"/> instance representing the created external process.</returns>
    [Pure]
    public IExternalProcess CreateExternalProcess(ProcessConfiguration configuration,
        ProcessExitConfiguration exitConfiguration)
        => new ExternalProcess(_filePathResolver, _processPipeHandler, configuration,
            exitConfiguration);
}