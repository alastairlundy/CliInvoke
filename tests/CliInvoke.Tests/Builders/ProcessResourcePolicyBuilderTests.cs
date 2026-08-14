using System.Collections.Generic;
using System.Collections.Generic;
using System.Runtime.Versioning;
using Assert = Xunit.Assert;

// ReSharper disable JoinDeclarationAndInitializer
// ReSharper disable NotAccessedVariable

namespace CliInvoke.Tests.Builders;

public class ProcessResourcePolicyBuilderTests
{
    private static nint ComputeMaxAffinity()
    {
        int processorCount = Environment.ProcessorCount;
        int nativeWidth = IntPtr.Size * 8;

        if (processorCount >= nativeWidth)
        {
            return nint.MaxValue;
        }

        return ((nint)1 << processorCount) - 1;
    }

    public static IEnumerable<object[]> ValidProcessorAffinityValues()
    {
        nint maxAffinity = ComputeMaxAffinity();
        nint smallMask1 = (nint)0x0001;
        nint smallMask2 = (nint)0x0003;
        nint smallMask4 = (nint)0x000F;

        if (smallMask4 <= maxAffinity)
        {
            yield return new object[] { smallMask4 };
        }

        if (smallMask2 <= maxAffinity && smallMask2 != smallMask4)
        {
            yield return new object[] { smallMask2 };
        }

        if (smallMask1 <= maxAffinity && smallMask1 != smallMask2 && smallMask1 != smallMask4)
        {
            yield return new object[] { smallMask1 };
        }

        yield return new object[] { maxAffinity };
    }

    public static IEnumerable<object[]> InvalidProcessorAffinityValues()
    {
        nint maxAffinity = ComputeMaxAffinity();
        yield return new object[] { maxAffinity + 1 };
        yield return new object[] { (nint)0 };
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [Theory]
    [MemberData(nameof(ValidProcessorAffinityValues))]
    public void WithProcessorAffinity_ValidProcessorAffinity_Valid_Success(nint processorAffinity)
    {
        // Arrange
        IProcessResourcePolicyBuilder processResourcePolicyBuilder;
        
        // Act
        processResourcePolicyBuilder = new ProcessResourcePolicyBuilder()
            .SetProcessorAffinity(processorAffinity);
        
        ProcessResourcePolicy resourcePolicy =  processResourcePolicyBuilder.Build();
        
        Assert.NotNull(resourcePolicy.ProcessorAffinity);
        Assert.Equal(processorAffinity, resourcePolicy.ProcessorAffinity);
    }
    
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [Theory]
    [MemberData(nameof(InvalidProcessorAffinityValues))]
    public void WithProcessorAffinity_ValidProcessorAffinity_Invalid_Fail(nint processorAffinity)
    {
        // Arrange
        IProcessResourcePolicyBuilder processResourcePolicyBuilder;
        
        // Act and Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => processResourcePolicyBuilder = new ProcessResourcePolicyBuilder()
            .SetProcessorAffinity(processorAffinity));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithPriorityBoost_Success(bool enablePriorityBoost)
    {
        // Arrange
        IProcessResourcePolicyBuilder processResourcePolicyBuilder;
        
        // Act
        processResourcePolicyBuilder = new ProcessResourcePolicyBuilder()
            .ConfigurePriorityBoost(enablePriorityBoost);
        
        ProcessResourcePolicy resourcePolicy =  processResourcePolicyBuilder.Build();
        
        // Assert
        Assert.Equal(enablePriorityBoost, resourcePolicy.EnablePriorityBoost);
    }

    [Theory]
    [InlineData(ProcessPriorityClass.High)]
    [InlineData(ProcessPriorityClass.Normal)]
    [InlineData(ProcessPriorityClass.AboveNormal)]
    [InlineData(ProcessPriorityClass.BelowNormal)]
    [InlineData(ProcessPriorityClass.Idle)]
    [InlineData(ProcessPriorityClass.RealTime)]
    public void WithPriorityClass_Success(ProcessPriorityClass processPriorityClass)
    {
        // Arrange
        IProcessResourcePolicyBuilder processResourcePolicyBuilder;
        
        // Act
        processResourcePolicyBuilder = new ProcessResourcePolicyBuilder()
            .SetPriorityClass(processPriorityClass);
        
        ProcessResourcePolicy resourcePolicy =  processResourcePolicyBuilder.Build();
        
        // Assert
        Assert.Equal(processPriorityClass, resourcePolicy.PriorityClass);
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    [Theory]
    [InlineData(1024_000)]
    [InlineData(8192)]
    [InlineData(1024)]
    public void WithMinWorkingSet_Valid_Success(nint minWorkingSet)
    {
        // Arrange
        IProcessResourcePolicyBuilder processResourcePolicyBuilder;
        
        // Act
        processResourcePolicyBuilder = new ProcessResourcePolicyBuilder()
            .SetMinWorkingSet(minWorkingSet);
        
        ProcessResourcePolicy resourcePolicy =  processResourcePolicyBuilder.Build();
        
        // Assert
        Assert.NotNull(resourcePolicy.MinWorkingSet);
        Assert.Equal(minWorkingSet, resourcePolicy.MinWorkingSet);
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    [Theory]
    [InlineData(-1000)]
    [InlineData(-1)]
    public void WithMinWorkingSet_Invalid_Fail(nint minWorkingSet)
    {
        // Arrange
        IProcessResourcePolicyBuilder processResourcePolicyBuilder;
        
        // Act
        // and Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => processResourcePolicyBuilder =
            new ProcessResourcePolicyBuilder()
                .SetMinWorkingSet(minWorkingSet));
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    [Theory]
    [InlineData(1024_000, 8192)]
    [InlineData(8192, 1024)]
    [InlineData(1024, 0)]
    [InlineData(1024, 1024)]
    public void WithMaxWorkingSet_Valid_Success(nint maxWorkingSet, nint minWorkingSet)
    {
        // Arrange
        IProcessResourcePolicyBuilder processResourcePolicyBuilder;
        
        // Act
        processResourcePolicyBuilder = new ProcessResourcePolicyBuilder()
            .SetMinWorkingSet(minWorkingSet)
            .SetMaxWorkingSet(maxWorkingSet);
        
        ProcessResourcePolicy resourcePolicy =  processResourcePolicyBuilder.Build();
        
        // Assert
        Assert.NotNull(resourcePolicy.MaxWorkingSet);
        Assert.Equal(maxWorkingSet, resourcePolicy.MaxWorkingSet);
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    [Theory]
    [InlineData(8192, 8200)]
    [InlineData(1024, 2000)]
    [InlineData(-1, -1)]
    [InlineData(0, 0)]
    public void WithMaxWorkingSet_Invalid_Fail(nint maxWorkingSet, nint minWorkingSet)
    {
        // Arrange
        IProcessResourcePolicyBuilder processResourcePolicyBuilder;
        
        //Act
        // and Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => processResourcePolicyBuilder =
            new ProcessResourcePolicyBuilder()
                .SetMinWorkingSet(minWorkingSet).SetMaxWorkingSet(maxWorkingSet));
    }

    public static IEnumerable<object[]> BuildSuccessData()
    {
        nint maxAffinity = ComputeMaxAffinity();
        nint mask4 = (nint)0x000F;
        nint mask3 = (nint)0x0003;

        if (mask4 <= maxAffinity)
        {
            yield return new object[] { maxAffinity, 1024_000, 8192, true, ProcessPriorityClass.AboveNormal };
            yield return new object[] { mask4, 8192, 1024, false, ProcessPriorityClass.Normal };
        }

        if (mask3 <= maxAffinity)
        {
            yield return new object[] { mask3, 1024, 0, false, ProcessPriorityClass.Normal };
            yield return new object[] { maxAffinity & ~mask3, 1024, 1024, true, ProcessPriorityClass.BelowNormal };
        }
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("linux")]
    [Theory]
    [MemberData(nameof(BuildSuccessData))]
    public void Build_Successfully(nint processorAffinity, nint maxWorkingSet, nint minWorkingSet,
        bool priorityBoostEnabled, ProcessPriorityClass priorityClass)
    {
        // Arrange
        IProcessResourcePolicyBuilder processResourcePolicyBuilder;
        
        // Act
        processResourcePolicyBuilder = new ProcessResourcePolicyBuilder()
            .ConfigurePriorityBoost(priorityBoostEnabled);
        
        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacCatalyst() ||
            OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
        {
            processResourcePolicyBuilder = processResourcePolicyBuilder
                .SetPriorityClass(priorityClass)
                .SetMinWorkingSet(minWorkingSet)
                .SetMaxWorkingSet(maxWorkingSet);
        }

        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
        {
            processResourcePolicyBuilder = processResourcePolicyBuilder
                .SetProcessorAffinity(processorAffinity);
        }
        
        ProcessResourcePolicy resourcePolicy =  processResourcePolicyBuilder.Build();
        
#pragma warning disable CA1416

        // Assert
        Assert.NotNull(resourcePolicy.ProcessorAffinity);

        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacCatalyst() ||
            OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
        {
            Assert.NotNull(resourcePolicy.MinWorkingSet);
            Assert.NotNull(resourcePolicy.MaxWorkingSet); 
            Assert.Equal(minWorkingSet, resourcePolicy.MinWorkingSet);
            Assert.Equal(maxWorkingSet, resourcePolicy.MaxWorkingSet);
            Assert.Equal(priorityClass, resourcePolicy.PriorityClass);
        }

        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
        {
            Assert.Equal(processorAffinity, resourcePolicy.ProcessorAffinity);
        }

        Assert.Equal(priorityBoostEnabled, resourcePolicy.EnablePriorityBoost);
#pragma warning restore CA1416
    }
}