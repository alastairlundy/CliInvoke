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
using CliInvoke.Validation;

namespace CliInvoke.Tests.Middleware.Integration;

public class PostExitValidationMiddlewareIntegrationTests
{
    [Test]
    public async Task UsePostExitValidation_ZeroExit_DoesNotThrow()
    {
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
        ProcessMiddlewareBuilder builder = new(_ => throw new InvalidOperationException("Not expected"));
        IReadOnlyList<IProcessMiddleware> middlewares = builder.Build();
        ProcessInvoker invoker = new ProcessInvoker(new ExternalProcessFactory(), middlewares, null);

        ProcessConfiguration config = ProcessConfigurationFactory.Create("dotnet", "--version");

        ProcessResult result = await invoker.ExecuteAsync(config, new ProcessExitConfiguration());

        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task UsePostExitValidation_MultipleRules_NamesFirstFailingRule()
    {
        IProcessResultValidator<ProcessResult> validator = new ProcessResultValidator<ProcessResult>(
        [
            new ValidationRule<ProcessResult>(_ => false, "FirstFailingRule", "The first rule failed."),
            new ValidationRule<ProcessResult>(_ => false, "SecondFailingRule", "The second rule failed.")
        ]);

        ProcessMiddlewareBuilder builder = new(_ => throw new InvalidOperationException("Not expected"));
        builder.UsePostExitValidation(validator);
        IReadOnlyList<IProcessMiddleware> middlewares = builder.Build();
        ProcessInvoker invoker = new ProcessInvoker(new ExternalProcessFactory(), middlewares, null);

        ProcessConfiguration config = ProcessConfigurationFactory.Create("dotnet", "--version");

        ProcessValidationException exception = await Assert.That(async () => await invoker.ExecuteAsync(
                config,
                ProcessExitConfiguration.CreateGraceful()))
            .Throws<ProcessValidationException>();

        await Assert.That(exception.Message).IsEqualTo("The first rule failed.");
    }

    [Test]
    public async Task UsePostExitValidation_WithExitConfigRules_ThrowsOnceNamingFirstRule()
    {
        IProcessResultValidator<ProcessResult> validator = new ProcessResultValidator<ProcessResult>(
        [
            new ValidationRule<ProcessResult>(_ => false, "SugarRule", "The sugar rule failed.")
        ]);

        ProcessMiddlewareBuilder builder = new(_ => throw new InvalidOperationException("Not expected"));
        builder.UsePostExitValidation(validator);
        IReadOnlyList<IProcessMiddleware> middlewares = builder.Build();
        ProcessInvoker invoker = new ProcessInvoker(new ExternalProcessFactory(), middlewares, null);

        ProcessConfiguration config = ProcessConfigurationFactory.Create("dotnet", "--version");

        ProcessExitConfiguration exit = new ProcessExitConfiguration
        {
            ValidationRules =
            [
                new ValidationRule<ProcessResult>(_ => false, "ConfigRule", "The configuration rule failed.")
            ]
        };

        ProcessValidationException exception = await Assert.That(async () => await invoker.ExecuteAsync(config, exit))
            .Throws<ProcessValidationException>();

        await Assert.That(exception.Message).IsEqualTo("The configuration rule failed.");
    }
}