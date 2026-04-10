/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Core.Validation;
using CliInvoke.Extensibility;
using CliInvoke.Factories;
using CliInvoke.Validation;

// ReSharper disable RedundantAssignment

namespace CliInvoke.Extensions;

/// <summary>
///     Provides extension methods for configuring dependency injection for CliInvoke components.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    ///     Sets up Dependency Injection for CliInvoke's main interface-able types.
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
                services.TryAddSingleton<IProcessResultValidator<ProcessResult>>(_ =>
                    new ProcessResultValidator<ProcessResult>(
                        [CommonValidationRules<ProcessResult>.RequiresExitCodeZero]));
                services.TryAddSingleton<IProcessResultValidator<BufferedProcessResult>>(_ =>
                    new ProcessResultValidator<BufferedProcessResult>(
                        [CommonValidationRules<BufferedProcessResult>.RequiresExitCodeZero]));
                services.TryAddSingleton<IProcessResultValidator<PipedProcessResult>>(_ =>
                    new ProcessResultValidator<PipedProcessResult>(
                        [CommonValidationRules<PipedProcessResult>.RequiresExitCodeZero]));

                services.AddSingleton<IExternalProcessFactory, ExternalProcessFactory>();
                services.AddSingleton<IProcessInvoker, ProcessInvoker>();

                services.AddSingleton<IRunnerConfigurationFactory, RunnerConfigurationFactory>();
                services.AddSingleton<IShellDetector, ShellDetector>();
                break;
            case ServiceLifetime.Scoped:
                services.TryAddScoped<IProcessResultValidator<ProcessResult>>(_ =>
                    new ProcessResultValidator<ProcessResult>(
                        [CommonValidationRules<ProcessResult>.RequiresExitCodeZero]));
                services.TryAddScoped<IProcessResultValidator<BufferedProcessResult>>(_ =>
                    new ProcessResultValidator<BufferedProcessResult>(
                        [CommonValidationRules<BufferedProcessResult>.RequiresExitCodeZero]));
                services.TryAddScoped<IProcessResultValidator<PipedProcessResult>>(_ =>
                    new ProcessResultValidator<PipedProcessResult>(
                        [CommonValidationRules<PipedProcessResult>.RequiresExitCodeZero]));

                services.AddScoped<IExternalProcessFactory, ExternalProcessFactory>();
                services.AddScoped<IProcessInvoker, ProcessInvoker>();

                services.AddScoped<IRunnerConfigurationFactory, RunnerConfigurationFactory>();
                services.AddScoped<IShellDetector, ShellDetector>();
                break;
            case ServiceLifetime.Transient:
                services.TryAddTransient<IProcessResultValidator<ProcessResult>>(_ =>
                    new ProcessResultValidator<ProcessResult>(
                        [CommonValidationRules<ProcessResult>.RequiresExitCodeZero]));
                services.TryAddTransient<IProcessResultValidator<BufferedProcessResult>>(_ =>
                    new ProcessResultValidator<BufferedProcessResult>(
                        [CommonValidationRules<BufferedProcessResult>.RequiresExitCodeZero]));
                services.TryAddTransient<IProcessResultValidator<PipedProcessResult>>(_ =>
                    new ProcessResultValidator<PipedProcessResult>(
                        [CommonValidationRules<PipedProcessResult>.RequiresExitCodeZero]));

                services.AddTransient<IExternalProcessFactory, ExternalProcessFactory>();
                services.AddTransient<IProcessInvoker, ProcessInvoker>();

                services.AddTransient<IRunnerConfigurationFactory, RunnerConfigurationFactory>();
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