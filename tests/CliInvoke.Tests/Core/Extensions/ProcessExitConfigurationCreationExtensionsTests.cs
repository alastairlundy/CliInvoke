/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Core;

namespace CliInvoke.Tests.Core.Extensions;

public class ProcessExitConfigurationCreationExtensionsTests
{
    [Test]
    public async Task Default_ReturnsGracefulDefault()
    {
        // Act
        ProcessExitConfiguration config = ProcessExitConfiguration.Default;

        // Assert
        await Assert.That(config.RequestedCancellationExitBehaviour).IsEqualTo(ProcessExitBehaviour.GracefulExit);
        await Assert.That(config.TimeoutPolicy).IsEqualTo(ProcessTimeoutPolicy.Default);
        await Assert.That(config.CancellationThrowsException).IsFalse();
    }

    [Test]
    public async Task Create_WithSuppressExceptions_SetsSuppressedBehaviour()
    {
        // Arrange
        ProcessTimeoutPolicy policy = new(TimeSpan.FromSeconds(30), true);

        // Act
        ProcessExitConfiguration config = ProcessExitConfiguration.Create(
            policy, ProcessExitBehaviour.GracefulExit, suppressExceptions: true);

        // Assert
        await Assert.That(config.TimeoutPolicy).IsEqualTo(policy);
        await Assert.That(config.ExceptionBehaviour).IsEqualTo(ProcessExceptionBehaviour.SuppressExceptions);
        await Assert.That(config.CancellationThrowsException).IsFalse();
    }

    [Test]
    public async Task Create_WithoutSuppressExceptions_AllowsExceptions()
    {
        // Arrange
        ProcessTimeoutPolicy policy = new(TimeSpan.FromSeconds(30), true);

        // Act
        ProcessExitConfiguration config = ProcessExitConfiguration.Create(
            policy, ProcessExitBehaviour.ForcefulExit, suppressExceptions: false);

        // Assert
        await Assert.That(config.ExceptionBehaviour)
            .IsEqualTo(ProcessExceptionBehaviour.AllowExceptionsIfUnexpected);
        await Assert.That(config.CancellationThrowsException).IsTrue();
        await Assert.That(config.RequestedCancellationExitBehaviour)
            .IsEqualTo(ProcessExitBehaviour.ForcefulExit);
    }

    [Test]
    public async Task CreateGraceful_DefaultPolicy_IsGraceful()
    {
        // Act
        ProcessExitConfiguration config = ProcessExitConfiguration.CreateGraceful();

        // Assert
        await Assert.That(config.TimeoutPolicy).IsEqualTo(ProcessTimeoutPolicy.Default);
        await Assert.That(config.RequestedCancellationExitBehaviour)
            .IsEqualTo(ProcessExitBehaviour.GracefulExit);
        await Assert.That(config.CancellationThrowsException).IsFalse();
    }

    [Test]
    public async Task CreateGraceful_WithPolicy_UsesProvidedPolicy()
    {
        // Arrange
        ProcessTimeoutPolicy policy = new(TimeSpan.FromMinutes(5), true);

        // Act
        ProcessExitConfiguration config = ProcessExitConfiguration.CreateGraceful(policy);

        // Assert
        await Assert.That(config.TimeoutPolicy).IsEqualTo(policy);
        await Assert.That(config.RequestedCancellationExitBehaviour)
            .IsEqualTo(ProcessExitBehaviour.GracefulExit);
        await Assert.That(config.CancellationThrowsException).IsFalse();
    }

    [Test]
    public async Task CreateForceful_DefaultPolicy_IsForceful()
    {
        // Act
        ProcessExitConfiguration config = ProcessExitConfiguration.CreateForceful();

        // Assert - the forceful behaviour applies to the timeout policy, not cancellation
        await Assert.That(config.TimeoutPolicy.TimeoutExitBehaviour)
            .IsEqualTo(ProcessExitBehaviour.ForcefulExit);
        await Assert.That(config.TimeoutPolicy.TimeoutThreshold)
            .IsEqualTo(ProcessTimeoutPolicy.Default.TimeoutThreshold);
        await Assert.That(config.TimeoutPolicy.Enabled)
            .IsEqualTo(ProcessTimeoutPolicy.Default.Enabled);
        await Assert.That(config.RequestedCancellationExitBehaviour)
            .IsEqualTo(ProcessExitBehaviour.GracefulExit);
    }

    [Test]
    public async Task CreateForceful_WithPolicy_AppliesForcefulExitBehaviour()
    {
        // Arrange
        ProcessTimeoutPolicy sourcePolicy = new(TimeSpan.FromSeconds(10), true);

        // Act
        ProcessExitConfiguration config = ProcessExitConfiguration.CreateForceful(sourcePolicy);

        // Assert
        await Assert.That(config.RequestedCancellationExitBehaviour)
            .IsEqualTo(ProcessExitBehaviour.GracefulExit);
        await Assert.That(config.TimeoutPolicy.TimeoutExitBehaviour)
            .IsEqualTo(ProcessExitBehaviour.ForcefulExit);
        await Assert.That(config.TimeoutPolicy.TimeoutThreshold)
            .IsEqualTo(sourcePolicy.TimeoutThreshold);
        await Assert.That(config.TimeoutPolicy.Enabled).IsEqualTo(sourcePolicy.Enabled);
    }
}
