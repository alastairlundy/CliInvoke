/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;

using CliInvoke.Core.Exceptions;
using CliInvoke.Core.Middleware;
using CliInvoke.Core.Validation;
using CliInvoke.Extensions.Middleware;
using CliInvoke.Extensions.Middleware.Validation;
using CliInvoke.Factories;

namespace CliInvoke.Tests.Middleware.Integration;

public class PostExitValidationMiddlewareIntegrationTests
{
    [Test]
    public async Task UsePostExitValidation_ZeroExit_DoesNotThrow()
    {
        // `dotnet --version` is a portable command that exits 0.
        ProcessMiddlewareBuilder builder = new(_ => throw new InvalidOperationException("Not expected"));
        builder.UsePostExitValidation(PostExitValidation.ExitCodeIsZero());
        IReadOnlyList<IProcessMiddleware> middlewares = builder.Build();
        ProcessInvoker invoker = new ProcessInvoker(new ExternalProcessFactory(), middlewares, null);

        ProcessConfiguration config = ProcessConfigurationFactory.Create("dotnet", "--version");

        ProcessResult result = await invoker.ExecuteAsync(
            config,
            ProcessExitConfiguration.CreateGraceful());

        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task UsePostExitValidation_NonZeroExit_ThrowsProcessValidationException()
    {
        // `dotnet --this-flag-does-not-exist` is a portable command that exits non-zero.
        ProcessMiddlewareBuilder builder = new(_ => throw new InvalidOperationException("Not expected"));
        builder.UsePostExitValidation(PostExitValidation.ExitCodeIsZero());
        IReadOnlyList<IProcessMiddleware> middlewares = builder.Build();
        ProcessInvoker invoker = new ProcessInvoker(new ExternalProcessFactory(), middlewares, null);

        ProcessConfiguration config =
            ProcessConfigurationFactory.Create("dotnet", "--this-flag-does-not-exist");

        ProcessValidationException exception = await Assert.That(async () => await invoker.ExecuteAsync(
                config,
                ProcessExitConfiguration.CreateGraceful()))
            .Throws<ProcessValidationException>();

        await Assert.That(exception.Message).Contains("exit", StringComparison.OrdinalIgnoreCase);
        await Assert.That(exception.Result).IsNotNull();
    }

    [Test]
    public async Task UsePostExitValidation_StdoutMatches_ValidatesBufferedOutput()
    {
        // `dotnet --version` writes a version string (e.g. "8.0.100") to standard output.
        ProcessMiddlewareBuilder builder = new(_ => throw new InvalidOperationException("Not expected"));
        builder.UsePostExitValidation(PostExitValidation.StdoutMatches(@"\d+\.\d+"));
        IReadOnlyList<IProcessMiddleware> middlewares = builder.Build();
        ProcessInvoker invoker = new ProcessInvoker(new ExternalProcessFactory(), middlewares, null);

        ProcessConfiguration config = ProcessConfigurationFactory.Create("dotnet", "--version");

        BufferedProcessResult result = await invoker.ExecuteBufferedAsync(
            config,
            ProcessExitConfiguration.CreateGraceful());

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.StandardOutput).Contains(".");
    }

    [Test]
    public async Task ExitConfigurationValidationRules_FailingRule_ThrowsProcessValidationException()
    {
        // `dotnet --version` exits 0, so a rule that requires a non-zero exit code fails and
        // the pipeline must surface it as a ProcessValidationException.
        ProcessMiddlewareBuilder builder = new(_ => throw new InvalidOperationException("Not expected"));
        IReadOnlyList<IProcessMiddleware> middlewares = builder.Build();
        ProcessInvoker invoker = new ProcessInvoker(new ExternalProcessFactory(), middlewares, null);

        ProcessConfiguration config = ProcessConfigurationFactory.Create("dotnet", "--version");

        ProcessExitConfiguration exit = new ProcessExitConfiguration
        {
            ValidationRules = [new ValidationRule<ProcessResult>(
                result => result.ExitCode != 0,
                "ExpectedNonZeroExit",
                "The process was expected to fail validation.")]
        };

        ProcessValidationException exception = await Assert.That(async () => await invoker.ExecuteAsync(config, exit))
            .Throws<ProcessValidationException>();

        await Assert.That(exception.Result).IsNotNull();
    }

    [Test]
    public async Task ExitConfigurationValidationRules_Empty_DoesNotThrow()
    {
        // A default exit configuration declares no validation rules, so the pipeline must not throw.
        ProcessMiddlewareBuilder builder = new(_ => throw new InvalidOperationException("Not expected"));
        IReadOnlyList<IProcessMiddleware> middlewares = builder.Build();
        ProcessInvoker invoker = new ProcessInvoker(new ExternalProcessFactory(), middlewares, null);

        ProcessConfiguration config = ProcessConfigurationFactory.Create("dotnet", "--version");

        ProcessResult result = await invoker.ExecuteAsync(config, new ProcessExitConfiguration());

        await Assert.That(result.ExitCode).IsEqualTo(0);
    }
}
