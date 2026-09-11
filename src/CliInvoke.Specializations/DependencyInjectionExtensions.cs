/*
    CliInvoke Specializations
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Specializations.Middleware;

namespace CliInvoke.Specializations;

/// <summary>
///     Provides extension methods for configuring dependency injection for CliInvoke's
///     Specializations middleware.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <param name="services">The service collection to add to.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     Registers the CliInvoke Specializations middleware types
        ///     (<see cref="PowerShellMiddleware"/>, <see cref="CmdMiddleware"/>,
        ///     <see cref="DefaultShellMiddleware"/>) and their options POCO
        ///     in the service collection so the type-based
        ///     <see cref="IProcessMiddlewareBuilder.UseMiddleware{T}"/> overload (used by the convenience
        ///     extensions <c>UsePowerShell()</c>, <c>UseCmd()</c>, and <c>UseDefaultShell()</c>) can
        ///     resolve them from the dependency injection container.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Call this method alongside <c>AddCliInvoke</c> from the main CliInvoke package, which
        ///         registers CliInvoke's core services and core built-in middleware; the two
        ///         registrations are independent and can be chained in either order.
        ///     </para>
        ///     <para>
        ///         <paramref name="lifetime"/> must match the lifetime passed to <c>AddCliInvoke</c>:
        ///         middleware lifetimes are matched to the invoker lifetime to avoid capturing scoped
        ///         services into a singleton (captive dependency). Middleware registered with a lifetime
        ///         that differs from the <c>IProcessInvoker</c> lifetime may be captured by a
        ///         longer-lived service.
        ///     </para>
        ///     <para>
        ///         All registrations use <see cref="ServiceCollectionDescriptorExtensions.TryAdd(IServiceCollection, ServiceDescriptor)"/>
        ///         so that a consumer-supplied registration for any of these types takes precedence.
        ///     </para>
        /// </remarks>
        /// <param name="lifetime">The service lifetime to use if specified; Scoped otherwise.</param>
        /// <returns>The updated service collection with the added CliInvoke Specializations middleware set up.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown if an invalid value is provided for <paramref name="lifetime"/>. Valid values are
        ///     Scoped, Singleton, and Transient.
        /// </exception>
        public IServiceCollection AddCliInvokeSpecializations(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            switch (lifetime)
            {
                case ServiceLifetime.Singleton:
                case ServiceLifetime.Scoped:
                case ServiceLifetime.Transient:
                    services.RegisterPlatformMiddleware(lifetime);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime),
                        lifetime,
                        null);
            }

            return services;
        }

        /// <summary>
        ///     Registers the Specializations middleware types and their options POCO in the service
        ///     collection so the type-based <see cref="IProcessMiddlewareBuilder.UseMiddleware{T}"/> overload
        ///     (used by the convenience extensions <c>UsePowerShell</c>, <c>UseCmd</c>)
        ///     can resolve them from the dependency injection container.
        /// </summary>
        /// <remarks>
        ///     All registrations use <see cref="ServiceCollectionDescriptorExtensions.TryAdd(IServiceCollection, ServiceDescriptor)"/>
        ///     so that a consumer-supplied registration for any of these types takes precedence. The
        ///     middleware lifetimes match the invoker lifetime to avoid capturing scoped services into a
        ///     singleton. <see cref="PowerShellMiddleware"/> and <see cref="DefaultShellMiddleware"/> fall
        ///     back to <see cref="ShellMiddlewareOptions.Default"/> when no
        ///     <see cref="ShellMiddlewareOptions"/> is registered.
        ///     <see cref="DefaultShellMiddleware"/> additionally requires
        ///     <see cref="IShellDetector"/>, registered by <c>AddCliInvoke</c>.
        /// </remarks>
        /// <param name="lifetime">The service lifetime to register the middleware with.</param>
        private void RegisterPlatformMiddleware(ServiceLifetime lifetime)
        {
            services.TryAdd(ServiceDescriptor.Describe(
                typeof(ShellMiddlewareOptions),
                _ => ShellMiddlewareOptions.Default,
                lifetime));

            // Registration is intentionally platform-agnostic: the middleware themselves throw
            // PlatformNotSupportedException at invocation time on unsupported platforms, which is the
            // documented behaviour. Suppress CA1416 for the construction call sites below.
#pragma warning disable CA1416
            services.TryAdd(ServiceDescriptor.Describe(
                typeof(PowerShellMiddleware),
                sp => new PowerShellMiddleware(
                    sp.GetService<ShellMiddlewareOptions>()),
                lifetime));

            services.TryAdd(ServiceDescriptor.Describe(
                typeof(DefaultShellMiddleware),
                sp => new DefaultShellMiddleware(
                    sp.GetRequiredService<IShellDetector>(),
                    sp.GetService<ShellMiddlewareOptions>()),
                lifetime));
            
            services.TryAdd(ServiceDescriptor.Describe(
                typeof(CmdMiddleware),
                _ => new CmdMiddleware(),
                lifetime));
#pragma warning restore CA1416
        }
    }
}