using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Core.Builders;
using Bogus;
using Xunit;
// ReSharper disable JoinDeclarationAndInitializer
// ReSharper disable NotAccessedVariable

namespace AlastairLundy.CliInvoke.Tests.Builders;

public class ProcessResourcePolicyBuilderTests
{
    private Faker _faker = new();

    
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
}