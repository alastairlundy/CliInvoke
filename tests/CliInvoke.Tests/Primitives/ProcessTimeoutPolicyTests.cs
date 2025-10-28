using System;
using AlastairLundy.CliInvoke.Core;
using Bogus;
using Xunit;

// ReSharper disable NotAccessedVariable

namespace AlastairLundy.CliInvoke.Tests.Primitives;

public class ProcessTimeoutPolicyTests
{
    private readonly Faker _faker = new Faker();

    [Fact]
    public void Add_TimeoutThreshold_ShouldSetValidTimeout()
    {
        // Arrange
        TimeSpan timeout = TimeSpan.FromSeconds(_faker.Random.Int(0, 1000));

        // Act
        ProcessTimeoutPolicy timeoutPolicy = new ProcessTimeoutPolicy(timeout);

        // Assert
        Assert.Equal(timeout, timeoutPolicy.TimeoutThreshold);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(-0.5)]
    [InlineData(-1)]
    public void Add_TimeoutThreshold_ShouldNotSetNegativeTimeout(double timeoutSpanSeconds)
    {
        // Arrange
        TimeSpan timeout = TimeSpan.FromSeconds(timeoutSpanSeconds);
        
        // Act
        ProcessTimeoutPolicy processTimeoutPolicy;
        
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(()=> processTimeoutPolicy = new ProcessTimeoutPolicy(timeout) );
    }

    [Theory]
    [InlineData(ProcessCancellationMode.None)]
    [InlineData(ProcessCancellationMode.Graceful)]
    [InlineData(ProcessCancellationMode.Forceful)]
    public void Add_CancellationMode_ShouldSetValidValue(ProcessCancellationMode mode)
    {
        // Arrange
        ProcessCancellationMode cancellationMode = mode;
        
        // Act
        ProcessTimeoutPolicy policy = new ProcessTimeoutPolicy(TimeSpan.Zero, cancellationMode);
        
        // Assert
        Assert.Equal(cancellationMode, policy.CancellationMode);
    }

    [Theory]
    [InlineData(ProcessCancellationMode.None)]
    [InlineData(ProcessCancellationMode.Graceful)]
    [InlineData(ProcessCancellationMode.Forceful)]
    public void FullyConfigured_ShouldEqualPolicy(ProcessCancellationMode mode)
    {
        // Arrange
        ProcessCancellationMode cancellationMode = mode;
        TimeSpan timeoutThreshold = TimeSpan.FromSeconds(_faker.Random.Int(0, 1000));
        
        // Act
        ProcessTimeoutPolicy policy = new ProcessTimeoutPolicy(timeoutThreshold, cancellationMode);
        
        // Assert
        Assert.NotNull(policy);
        Assert.Equal(cancellationMode, policy.CancellationMode);
        Assert.Equal(timeoutThreshold, policy.TimeoutThreshold);
    }
}