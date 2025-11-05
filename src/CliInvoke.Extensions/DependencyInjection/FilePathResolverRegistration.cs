/*
    CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System;

using AlastairLundy.CliInvoke.Core;

using Microsoft.Extensions.DependencyInjection;

namespace AlastairLundy.CliInvoke.Extensions;

public static class FilePathResolverRegistration
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="serviceLifetime"></param>
    /// <typeparam name="TResolver"></typeparam>
    /// <returns></returns>
    public static IServiceCollection UseFilePathResolver<TResolver>(this IServiceCollection services, ServiceLifetime serviceLifetime)
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