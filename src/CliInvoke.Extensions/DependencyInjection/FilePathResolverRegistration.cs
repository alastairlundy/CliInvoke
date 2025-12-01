/*
    CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Diagnostics.CodeAnalysis;

using CliInvoke.Core;

using Microsoft.Extensions.DependencyInjection;

namespace CliInvoke.Extensions;

public static class FilePathResolverRegistration
{
    /// <param name="services">The <see cref="IServiceCollection"/> to register the implementation with.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers a custom implementation of the <see cref="IFilePathResolver"/> interface
        /// with the specified service lifetime in the dependency injection container.
        /// </summary>
        /// <typeparam name="TResolver">
        /// The type of the custom implementation for <see cref="IFilePathResolver"/>.
        /// This type must be a class and implement the <see cref="IFilePathResolver"/> interface.
        /// </typeparam>
        /// <param name="serviceLifetime">
        /// The <see cref="ServiceLifetime"/> defining the lifetime of the registered service.
        /// Supported lifetimes are <see cref="ServiceLifetime.Singleton"/>, <see cref="ServiceLifetime.Scoped"/>,
        /// and <see cref="ServiceLifetime.Transient"/>.
        /// </param>
        /// <returns>The modified <see cref="IServiceCollection"/> instance for further configuration.</returns>
        /// <exception cref="NotSupportedException">Thrown if an unsupported <see cref="ServiceLifetime"/> is specified.</exception>
        public IServiceCollection UseCustomFilePathResolver<
#if NET8_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
            TResolver>(
            ServiceLifetime serviceLifetime)
            where TResolver : class, IFilePathResolver
        {
            switch (serviceLifetime)
            {
                case ServiceLifetime.Singleton:
                    services.AddSingleton<IFilePathResolver, TResolver>();
                    return services;
                case ServiceLifetime.Scoped:
                    services.AddScoped<IFilePathResolver, TResolver>();
                    return services;
                case ServiceLifetime.Transient:
                    services.AddTransient<IFilePathResolver, TResolver>();
                    return services;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}