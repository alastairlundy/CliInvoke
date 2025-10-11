using System;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Core.Builders;
using Bogus;
using Xunit;

namespace AlastairLundy.CliInvoke.Tests.Builders;

public class ProcessTimeoutPolicyBuilderTests
{
    private readonly Faker _faker = new Faker();

    [Fact]
    public void WithTimeoutThreshold_ShouldSetValidTimeout()
    {
        // Arrange
        TimeSpan timeout = TimeSpan.FromSeconds(_faker.Random.Int(0, 1000));

        // Act
        IProcessTimeoutPolicyBuilder builder
            = new ProcessTimeoutPolicyBuilder()
                .WithTimeoutThreshold(timeout);

        ProcessTimeoutPolicy timeoutPolicy = builder.Build();

        // Assert
        Assert.Equal(timeout, timeoutPolicy.TimeoutThreshold);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(-0.5)]
    [InlineData(-1)]
    public void WithTimeoutThreshold_ShouldNotSetNegativeTimeout(double timeoutSpanSeconds)
    {
        // Arrange
        TimeSpan timeout = TimeSpan.FromSeconds(timeoutSpanSeconds);

        // Act
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProcessTimeoutPolicyBuilder()
            .WithTimeoutThreshold(timeout));
    }

    [Theory]
    [InlineData(ProcessCancellationMode.None)]
    [InlineData(ProcessCancellationMode.Graceful)]
    [InlineData(ProcessCancellationMode.Forceful)]
    public void WithCancellationMode_ShouldSetValidValue(ProcessCancellationMode mode)
    {
        // Arrange
        ProcessCancellationMode cancellationMode = mode;
        
        // Act
        IProcessTimeoutPolicyBuilder builder = new
            ProcessTimeoutPolicyBuilder()
            .WithCancellationMode(cancellationMode);
        
        ProcessTimeoutPolicy policy = builder.Build();
        
        // Assert
        Assert.Equal(cancellationMode, policy.CancellationMode);
    }

    [Theory]
    [InlineData(ProcessCancellationMode.None)]
    [InlineData(ProcessCancellationMode.Graceful)]
    [InlineData(ProcessCancellationMode.Forceful)]
    public void Build_All_ShouldReturnPolicy(ProcessCancellationMode mode)
    {
        // Arrange
        ProcessCancellationMode cancellationMode = mode;
        TimeSpan timeoutThreshold = TimeSpan.FromSeconds(_faker.Random.Int(0, 1000));
        
        // Act
        IProcessTimeoutPolicyBuilder builder = new
                ProcessTimeoutPolicyBuilder()
            .WithCancellationMode(cancellationMode)
            .WithTimeoutThreshold(timeoutThreshold);
        
        ProcessTimeoutPolicy policy = builder.Build();
        
        // Assert
        Assert.NotNull(policy);
        Assert.Equal(cancellationMode, policy.CancellationMode);
        Assert.Equal(timeoutThreshold, policy.TimeoutThreshold);
    }
}