/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Core.Factories;
using CliInvoke.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace CliInvoke.Tests.Extensions.DependencyInjection;

/// <summary>
///     Tests for the service registrations made by <c>AddCliInvoke</c>. Every lifetime
///     registers <see cref="IProcessConfigurationBuilder"/> as a factory that resolves to
///     a blank builder: the only public <c>ProcessConfigurationBuilder</c> constructor
///     takes a target file path the container cannot supply, so the factory constructs the
///     builder with an empty target and the caller sets the target before building.
/// </summary>
public class AddCliInvokeServiceRegistrationTests
{
    [Test]
    public async Task AddCliInvoke_Registers_IProcessConfigurationBuilder_AsBlankBuilder()
    {
        ServiceLifetime[] lifetimes =
        [
            ServiceLifetime.Singleton,
            ServiceLifetime.Scoped,
            ServiceLifetime.Transient
        ];

        foreach (ServiceLifetime lifetime in lifetimes)
        {
            IServiceCollection services = new ServiceCollection();
            services.AddCliInvoke(lifetime);
            IServiceProvider provider = services.BuildServiceProvider();

            using IServiceScope scope = provider.CreateScope();

            // The blank-builder factory resolves to a builder whose target path is empty;
            // the caller sets the target via SetTargetFilePath before building.
            IProcessConfigurationBuilder resolvedBuilder =
                scope.ServiceProvider.GetRequiredService<IProcessConfigurationBuilder>();
            await Assert.That(resolvedBuilder).IsNotNull();

            // The rest of the lifetime-switched registrations still resolve.
            await Assert.That(scope.ServiceProvider.GetService<IProcessInvoker>()).IsNotNull();
            await Assert.That(scope.ServiceProvider.GetService<IExternalProcessFactory>())
                .IsNotNull();
        }
    }
}
