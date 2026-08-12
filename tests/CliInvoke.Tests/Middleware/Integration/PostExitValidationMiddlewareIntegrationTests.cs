/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Core.Exceptions;
using CliInvoke.Extensions.Middleware.Validation;
using CliInvoke.Factories;

namespace CliInvoke.Tests.Middleware.Integration;

public class PostExitValidationMiddlewareIntegrationTests
{
    [Test]
    public async Task UsePostExitValidation_ZeroExit_DoesNotThrow()
    {
        // `dotnet --version` is a portable command that exits 0.
        var invoker = new ProcessInvoker(new ExternalProcessFactory())
            .UsePostExitValidation(PostExitValidationOptions.ExitCodeIsZero());

        using ProcessConfiguration config = ProcessConfigurationFactory.Create("dotnet", "--version");

        ProcessResult result = await invoker.ExecuteAsync(
            config,
            ProcessExitConfiguration.CreateGraceful());

        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task UsePostExitValidation_NonZeroExit_ThrowsProcessValidationException()
    {
        // `dotnet --this-flag-does-not-exist` is a portable command that exits non-zero.
        var invoker = new ProcessInvoker(new ExternalProcessFactory())
            .UsePostExitValidation(PostExitValidationOptions.ExitCodeIsZero());

        using ProcessConfiguration config =
            ProcessConfigurationFactory.Create("dotnet", "--this-flag-does-not-exist");

        await Assert.That(async () => await invoker.ExecuteAsync(
                config,
                ProcessExitConfiguration.CreateGraceful()))
            .Throws<ProcessValidationException>();
    }
}
