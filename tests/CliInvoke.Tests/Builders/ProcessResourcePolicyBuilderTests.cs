using System.Runtime.Versioning;

// ReSharper disable JoinDeclarationAndInitializer
// ReSharper disable NotAccessedVariable

namespace CliInvoke.Tests.Builders;

public class ProcessResourcePolicyBuilderTests
{
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [Test]
    [Arguments(1 * 16 - 1)]
    [Arguments(1 * 8 - 1)]
    public async Task WithProcessorAffinity_ValidProcessorAffinity_Valid_Success(nint processorAffinity)
    {
        // Arrange
        IProcessResourcePolicyBuilder processResourcePolicyBuilder;

        // Act
        processResourcePolicyBuilder = new ProcessResourcePolicyBuilder()
            .SetProcessorAffinity(processorAffinity);

        ProcessResourcePolicy resourcePolicy = processResourcePolicyBuilder.Build();

        await Assert.That(resourcePolicy.ProcessorAffinity).IsNotNull();
        await Assert.That(resourcePolicy.ProcessorAffinity).IsEqualTo(processorAffinity);
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [Test]
    [Arguments(2 * 24)]
    [Arguments(0)]
    public async Task WithProcessorAffinity_ValidProcessorAffinity_Invalid_Fail(nint processorAffinity)
    {
        // Arrange
        IProcessResourcePolicyBuilder processResourcePolicyBuilder;

        // Act and Assert
        await Assert.That(() => processResourcePolicyBuilder =
            new ProcessResourcePolicyBuilder()
                .SetProcessorAffinity(processorAffinity)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task WithPriorityBoost_Success(bool enablePriorityBoost)
    {
        // Arrange
        IProcessResourcePolicyBuilder processResourcePolicyBuilder;

        // Act
        processResourcePolicyBuilder = new ProcessResourcePolicyBuilder()
            .ConfigurePriorityBoost(enablePriorityBoost);

        ProcessResourcePolicy resourcePolicy = processResourcePolicyBuilder.Build();

        // Assert
        await Assert.That(resourcePolicy.EnablePriorityBoost).IsEqualTo(enablePriorityBoost);
    }

    [Test]
    [Arguments(ProcessPriorityClass.High)]
    [Arguments(ProcessPriorityClass.Normal)]
    [Arguments(ProcessPriorityClass.AboveNormal)]
    [Arguments(ProcessPriorityClass.BelowNormal)]
    [Arguments(ProcessPriorityClass.Idle)]
    [Arguments(ProcessPriorityClass.RealTime)]
    public async Task WithPriorityClass_Success(ProcessPriorityClass processPriorityClass)
    {
        // Arrange
        IProcessResourcePolicyBuilder processResourcePolicyBuilder;

        // Act
        processResourcePolicyBuilder = new ProcessResourcePolicyBuilder()
            .SetPriorityClass(processPriorityClass);

        ProcessResourcePolicy resourcePolicy = processResourcePolicyBuilder.Build();

        // Assert
        await Assert.That(resourcePolicy.PriorityClass).IsEqualTo(processPriorityClass);
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    [Test]
    [Arguments(1024_000)]
    [Arguments(8192)]
    [Arguments(1024)]
    public async Task WithMinWorkingSet_Valid_Success(nint minWorkingSet)
    {
        // Arrange
        IProcessResourcePolicyBuilder processResourcePolicyBuilder;

        // Act
        processResourcePolicyBuilder = new ProcessResourcePolicyBuilder()
            .SetMinWorkingSet(minWorkingSet);

        ProcessResourcePolicy resourcePolicy = processResourcePolicyBuilder.Build();

        // Assert
        await Assert.That(resourcePolicy.MinWorkingSet).IsNotNull();
        await Assert.That(resourcePolicy.MinWorkingSet).IsEqualTo(minWorkingSet);
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    [Test]
    [Arguments(-1000)]
    [Arguments(-1)]
    public async Task WithMinWorkingSet_Invalid_Fail(nint minWorkingSet)
    {
        // Arrange
        IProcessResourcePolicyBuilder processResourcePolicyBuilder;

        // Act
        // and Assert
        await Assert.That(() => processResourcePolicyBuilder =
            new ProcessResourcePolicyBuilder()
                .SetMinWorkingSet(minWorkingSet)).Throws<ArgumentOutOfRangeException>();
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    [Test]
    [Arguments(1024_000, 8192)]
    [Arguments(8192, 1024)]
    [Arguments(1024, 0)]
    [Arguments(1024, 1024)]
    public async Task WithMaxWorkingSet_Valid_Success(nint maxWorkingSet, nint minWorkingSet)
    {
        // Arrange
        IProcessResourcePolicyBuilder processResourcePolicyBuilder;

        // Act
        processResourcePolicyBuilder = new ProcessResourcePolicyBuilder()
            .SetMinWorkingSet(minWorkingSet)
            .SetMaxWorkingSet(maxWorkingSet);

        ProcessResourcePolicy resourcePolicy = processResourcePolicyBuilder.Build();

        // Assert
        await Assert.That(resourcePolicy.MaxWorkingSet).IsNotNull();
        await Assert.That(resourcePolicy.MaxWorkingSet).IsEqualTo(maxWorkingSet);
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    [Test]
    [Arguments(8192, 8200)]
    [Arguments(1024, 2000)]
    [Arguments(-1, -1)]
    [Arguments(0, 0)]
    public async Task WithMaxWorkingSet_Invalid_Fail(nint maxWorkingSet, nint minWorkingSet)
    {
        // Arrange
        IProcessResourcePolicyBuilder processResourcePolicyBuilder;

        //Act
        // and Assert
        await Assert.That(() => processResourcePolicyBuilder =
            new ProcessResourcePolicyBuilder()
                .SetMinWorkingSet(minWorkingSet).SetMaxWorkingSet(maxWorkingSet)).Throws<ArgumentOutOfRangeException>();
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("linux")]
    [Test]
    [Arguments(2 * 16 - 1, 1024_000, 8192, true, ProcessPriorityClass.AboveNormal)]
    [Arguments(1 * 16 - 1, 8192, 1024, false, ProcessPriorityClass.Normal)]
    [Arguments(1 * 8 - 1, 1024, 0, false, ProcessPriorityClass.Normal)]
    [Arguments(2 * 8 - 1, 1024, 1024, true, ProcessPriorityClass.BelowNormal)]
    public async Task Build_Successfully(nint processorAffinity, nint maxWorkingSet, nint minWorkingSet,
        bool priorityBoostEnabled, ProcessPriorityClass priorityClass)
    {
        // Arrange
        IProcessResourcePolicyBuilder processResourcePolicyBuilder;

        // Act
        processResourcePolicyBuilder = new ProcessResourcePolicyBuilder()
            .ConfigurePriorityBoost(priorityBoostEnabled);

        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacCatalyst() ||
            OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
            processResourcePolicyBuilder = processResourcePolicyBuilder
                .SetPriorityClass(priorityClass)
                .SetMinWorkingSet(minWorkingSet)
                .SetMaxWorkingSet(maxWorkingSet);

        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
            processResourcePolicyBuilder = processResourcePolicyBuilder
                .SetProcessorAffinity(processorAffinity);

        ProcessResourcePolicy resourcePolicy = processResourcePolicyBuilder.Build();

#pragma warning disable CA1416

        // Assert
        await Assert.That(resourcePolicy.ProcessorAffinity).IsNotNull();

        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacCatalyst() ||
            OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
        {
            await Assert.That(resourcePolicy.MinWorkingSet).IsNotNull();
            await Assert.That(resourcePolicy.MaxWorkingSet).IsNotNull();
            await Assert.That(resourcePolicy.MinWorkingSet).IsEqualTo(minWorkingSet);
            await Assert.That(resourcePolicy.MaxWorkingSet).IsEqualTo(maxWorkingSet);
            await Assert.That(resourcePolicy.PriorityClass).IsEqualTo(priorityClass);
        }

        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
            await Assert.That(resourcePolicy.ProcessorAffinity).IsEqualTo(processorAffinity);

        await Assert.That(resourcePolicy.EnablePriorityBoost).IsEqualTo(priorityBoostEnabled);
#pragma warning restore CA1416
    }
}