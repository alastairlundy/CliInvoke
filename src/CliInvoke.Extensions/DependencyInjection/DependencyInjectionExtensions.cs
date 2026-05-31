/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Core.Validation;
using CliInvoke.Extensibility.Factories;
using CliInvoke.Factories;
using CliInvoke.Piping;
using CliInvoke.Validation;
#pragma warning disable CS0618 // Type or member is obsolete

// ReSharper disable RedundantAssignment

namespace CliInvoke.Extensions;

/// <summary>
/// Provides extension methods for configuring dependency injection for CliInvoke components.
/// </summary>
public static partial class DependencyInjectionExtensions
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
                services.AddSingleton<IFilePathResolver, FilePathResolver>();
                services.AddSingleton<IProcessPipeHandler, ProcessPipeHandler>();
                
                services.AddSingleton<IProcessResultValidator<ProcessResult>>(_ => new ProcessResultValidator<ProcessResult>(
                    [CommonValidationRules<ProcessResult>.RequiresExitCodeZero]));
                services.AddSingleton<IProcessResultValidator<BufferedProcessResult>>(_ => new  ProcessResultValidator<BufferedProcessResult>(
                    [CommonValidationRules<BufferedProcessResult>.RequiresExitCodeZero]));
                services.AddSingleton<IProcessResultValidator<PipedProcessResult>>(_ => new ProcessResultValidator<PipedProcessResult>(
                    [CommonValidationRules<PipedProcessResult>.RequiresExitCodeZero]));
                
                services.AddSingleton<IProcessConfigurationFactory, ProcessConfigurationFactory>();
                services.AddSingleton<IExternalProcessFactory, ExternalProcessFactory>();
                services.AddSingleton<IProcessInvoker>(sp => 
                    new ProcessInvoker(sp.GetRequiredService<IExternalProcessFactory>()));
                
                services.AddSingleton<IRunnerProcessFactory, RunnerProcessFactory>();
                services.AddSingleton<IShellDetector, ShellDetector>();
                break;
            case ServiceLifetime.Scoped:
                services.AddScoped<IFilePathResolver, FilePathResolver>();
                services.AddScoped<IProcessPipeHandler, ProcessPipeHandler>();
                
                services.AddScoped<IProcessResultValidator<ProcessResult>>(_ => new ProcessResultValidator<ProcessResult>(
                    [CommonValidationRules<ProcessResult>.RequiresExitCodeZero]));
                services.AddScoped<IProcessResultValidator<BufferedProcessResult>>(_ => new  ProcessResultValidator<BufferedProcessResult>(
                    [CommonValidationRules<BufferedProcessResult>.RequiresExitCodeZero]));
                services.AddScoped<IProcessResultValidator<PipedProcessResult>>(_ => new ProcessResultValidator<PipedProcessResult>(
                    [CommonValidationRules<PipedProcessResult>.RequiresExitCodeZero]));
                
                services.AddScoped<IProcessConfigurationFactory, ProcessConfigurationFactory>();
                services.AddScoped<IExternalProcessFactory, ExternalProcessFactory>();
                services.AddScoped<IProcessInvoker>(sp => 
                    new ProcessInvoker(sp.GetRequiredService<IExternalProcessFactory>()));

                services.AddScoped<IRunnerProcessFactory, RunnerProcessFactory>();
                services.AddScoped<IShellDetector, ShellDetector>();
                break;
            case ServiceLifetime.Transient:
                services.AddTransient<IFilePathResolver, FilePathResolver>();
                services.AddTransient<IProcessPipeHandler, ProcessPipeHandler>();

                services.AddTransient<IProcessResultValidator<ProcessResult>>(_ => new ProcessResultValidator<ProcessResult>(
                    [CommonValidationRules<ProcessResult>.RequiresExitCodeZero]));
                services.AddTransient<IProcessResultValidator<BufferedProcessResult>>(_ => new  ProcessResultValidator<BufferedProcessResult>(
                    [CommonValidationRules<BufferedProcessResult>.RequiresExitCodeZero]));
                services.AddTransient<IProcessResultValidator<PipedProcessResult>>(_ => new ProcessResultValidator<PipedProcessResult>(
                    [CommonValidationRules<PipedProcessResult>.RequiresExitCodeZero]));
                
                services.AddTransient<IProcessConfigurationFactory, ProcessConfigurationFactory>();
                services.AddTransient<IExternalProcessFactory, ExternalProcessFactory>();
                services.AddTransient<IProcessInvoker>(sp => 
                    new ProcessInvoker(sp.GetRequiredService<IExternalProcessFactory>()));

                services.AddTransient<IRunnerProcessFactory, RunnerProcessFactory>();
                services.AddTransient<IShellDetector, ShellDetector>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime),
                    lifetime,
                    null);
        }

        return services;
    }
}