﻿/*
    CliInvoke 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System;

using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Core.Piping;

using AlastairLundy.CliInvoke.Piping;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedMember.Global

namespace AlastairLundy.CliInvoke.Extensions;

public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Sets up Dependency Injection for CliInvoke's main interface-able types.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="lifetime">The service lifetime to use if specified; Singleton otherwise.</param>
    /// <returns>The updated service collection with the added CliInvoke services set up.</returns>
    public static IServiceCollection AddCliInvoke(this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                services.TryAddSingleton<IFilePathResolver, FilePathResolver>();
                services.TryAddSingleton<IProcessPipeHandler, ProcessPipeHandler>();

                services.AddSingleton<IProcessFactory, ProcessFactory>();
                services.AddSingleton<IProcessInvoker, ProcessInvoker>();
                break;
            case ServiceLifetime.Scoped:
                services.TryAddScoped<IFilePathResolver, FilePathResolver>();
                services.TryAddScoped<IProcessPipeHandler, ProcessPipeHandler>();
                
                services.AddScoped<IProcessFactory, ProcessFactory>();
                services.AddScoped<IProcessInvoker, ProcessInvoker>();
                
                break;
            case ServiceLifetime.Transient:
                services.TryAddTransient<IFilePathResolver, FilePathResolver>();
                services.TryAddTransient<IProcessPipeHandler, ProcessPipeHandler>();

                services.AddTransient<IProcessFactory, ProcessFactory>();
                services.AddTransient<IProcessInvoker, ProcessInvoker>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime),
                    lifetime,
                    null);
        }
        
        return services;
    }
}