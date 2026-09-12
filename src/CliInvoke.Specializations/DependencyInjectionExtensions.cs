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
        ///     Registers the Specializations middleware types
        ///     (<see cref="PowerShellMiddleware"/>, <see cref="CmdMiddleware"/>,
        ///     <see cref="DefaultShellMiddleware"/>) and their options POCO
        ///     so the type-based <see cref="IProcessMiddlewareBuilder.UseMiddleware{T}"/> overload
        ///     (used by <c>UsePowerShell()</c>, <c>UseCmd()</c>, <c>UseDefaultShell()</c>) can
        ///     resolve them from the dependency injection container.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Call this alongside <c>AddCliInvoke</c>, which registers core services and middleware.
        ///         The two registrations are independent and can be chained in either order.
        ///     </para>
        ///     <para>
        ///         <paramref name="lifetime"/> must match the value passed to <c>AddCliInvoke</c>.
        ///         Mismatched lifetimes risk capturing scoped services into a singleton.
        ///     </para>
        ///     <para>
        ///         Registrations use <see cref="ServiceCollectionDescriptorExtensions.TryAdd(IServiceCollection, ServiceDescriptor)"/>
        ///         so consumer-supplied registrations take precedence.
        ///     </para>
        /// </remarks>
        /// <param name="lifetime">The service lifetime to use; defaults to <see cref="ServiceLifetime.Scoped"/>.</param>
        /// <returns>The service collection with the Specializations middleware registered.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown if <paramref name="lifetime"/> is not <see cref="ServiceLifetime.Scoped"/>,
        ///     <see cref="ServiceLifetime.Singleton"/>, or <see cref="ServiceLifetime.Transient"/>.
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
        ///     Registers the Specializations middleware types and their options POCO
        ///     so the type-based <see cref="IProcessMiddlewareBuilder.UseMiddleware{T}"/> overload
        ///     (used by <c>UsePowerShell</c>, <c>UseCmd</c>) can resolve them from DI.
        /// </summary>
        /// <remarks>
        ///     Registrations use <see cref="ServiceCollectionDescriptorExtensions.TryAdd(IServiceCollection, ServiceDescriptor)"/>
        ///     so consumer-supplied registrations take precedence. Middleware lifetimes match the
        ///     invoker lifetime to avoid captive dependencies. <see cref="PowerShellMiddleware"/> and
        ///     <see cref="DefaultShellMiddleware"/> fall back to <see cref="ShellMiddlewareOptions.Default"/>
        ///     when no <see cref="ShellMiddlewareOptions"/> is registered.
        ///     <see cref="DefaultShellMiddleware"/> also requires <see cref="IShellDetector"/>,
        ///     registered by <c>AddCliInvoke</c>.
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