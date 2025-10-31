/*
    CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Core.Extensibility;
using AlastairLundy.CliInvoke.Core.Extensibility.Factories;
using AlastairLundy.CliInvoke.Core.Factories;
using AlastairLundy.CliInvoke.Core.Piping;

using AlastairLundy.CliInvoke.Extensibility;
using AlastairLundy.CliInvoke.Extensibility.Factories;
using AlastairLundy.CliInvoke.Factories;
using AlastairLundy.CliInvoke.Piping;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedMember.Global

namespace AlastairLundy.CliInvoke.Extensions;

/// <summary>
/// Provides extension methods for configuring dependency injection for CliInvoke components.
/// </summary>
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

                services.AddSingleton<IProcessConfigurationFactory, ProcessConfigurationFactory>();
                services.AddSingleton<IProcessInvoker, ProcessInvoker>();

                services.AddSingleton<IRunnerProcessFactory, RunnerProcessFactory>();
                break;
            case ServiceLifetime.Scoped:
                services.TryAddScoped<IFilePathResolver, FilePathResolver>();
                services.TryAddScoped<IProcessPipeHandler, ProcessPipeHandler>();

                services.AddScoped<IProcessConfigurationFactory, ProcessConfigurationFactory>();
                services.AddScoped<IProcessInvoker, ProcessInvoker>();

                services.AddScoped<IRunnerProcessFactory, RunnerProcessFactory>();
                break;
            case ServiceLifetime.Transient:
                services.TryAddTransient<IFilePathResolver, FilePathResolver>();
                services.TryAddTransient<IProcessPipeHandler, ProcessPipeHandler>();

                services.AddTransient<IProcessConfigurationFactory, ProcessConfigurationFactory>();
                services.AddTransient<IProcessInvoker, ProcessInvoker>();

                services.AddTransient<IRunnerProcessFactory, RunnerProcessFactory>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime),
                    lifetime,
                    null);
        }

        return services;
    }

    /// <summary>
    /// Registers a default implementation of the <see cref="DefaultRunnerProcessInvoker"/>
    /// with the specified process configuration and service lifetime to the service collection.
    /// </summary>
    /// <param name="services">The service collection to which the default runner process invoker is added.</param>
    /// <param name="runnerProcessConfiguration">The configuration for the runner process.</param>
    /// <param name="lifetime">The desired service lifetime for the default runner process invoker (Scoped by default).</param>
    /// <returns>The updated service collection, now including the default runner process invoker registration.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the specified service lifetime is not a valid <see cref="ServiceLifetime"/> value.
    /// </exception>
    public static IServiceCollection AddDefaultRunnerProcessInvoker(this IServiceCollection services,
        ProcessConfiguration runnerProcessConfiguration,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        switch (lifetime)
        {
            case ServiceLifetime.Scoped:
                services.AddScoped<RunnerProcessInvokerBase>(sp => new DefaultRunnerProcessInvoker(
                    sp.GetRequiredService<IProcessInvoker>(),
                    sp.GetRequiredService<IRunnerProcessFactory>(),
                    runnerProcessConfiguration));
                services.AddScoped<DefaultRunnerProcessInvoker>(sp => new DefaultRunnerProcessInvoker(
                    sp.GetRequiredService<IProcessInvoker>(),
                    sp.GetRequiredService<IRunnerProcessFactory>(),
                    runnerProcessConfiguration));
                break;
            case ServiceLifetime.Singleton:
                services.AddSingleton<RunnerProcessInvokerBase>(sp => new DefaultRunnerProcessInvoker(
                    sp.GetRequiredService<IProcessInvoker>(),
                    sp.GetRequiredService<IRunnerProcessFactory>(),
                    runnerProcessConfiguration));
                services.AddSingleton<DefaultRunnerProcessInvoker>(sp => new DefaultRunnerProcessInvoker(
                    sp.GetRequiredService<IProcessInvoker>(),
                    sp.GetRequiredService<IRunnerProcessFactory>(),
                    runnerProcessConfiguration));
                break;
            case ServiceLifetime.Transient:
                services.AddTransient<RunnerProcessInvokerBase>(sp => new DefaultRunnerProcessInvoker(
                    sp.GetRequiredService<IProcessInvoker>(),
                    sp.GetRequiredService<IRunnerProcessFactory>(),
                    runnerProcessConfiguration));
                services.AddTransient<DefaultRunnerProcessInvoker>(sp => new DefaultRunnerProcessInvoker(
                    sp.GetRequiredService<IProcessInvoker>(),
                    sp.GetRequiredService<IRunnerProcessFactory>(),
                    runnerProcessConfiguration));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return services;
    }

    /// <summary>
    /// Configures dependency injection to include a derived runner process invoker type.
    /// </summary>
    /// <param name="services">The service collection to add the derived runner process invoker to.</param>
    /// <param name="lifetime">The service lifetime to use for the derived runner process invoker. The default is Scoped.</param>
    /// <returns>The updated service collection with the derived runner process invoker configured.</returns>
    /// <typeparam name="TRunnerType">The type of the derived runner process invoker, which must inherit from <see cref="RunnerProcessInvokerBase"/>.</typeparam>
    /// <exception cref="ArgumentException">Thrown if the provided type is not a subclass of or assignable from <see cref="RunnerProcessInvokerBase"/>.</exception>
    public static IServiceCollection AddDerivedRunnerProcessInvoker<
#if NET8_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
 #endif
        TRunnerType>(this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TRunnerType : RunnerProcessInvokerBase, new()
    {
        switch (lifetime)
        {
            case ServiceLifetime.Scoped:
                services.AddScoped<RunnerProcessInvokerBase, TRunnerType>();
                break;
            case  ServiceLifetime.Transient:
                services.AddTransient<RunnerProcessInvokerBase, TRunnerType>();
                break;
            case ServiceLifetime.Singleton:
                services.AddSingleton<RunnerProcessInvokerBase, TRunnerType>();
                break;
        }
        return services;
    }
    
    /// <summary>
    /// Configures dependency injection to include a derived runner process invoker type.
    /// </summary>
    /// <param name="services">The service collection to add the derived runner process invoker to.</param>
    /// <param name="runnerProcessInvokerType">The type of the derived runner process invoker, which must inherit from <see cref="RunnerProcessInvokerBase"/>.</param>
    /// <param name="runnerProcessConfiguration">The process configuration instance to use for the derived runner process invoker.</param>
    /// <param name="lifetime">The service lifetime to use for the derived runner process invoker. The default is Scoped.</param>
    /// <returns>The updated service collection with the derived runner process invoker configured.</returns>
    /// <exception cref="ArgumentException">Thrown if the provided type is not a subclass of or assignable from <see cref="RunnerProcessInvokerBase"/>.</exception>
    public static IServiceCollection AddDerivedRunnerProcessInvoker(this IServiceCollection services,
#if NET8_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        Type runnerProcessInvokerType,
        ProcessConfiguration runnerProcessConfiguration,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        bool isSubclass = runnerProcessInvokerType.IsSubclassOf(typeof(RunnerProcessInvokerBase));

        bool isAssignableFrom = typeof(RunnerProcessInvokerBase).IsAssignableFrom(runnerProcessInvokerType);

        if (isSubclass == false && isAssignableFrom == false)
            throw new ArgumentException(
                $"Provided type is not a subclass of or assignable from type {nameof(RunnerProcessInvokerBase)}");

        // Factory resolves constructor parameters from the service provider and uses extraArgs for remaining parameters.
        object Factory(IServiceProvider provider) =>
            ActivatorUtilities.CreateInstance(provider, runnerProcessInvokerType, runnerProcessConfiguration);

        services.Add(new ServiceDescriptor(typeof(RunnerProcessInvokerBase), Factory, lifetime));

        return services;
    }
}