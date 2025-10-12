using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Core.Builders;
using Xunit;
// ReSharper disable JoinDeclarationAndInitializer
// ReSharper disable NotAccessedVariable

namespace AlastairLundy.CliInvoke.Tests.Builders;

public class ProcessResourcePolicyBuilderTests
{
    
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [Theory]
    [InlineData(2 * 16 - 1)]
    [InlineData(1 * 16 - 1)]
    [InlineData(1 * 8 - 1)]
    public void WithProcessorAffinity_ValidProcessorAffinity_Valid_Success(nint processorAffinity)
    {
        // Arrange
        IProcessResourcePolicyBuilder processResourcePolicyBuilder;
        
        // Act
        processResourcePolicyBuilder = new ProcessResourcePolicyBuilder()
            .WithProcessorAffinity(processorAffinity);
        
        ProcessResourcePolicy resourcePolicy =  processResourcePolicyBuilder.Build();
        
        Assert.NotNull(resourcePolicy.ProcessorAffinity);
        Assert.Equal(processorAffinity, resourcePolicy.ProcessorAffinity);
    }
    
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [Theory]
    [InlineData(2 * 24)]
    [InlineData(0)]
    public void WithProcessorAffinity_ValidProcessorAffinity_Invalid_Fail(nint processorAffinity)
    {
        // Arrange
        IProcessResourcePolicyBuilder processResourcePolicyBuilder;
        
        // Act and Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => processResourcePolicyBuilder = new ProcessResourcePolicyBuilder()
            .WithProcessorAffinity(processorAffinity));
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
            .WithPriorityBoost(enablePriorityBoost);
        
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
            .WithPriorityClass(processPriorityClass);
        
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
            .WithMinWorkingSet(minWorkingSet);
        
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
                .WithMinWorkingSet(minWorkingSet));
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
            .WithMinWorkingSet(minWorkingSet)
            .WithMaxWorkingSet(maxWorkingSet);
        
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
                .WithMinWorkingSet(minWorkingSet).WithMaxWorkingSet(maxWorkingSet));
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("linux")]
    [Theory]
    [InlineData(2 * 16 - 1,1024_000, 8192, true, ProcessPriorityClass.AboveNormal)]
    [InlineData(1 * 16 -1, 8192, 1024, false, ProcessPriorityClass.Normal)]
    [InlineData(1 * 8 - 1, 1024, 0, false,  ProcessPriorityClass.Normal)]
    [InlineData(2 * 8 - 1, 1024, 1024, true,   ProcessPriorityClass.BelowNormal)]
    public void Build_Successfully(nint processorAffinity, nint maxWorkingSet, nint minWorkingSet,
         bool priorityBoostEnabled, ProcessPriorityClass priorityClass)
    {
        // Arrange
        IProcessResourcePolicyBuilder processResourcePolicyBuilder;
        
        // Act
#pragma warning disable CA1416
        processResourcePolicyBuilder = new ProcessResourcePolicyBuilder()
            .WithProcessorAffinity(processorAffinity)
#pragma warning restore CA1416
            .WithPriorityBoost(priorityBoostEnabled)
            .WithPriorityClass(priorityClass)
            .WithMinWorkingSet(minWorkingSet)
            .WithMaxWorkingSet(maxWorkingSet);
        
        ProcessResourcePolicy resourcePolicy =  processResourcePolicyBuilder.Build();
        
#pragma warning disable CA1416

        // Assert
        Assert.NotNull(resourcePolicy.ProcessorAffinity);
        Assert.NotNull(resourcePolicy.MinWorkingSet);
        Assert.NotNull(resourcePolicy.MaxWorkingSet);
        Assert.Equal(processorAffinity, resourcePolicy.ProcessorAffinity);
        Assert.Equal(minWorkingSet, resourcePolicy.MinWorkingSet);
        Assert.Equal(maxWorkingSet, resourcePolicy.MaxWorkingSet);
        Assert.Equal(priorityBoostEnabled, resourcePolicy.EnablePriorityBoost);
        Assert.Equal(priorityClass, resourcePolicy.PriorityClass);
#pragma warning restore CA1416

    }
    
}