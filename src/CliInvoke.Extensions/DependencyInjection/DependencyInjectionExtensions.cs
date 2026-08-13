/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Builders;
using CliInvoke.Core.Builders;
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
        return AddCliInvokeCore(services, configure: null, lifetime);
    }

    /// <summary>
    ///     Sets up Dependency Injection for CliInvoke's main interface-able types,
    ///     allowing fluent configuration of the <see cref="ProcessInvoker"/> via a callback.
    /// </summary>
    /// <remarks>
    ///     The callback receives a bare <see cref="ProcessInvoker"/> and must return a configured instance.
    ///     Typical usage chains middleware extensions such as <c>UseLogging()</c>, <c>UsePostExitValidation()</c>,
    ///     or <c>UsePowerShell()</c>.
    /// </remarks>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configure">
    ///     A delegate that receives a bare <see cref="ProcessInvoker"/> and returns a configured instance.
    /// </param>
    /// <param name="lifetime">The service lifetime to use if specified; Scoped otherwise.</param>
    /// <returns>The updated service collection with the added CliInvoke services set up.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="configure"/> is <c>null</c>.
    /// </exception>
    public static IServiceCollection AddCliInvoke(this IServiceCollection services,
        Func<ProcessInvoker, ProcessInvoker> configure,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(configure);

        return AddCliInvokeCore(services, configure, lifetime);
    }

    /// <summary>
    ///     Core implementation shared by both <see cref="AddCliInvoke(IServiceCollection, ServiceLifetime)"/>
    ///     and <see cref="AddCliInvoke(IServiceCollection, Func{ProcessInvoker, ProcessInvoker}, ServiceLifetime)"/>.
    /// </summary>
    private static IServiceCollection AddCliInvokeCore(this IServiceCollection services,
        Func<ProcessInvoker, ProcessInvoker>? configure,
        ServiceLifetime lifetime)
    {
        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                services.TryAddSingleton<IFilePathResolver, FilePathResolver>();
                services.TryAddSingleton<IProcessResultValidator<ProcessResult>>(_ =>
                    new ProcessResultValidator<ProcessResult>(
                        [CommonValidationRules<ProcessResult>.RequiresExitCodeZero]));
                services.TryAddSingleton<IProcessResultValidator<BufferedProcessResult>>(_ =>
                    new ProcessResultValidator<BufferedProcessResult>(
                        [CommonValidationRules<BufferedProcessResult>.RequiresExitCodeZero]));
                services.TryAddSingleton<IProcessResultValidator<PipedProcessResult>>(_ =>
                    new ProcessResultValidator<PipedProcessResult>(
                        [CommonValidationRules<PipedProcessResult>.RequiresExitCodeZero]));

                services.AddSingleton<IProcessConfigurationBuilder, ProcessConfigurationBuilder>();
                
                services.AddSingleton<IExternalProcessFactory, ExternalProcessFactory>();

                if (configure is not null)
                {
                    services.AddSingleton<IProcessInvoker>(sp =>
                    {
                        IExternalProcessFactory factory = sp.GetRequiredService<IExternalProcessFactory>();
                        ProcessInvoker invoker = new ProcessInvoker(factory);
                        return configure(invoker);
                    });
                }
                else
                {
                    services.AddSingleton<IProcessInvoker, ProcessInvoker>();
                }

                services.AddSingleton<IRunnerConfigurationFactory, RunnerConfigurationFactory>();
                services.AddSingleton<IShellDetector, ShellDetector>();
                break;
            case ServiceLifetime.Scoped:
                services.TryAddScoped<IFilePathResolver, FilePathResolver>();
                services.TryAddScoped<IProcessResultValidator<ProcessResult>>(_ =>
                    new ProcessResultValidator<ProcessResult>(
                        [CommonValidationRules<ProcessResult>.RequiresExitCodeZero]));
                services.TryAddScoped<IProcessResultValidator<BufferedProcessResult>>(_ =>
                    new ProcessResultValidator<BufferedProcessResult>(
                        [CommonValidationRules<BufferedProcessResult>.RequiresExitCodeZero]));
                services.TryAddScoped<IProcessResultValidator<PipedProcessResult>>(_ =>
                    new ProcessResultValidator<PipedProcessResult>(
                        [CommonValidationRules<PipedProcessResult>.RequiresExitCodeZero]));

                services.AddScoped<IProcessConfigurationBuilder, ProcessConfigurationBuilder>();

                services.AddScoped<IExternalProcessFactory, ExternalProcessFactory>();

                if (configure is not null)
                {
                    services.AddScoped<IProcessInvoker>(sp =>
                    {
                        IExternalProcessFactory factory = sp.GetRequiredService<IExternalProcessFactory>();
                        ProcessInvoker invoker = new ProcessInvoker(factory);
                        return configure(invoker);
                    });
                }
                else
                {
                    services.AddScoped<IProcessInvoker, ProcessInvoker>();
                }

                services.AddScoped<IRunnerConfigurationFactory, RunnerConfigurationFactory>();
                services.AddScoped<IShellDetector, ShellDetector>();
                break;
            case ServiceLifetime.Transient:
                services.TryAddTransient<IFilePathResolver, FilePathResolver>();
                services.TryAddTransient<IProcessResultValidator<ProcessResult>>(_ =>
                    new ProcessResultValidator<ProcessResult>(
                        [CommonValidationRules<ProcessResult>.RequiresExitCodeZero]));
                services.TryAddTransient<IProcessResultValidator<BufferedProcessResult>>(_ =>
                    new ProcessResultValidator<BufferedProcessResult>(
                        [CommonValidationRules<BufferedProcessResult>.RequiresExitCodeZero]));
                services.TryAddTransient<IProcessResultValidator<PipedProcessResult>>(_ =>
                    new ProcessResultValidator<PipedProcessResult>(
                        [CommonValidationRules<PipedProcessResult>.RequiresExitCodeZero]));

                services.AddTransient<IProcessConfigurationBuilder, ProcessConfigurationBuilder>();

                services.AddTransient<IExternalProcessFactory, ExternalProcessFactory>();

                if (configure is not null)
                {
                    services.AddTransient<IProcessInvoker>(sp =>
                    {
                        IExternalProcessFactory factory = sp.GetRequiredService<IExternalProcessFactory>();
                        ProcessInvoker invoker = new ProcessInvoker(factory);
                        return configure(invoker);
                    });
                }
                else
                {
                    services.AddTransient<IProcessInvoker, ProcessInvoker>();
                }

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