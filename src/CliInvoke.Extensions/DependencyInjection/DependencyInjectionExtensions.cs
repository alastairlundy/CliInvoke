/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;

using CliInvoke.Builders;
using CliInvoke.Core.Builders;
using CliInvoke.Core.Middleware;
using CliInvoke.Core.Validation;
using CliInvoke.Extensibility;
using CliInvoke.Extensions.Middleware;
using CliInvoke.Extensions.Middleware.Retry;
using CliInvoke.Factories;
using CliInvoke.Specializations.Middleware;
using CliInvoke.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
        // Null configure means "register default invoker with no custom middleware".
        return ConfigureCliInvokeServices(services, configure: null, lifetime);
    }

    /// <summary>
    ///     Sets up Dependency Injection for CliInvoke's main interface-able types,
    ///     allowing fluent configuration of the middleware pipeline via a callback.
    /// </summary>
    /// <remarks>
    ///     The callback receives an <see cref="IProcessMiddlewareBuilder"/> and can compose
    ///     middleware using <c>UseMiddleware()</c>, <c>UseMiddleware&lt;T&gt;()</c>,
    ///     <c>UseLogging()</c>, <c>UsePostExitValidation()</c>, etc.
    /// </remarks>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configure">
    ///     A delegate that configures the middleware pipeline.
    /// </param>
    /// <param name="lifetime">The service lifetime to use if specified; Scoped otherwise.</param>
    /// <returns>The updated service collection with the added CliInvoke services set up.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="configure"/> is <c>null</c>.
    /// </exception>
    public static IServiceCollection AddCliInvoke(this IServiceCollection services,
        Action<IProcessMiddlewareBuilder> configure,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(configure);

        // configure is validated non-null here; the Core method accepts nullable
        // because the parameterless overload intentionally passes null.
        return ConfigureCliInvokeServices(services, configure, lifetime);
    }

    /// <summary>
    ///     Core implementation shared by both <see cref="AddCliInvoke(IServiceCollection, ServiceLifetime)"/>
    ///     and <see cref="AddCliInvoke(IServiceCollection, Action{IProcessMiddlewareBuilder}, ServiceLifetime)"/>.
    ///     When <paramref name="configure"/> is <c>null</c>, a default <see cref="ProcessInvoker"/>
    ///     with no custom middleware is registered. Null is intentionally allowed here because the
    ///     parameterless public overload delegates to this method without a configure callback.
    /// </summary>
    private static IServiceCollection ConfigureCliInvokeServices(this IServiceCollection services,
        Action<IProcessMiddlewareBuilder>? configure,
        ServiceLifetime lifetime)
    {
        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                services.TryAddSingleton<IFilePathResolver, FilePathResolver>();
                services.RegisterBuiltInMiddleware(lifetime);
                services.TryAddSingleton<IProcessResultValidator<ProcessResult>>(_ =>
                    new ProcessResultValidator<ProcessResult>(
                        [CommonValidationRules<ProcessResult>.RequiresExitCodeZero]));
                services.TryAddSingleton<IProcessResultValidator<BufferedProcessResult>>(_ =>
                    new ProcessResultValidator<BufferedProcessResult>(
                        [CommonValidationRules<BufferedProcessResult>.RequiresExitCodeZero]));

                services.AddSingleton<IProcessConfigurationBuilder, ProcessConfigurationBuilder>();
                
                services.AddSingleton<IExternalProcessFactory, ExternalProcessFactory>();

                if (configure is not null)
                {
                    services.AddSingleton<IProcessInvoker>(sp =>
                    {
                        IExternalProcessFactory factory = sp.GetRequiredService<IExternalProcessFactory>();
                        ProcessMiddlewareBuilder builder = new(sp);
                        configure(builder);
                        IReadOnlyList<IProcessMiddleware> middlewareList = builder.Build();
                        return new ProcessInvoker(factory, middlewareList, null);
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
                services.RegisterBuiltInMiddleware(lifetime);
                services.TryAddScoped<IProcessResultValidator<ProcessResult>>(_ =>
                    new ProcessResultValidator<ProcessResult>(
                        [CommonValidationRules<ProcessResult>.RequiresExitCodeZero]));
                services.TryAddScoped<IProcessResultValidator<BufferedProcessResult>>(_ =>
                    new ProcessResultValidator<BufferedProcessResult>(
                        [CommonValidationRules<BufferedProcessResult>.RequiresExitCodeZero]));

                services.AddScoped<IProcessConfigurationBuilder, ProcessConfigurationBuilder>();

                services.AddScoped<IExternalProcessFactory, ExternalProcessFactory>();

                if (configure is not null)
                {
                    services.AddScoped<IProcessInvoker>(sp =>
                    {
                        IExternalProcessFactory factory = sp.GetRequiredService<IExternalProcessFactory>();
                        ProcessMiddlewareBuilder builder = new(sp);
                        configure(builder);
                        IReadOnlyList<IProcessMiddleware> middlewareList = builder.Build();
                        return new ProcessInvoker(factory, middlewareList, null);
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
                services.RegisterBuiltInMiddleware(lifetime);
                services.TryAddTransient<IProcessResultValidator<ProcessResult>>(_ =>
                    new ProcessResultValidator<ProcessResult>(
                        [CommonValidationRules<ProcessResult>.RequiresExitCodeZero]));
                services.TryAddTransient<IProcessResultValidator<BufferedProcessResult>>(_ =>
                    new ProcessResultValidator<BufferedProcessResult>(
                        [CommonValidationRules<BufferedProcessResult>.RequiresExitCodeZero]));

                services.AddTransient<IProcessConfigurationBuilder, ProcessConfigurationBuilder>();

                services.AddTransient<IExternalProcessFactory, ExternalProcessFactory>();

                if (configure is not null)
                {
                    services.AddTransient<IProcessInvoker>(sp =>
                    {
                        IExternalProcessFactory factory = sp.GetRequiredService<IExternalProcessFactory>();
                        ProcessMiddlewareBuilder builder = new(sp);
                        configure(builder);
                        IReadOnlyList<IProcessMiddleware> middlewareList = builder.Build();
                        return new ProcessInvoker(factory, middlewareList, null);
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

    /// <summary>
    ///     Registers CliInvoke's built-in middleware types and their options POCO in the service
    ///     collection so the type-based <see cref="IProcessMiddlewareBuilder.UseMiddleware{T}"/> overload
    ///     (used by the convenience extensions <c>UseLogging</c>, <c>UsePowerShell</c>, <c>UseCmd</c>)
    ///     can resolve them from the dependency injection container.
    /// </summary>
    /// <remarks>
    ///     All registrations use <see cref="ServiceCollectionDescriptorExtensions.TryAdd(IServiceCollection, ServiceDescriptor)"/>
    ///     so that a consumer-supplied registration for any of these types takes precedence. The
    ///     middleware lifetimes match the invoker lifetime to avoid capturing scoped services into a
    ///     singleton. <see cref="LoggingMiddleware"/> falls back to <see cref="NullLogger{T}.Instance"/>
    ///     when no <see cref="ILogger{T}"/> is registered.
    /// </remarks>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="lifetime">The service lifetime to register the middleware with.</param>
    private static void RegisterBuiltInMiddleware(this IServiceCollection services, ServiceLifetime lifetime)
    {
        services.TryAdd(ServiceDescriptor.Describe(
            typeof(PowerShellMiddlewareOptions),
            _ => PowerShellMiddlewareOptions.Default,
            lifetime));

        // Registration is intentionally platform-agnostic: the middleware themselves throw
        // PlatformNotSupportedException at invocation time on unsupported platforms, which is the
        // documented behaviour. Suppress CA1416 for the construction call sites below.
#pragma warning disable CA1416
        services.TryAdd(ServiceDescriptor.Describe(
            typeof(PowerShellMiddleware),
            sp => new PowerShellMiddleware(
                sp.GetService<PowerShellMiddlewareOptions>()),
            lifetime));

        services.TryAdd(ServiceDescriptor.Describe(
            typeof(CmdMiddleware),
            _ => new CmdMiddleware(),
            lifetime));
#pragma warning restore CA1416

        services.TryAdd(ServiceDescriptor.Describe(
            typeof(LoggingMiddleware),
            sp => new LoggingMiddleware(
                sp.GetService<ILogger<LoggingMiddleware>>() ?? NullLogger<LoggingMiddleware>.Instance),
            lifetime));

        services.TryAdd(ServiceDescriptor.Describe(
            typeof(RetryOptions),
            _ => RetryOptions.Default,
            lifetime));

        services.TryAdd(ServiceDescriptor.Describe(
            typeof(RetryMiddleware),
            sp => new RetryMiddleware(
                sp.GetService<IProcessResultValidator<ProcessResult>>()
                    ?? new ProcessResultValidator<ProcessResult>([CommonValidationRules<ProcessResult>.RequiresExitCodeZero]),
                sp.GetService<RetryOptions>() ?? RetryOptions.Default),
            lifetime));
    }
}