// ReSharper disable NotAccessedVariable

namespace CliInvoke.Tests.Primitives;

public class ProcessTimeoutPolicyTests
{
    private readonly Faker _faker = new();

    [Test]
    public async Task Add_TimeoutThreshold_ShouldSetValidTimeout()
    {
        // Arrange
        TimeSpan timeout = TimeSpan.FromSeconds(_faker.Random.Int(0, 1000));

        // Act
        ProcessTimeoutPolicy timeoutPolicy = ProcessTimeoutPolicy.FromTimeSpan(timeout);

        // Assert
        await Assert.That(timeoutPolicy.TimeoutThreshold).IsEqualTo(timeout);
    }

    [Test]
    [Arguments(-0.001)]
    [Arguments(-0.5)]
    [Arguments(-1)]
    public async Task Add_TimeoutThreshold_ShouldNotSetNegativeTimeout(double timeoutSpanSeconds)
    {
        // Arrange
        TimeSpan timeout = TimeSpan.FromSeconds(timeoutSpanSeconds);

        // Act
        ProcessTimeoutPolicy processTimeoutPolicy;

        // Assert
        await Assert.That(() =>
            processTimeoutPolicy = ProcessTimeoutPolicy.FromTimeSpan(timeout)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task FullyConfigured_ShouldEqualPolicy()
    {
        // Arrange
        TimeSpan timeoutThreshold = TimeSpan.FromSeconds(_faker.Random.Int(0, 1000));

        // Act
        ProcessTimeoutPolicy policy = ProcessTimeoutPolicy.FromTimeSpan(timeoutThreshold);

        // Assert
        await Assert.That(policy).IsNotNull();
        /*Assert.Equal(cancellationMode, policy.C);
        Assert.Equal(timeoutThreshold, policy.TimeoutThreshold);*/
    }
}