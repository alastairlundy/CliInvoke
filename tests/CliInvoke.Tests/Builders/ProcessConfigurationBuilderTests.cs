using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Text;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.DotPrimitives.Processes;
using AlastairLundy.DotPrimitives.Processes.Policies;
using AlastairLundy.DotPrimitives.Processes.Results;
using Xunit;
#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace AlastairLundy.CliInvoke.Tests.Builders
{
        public class ProcessConfigurationBuilderTests
        {

                [Fact]
                public void TestDefaultConfiguration()
                {
                        IProcessConfigurationBuilder commandBuilder = new ProcessConfigurationBuilder("foo");

                        var builtCommand = commandBuilder.Build();
                        Assert.Equal("foo", builtCommand.TargetFilePath);
                        Assert.Equal(string.Empty, builtCommand.Arguments);
                        Assert.Equal(Directory.GetCurrentDirectory(), builtCommand.WorkingDirectoryPath);

                        Assert.Equal(new Dictionary<string, string>(), builtCommand.EnvironmentVariables);
                        Assert.True(builtCommand.StandardInputEncoding.Equals(Encoding.Default) &&
                                    builtCommand.StandardOutputEncoding.Equals(Encoding.Default) &&
                                    builtCommand.StandardErrorEncoding.Equals(Encoding.Default));

                        Assert.Equal(builtCommand.Credential, UserCredential.Null);
                        Assert.Equal(ProcessResultValidation.ExitCodeZero, builtCommand.ResultValidation);

                        Assert.Equal(builtCommand.StandardInput, StreamWriter.Null);
                        Assert.Equal(builtCommand.StandardOutput, StreamReader.Null);
                        Assert.Equal(builtCommand.StandardError, StreamReader.Null);

                        Assert.Equal(builtCommand.Credential, UserCredential.Null);
                
                        Assert.Equal(ProcessResourcePolicy.Default, builtCommand.ResourcePolicy);

                        Assert.False(builtCommand.WindowCreation);
                        Assert.False(builtCommand.UseShellExecution);
                        Assert.False(builtCommand.RequiresAdministrator);
                }

                [Fact]
                public void TestIncompatiblePipingOptionsThrowsException()
                {
                        IProcessConfigurationBuilder commandBuilder = new ProcessConfigurationBuilder("foo");

                        //Assert
                        Assert.Throws<ArgumentException>(() =>
                        {
                                commandBuilder.WithShellExecution(true)
                                        .WithStandardOutputPipe(new StreamReader(Console.OpenStandardOutput()));
                        });
                
                        Assert.Throws<ArgumentException>(() =>
                        {
                                commandBuilder.WithShellExecution(true)
                                        .WithStandardErrorPipe(new StreamReader(Console.OpenStandardError()));
                        });
                
                        Assert.Throws<ArgumentException>(() =>
                        {
                                commandBuilder.WithShellExecution(true)
                                        .WithStandardInputPipe(new StreamWriter(Console.OpenStandardInput()));
                        });
                }

                [Fact]
                public void TestTargetFileReconfigured()
                { 
                        //Arrange
                        IProcessConfigurationBuilder commandBuilder = new ProcessConfigurationBuilder("foo");
              
                        //Act
                        commandBuilder = commandBuilder.WithTargetFile("bar");
              
                        //Assert
                        ProcessConfiguration command = commandBuilder.Build();
                        Assert.Equal("bar", command.TargetFilePath);
                }

                [Fact]
                public void TestArgumentsReplaced()
                {
                        //Arrange
                        IProcessConfigurationBuilder commandBuilder = new ProcessConfigurationBuilder("foo")
                                .WithArguments("--arg-value=value");
             
                        //Act
                        var newArguments = commandBuilder.WithArguments("--flag")
                                .Build();
             
                        //Assert
                        Assert.NotEqual(newArguments, commandBuilder.Build());
                }

                [Fact]
                public void TestValidationReconfigured()
                {
                        //Arrange
                        IProcessConfigurationBuilder commandBuilder = new ProcessConfigurationBuilder("foo")
                                .WithValidation(ProcessResultValidation.None);
                
                        //Act
                        commandBuilder = commandBuilder.WithValidation(ProcessResultValidation.ExitCodeZero);
                
                        //Assert
                        ProcessConfiguration command = commandBuilder.Build();
                        Assert.Equal(ProcessResultValidation.ExitCodeZero, command.ResultValidation);
                }

#if NET5_0_OR_GREATER
                [SupportedOSPlatform("windows")]
#endif
                [Fact]
                public void TestReconfiguredUserCredential()
                {
                        //Arrange
                        SecureString password = new SecureString();
                        password.AppendChar('1');
                        password.AppendChar('2');
                        password.AppendChar('3');
                        password.AppendChar('4');
                
                        IProcessConfigurationBuilder commandBuilder = new ProcessConfigurationBuilder("foo")
                                .WithUserCredential(new UserCredential("", "admin", password, false));
                
                        //Act
                        SecureString password2 = new SecureString();
                        password2.AppendChar('9');
                        password2.AppendChar('8');
                        password2.AppendChar('7');
                        password2.AppendChar('6');

                        UserCredential userCredential = new UserCredential("", "root", password2, false);
                
                        commandBuilder = commandBuilder.WithUserCredential(userCredential);
                
                        //Assert
                        ProcessConfiguration command = commandBuilder.Build();
                        Assert.Equal(userCredential, command.Credential);
                }

#if NET5_0_OR_GREATER
                [SupportedOSPlatform("windows")]
                [SupportedOSPlatform("linux")]
                [SupportedOSPlatform("macos")]
                [SupportedOSPlatform("freebsd")]
#endif
                [Fact]
                public void TestReconfiguredResourcePolicy()
                {
                        //Arrange
                        IProcessConfigurationBuilder commandBuilder = new ProcessConfigurationBuilder("foo")
                                .WithProcessResourcePolicy(ProcessResourcePolicy.Default);
                
                
                        //Arrange
                        ProcessResourcePolicy resourcePolicy = new ProcessResourcePolicy(null,
                                null,
                                null,
                                ProcessPriorityClass.AboveNormal);
                
                        commandBuilder = commandBuilder.WithProcessResourcePolicy(resourcePolicy);
                
                        //Assert
                        ProcessConfiguration command = commandBuilder.Build();
                        Assert.Equal(resourcePolicy, command.ResourcePolicy);
                }

                [Fact]
                public void TestReconfiguredAdminPrivileges()
                {
                        //Act
                        IProcessConfigurationBuilder commandBuilder = new ProcessConfigurationBuilder("foo")
                                .WithAdministratorPrivileges(false);
             
                        //Arrange
                        commandBuilder = commandBuilder.WithAdministratorPrivileges(true);
             
                        //Assert
                        ProcessConfiguration command = commandBuilder.Build();
                        Assert.True(command.RequiresAdministrator);
                }

                [Fact]
                public void TestReconfiguredWorkingDirectory()
                {
                        //Act
                        IProcessConfigurationBuilder commandBuilder = new ProcessConfigurationBuilder("foo")
                                .WithWorkingDirectory("dir");
                
                        //Arrange
                        commandBuilder = commandBuilder.WithWorkingDirectory("dir2");
                
                        //Assert
                        ProcessConfiguration command = commandBuilder.Build();
                        Assert.Equal("dir2", command.WorkingDirectoryPath);
                }
        }
}