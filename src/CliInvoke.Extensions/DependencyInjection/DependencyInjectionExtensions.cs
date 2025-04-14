/*
    CliInvoke 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System;
using AlastairLundy.CliInvoke;
using AlastairLundy.CliInvoke.Abstractions;
using AlastairLundy.Extensions.IO.Files;
using AlastairLundy.Extensions.IO.Files.Abstractions;
using AlastairLundy.Extensions.Processes;
using AlastairLundy.Extensions.Processes.Abstractions;
using AlastairLundy.Extensions.Processes.Abstractions.Piping;
using AlastairLundy.Extensions.Processes.Abstractions.Utilities;
using AlastairLundy.Extensions.Processes.Piping;
using AlastairLundy.Extensions.Processes.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedMember.Global

namespace CliInvoke.Extensions;

public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Sets up Dependency Injection for CliInvoke's main interface-able types.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="lifetime">The service lifetime to use if specified; Singleton otherwise.</param>
    /// <returns>The updated service collection with the added CliInvoke services set up.</returns>
    public static IServiceCollection AddCliInvoke(this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                services.TryAddSingleton<IFilePathResolver, FilePathResolver>();
                services.TryAddSingleton<IProcessRunnerUtility, ProcessRunnerUtility>();
                services.TryAddSingleton<IPipedProcessRunner, PipedProcessRunner>();
                services.TryAddSingleton<IProcessPipeHandler, ProcessPipeHandler>();
                
                services.AddSingleton<ICommandProcessFactory, CommandProcessFactory>();
                services.AddSingleton<ICliCommandInvoker, CliCommandInvoker>();
                break;
            case ServiceLifetime.Scoped:
                services.TryAddScoped<IFilePathResolver, FilePathResolver>();
                services.TryAddScoped<IProcessRunnerUtility, ProcessRunnerUtility>();
                services.TryAddScoped<IPipedProcessRunner, PipedProcessRunner>();
                services.TryAddScoped<IProcessPipeHandler, ProcessPipeHandler>();
                
                services.AddScoped<ICommandProcessFactory, CommandProcessFactory>();
                services.AddScoped<ICliCommandInvoker, CliCommandInvoker>();
                break;
            case ServiceLifetime.Transient:
                services.TryAddTransient<IFilePathResolver, FilePathResolver>();
                services.TryAddTransient<IProcessRunnerUtility, ProcessRunnerUtility>();
                services.TryAddTransient<IPipedProcessRunner, PipedProcessRunner>();
                services.TryAddTransient<IProcessPipeHandler, ProcessPipeHandler>();
                
                services.AddTransient<ICommandProcessFactory, CommandProcessFactory>();
                services.AddTransient<ICliCommandInvoker, CliCommandInvoker>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
        
        return services;
    }
}