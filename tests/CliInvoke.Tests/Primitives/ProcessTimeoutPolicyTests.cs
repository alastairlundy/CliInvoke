

// ReSharper disable NotAccessedVariable

namespace CliInvoke.Tests.Primitives;

public class ProcessTimeoutPolicyTests
{
    private readonly Faker _faker = new Faker();

    [Fact]
    public void Add_TimeoutThreshold_ShouldSetValidTimeout()
    {
        // Arrange
        TimeSpan timeout = TimeSpan.FromSeconds(_faker.Random.Int(0, 1000));

        // Act
        ProcessTimeoutPolicy timeoutPolicy = ProcessTimeoutPolicy.FromTimeSpan(timeout);

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
        Assert.Throws<ArgumentOutOfRangeException>(()=> processTimeoutPolicy = ProcessTimeoutPolicy.FromTimeSpan(timeout) );
    }

    [Theory]
    [InlineData(ProcessCancellationMode.None)]
    [InlineData(ProcessCancellationMode.Graceful)]
    [InlineData(ProcessCancellationMode.Forceful)]
    public void FullyConfigured_ShouldEqualPolicy(ProcessCancellationMode mode)
    {
        // Arrange
        TimeSpan timeoutThreshold = TimeSpan.FromSeconds(_faker.Random.Int(0, 1000));
        
        // Act
        ProcessTimeoutPolicy policy = ProcessTimeoutPolicy.FromTimeSpan(timeoutThreshold);
        
        // Assert
        Assert.NotNull(policy);
        /*Assert.Equal(cancellationMode, policy.C);
        Assert.Equal(timeoutThreshold, policy.TimeoutThreshold);*/
    }
}