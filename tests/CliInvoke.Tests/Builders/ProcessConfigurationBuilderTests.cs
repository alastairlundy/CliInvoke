using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Text;

using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core.Builders;
using Xunit;

using System.Runtime.Versioning;
using AlastairLundy.CliInvoke.Core;


namespace AlastairLundy.CliInvoke.Tests.Builders;

public class ProcessConfigurationBuilderTests
{
        [Fact]
        public void TestDefaultConfiguration()
        {
                // Arrange
                IProcessConfigurationBuilder processConfigBuilder = new ProcessConfigurationBuilder("foo");
                
                // Act
                ProcessConfiguration builtCommand = processConfigBuilder.Build();
                
                // Assert 
                Assert.Equal("foo", builtCommand.TargetFilePath);
                Assert.Equal(string.Empty, builtCommand.Arguments);
                Assert.Equal(Directory.GetCurrentDirectory(), builtCommand.WorkingDirectoryPath);
                Assert.Equal(new Dictionary<string, string>(), builtCommand.EnvironmentVariables);
                Assert.True(builtCommand.StandardInputEncoding.Equals(Encoding.Default) &&
                            builtCommand.StandardOutputEncoding.Equals(Encoding.Default) &&
                            builtCommand.StandardErrorEncoding.Equals(Encoding.Default));
                Assert.Equal(builtCommand.Credential, UserCredential.Null);
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
        public void WithResourcePolicy_ShouldSetResourcePolicy()
        {
                // TODO: Test WithResourcePolicy method
        }

        [Fact]
        public void Build_ShouldReturnConfiguration()
        {
                // TODO: Test Build method
        }

        [Fact]
        public void TestIncompatiblePipingOptionsThrowsException()
        {
                IProcessConfigurationBuilder processConfigBuilder = new ProcessConfigurationBuilder("foo");

                //Assert
                Assert.Throws<ArgumentException>(() =>
                {
                        processConfigBuilder.WithShellExecution(true)
                                .WithStandardOutputPipe(new StreamReader(Console.OpenStandardOutput()));
                });
                
                Assert.Throws<ArgumentException>(() =>
                {
                        processConfigBuilder.WithShellExecution(true)
                                .WithStandardErrorPipe(new StreamReader(Console.OpenStandardError()));
                });
                
                Assert.Throws<ArgumentException>(() =>
                {
                        processConfigBuilder.WithShellExecution(true)
                                .WithStandardInputPipe(new StreamWriter(Console.OpenStandardInput()));
                });
        }

        [Fact]
        public void TestTargetFileReconfigured()
        { 
                //Arrange
                IProcessConfigurationBuilder processConfigBuilder = new ProcessConfigurationBuilder("foo");
              
                //Act
                processConfigBuilder = processConfigBuilder.WithTargetFile("bar");
              
                //Assert
                ProcessConfiguration command = processConfigBuilder.Build();
                Assert.Equal("bar",
                        command.TargetFilePath);
        }

        [Fact]
        public void TestArgumentsReplaced()
        {
                //Arrange
                IProcessConfigurationBuilder processConfigBuilder = new ProcessConfigurationBuilder("foo")
                        .WithArguments("--arg-value=value");
             
                //Act
                ProcessConfiguration newArguments = processConfigBuilder.WithArguments("--flag")
                        .Build();
             
                //Assert
                Assert.NotEqual(newArguments,
                        processConfigBuilder.Build());
        }


        [SupportedOSPlatform("windows")]

        [Fact]
        public void TestReconfiguredUserCredential()
        {
                //Arrange
                SecureString password = new SecureString();
                password.AppendChar('1');
                password.AppendChar('2');
                password.AppendChar('3');
                password.AppendChar('4');
                
                IProcessConfigurationBuilder processConfigBuilder = new ProcessConfigurationBuilder("foo")
                        .WithUserCredential(new UserCredential("",
                                "admin",
                                password,
                                false));
                
                //Act
                SecureString password2 = new SecureString();
                password2.AppendChar('9');
                password2.AppendChar('8');
                password2.AppendChar('7');
                password2.AppendChar('6');

                UserCredential userCredential = new UserCredential("",
                        "root",
                        password2,
                        false);
                
                processConfigBuilder = processConfigBuilder.WithUserCredential(userCredential);
                
                //Assert
                ProcessConfiguration command = processConfigBuilder.Build();
                Assert.Equal(userCredential,
                        command.Credential);
        }


        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("freebsd")]

        [Fact]
        public void TestReconfiguredResourcePolicy()
        {
                //Arrange
                IProcessConfigurationBuilder processConfigBuilder = new ProcessConfigurationBuilder("foo")
                        .WithProcessResourcePolicy(ProcessResourcePolicy.Default);
                
                
                //Arrange
                ProcessResourcePolicy resourcePolicy = new ProcessResourcePolicy(null,
                        null,
                        null,
                        ProcessPriorityClass.AboveNormal);
                
                processConfigBuilder = processConfigBuilder.WithProcessResourcePolicy(resourcePolicy);
                
                //Assert
                ProcessConfiguration command = processConfigBuilder.Build();
                Assert.Equal(resourcePolicy,
                        command.ResourcePolicy);
        }

        [Fact]
        public void TestReconfiguredAdminPrivileges()
        {
                //Act
                IProcessConfigurationBuilder processConfigBuilder = new ProcessConfigurationBuilder("foo")
                        .WithAdministratorPrivileges(false);
             
                //Arrange
                processConfigBuilder = processConfigBuilder.WithAdministratorPrivileges(true);
             
                //Assert
                ProcessConfiguration command = processConfigBuilder.Build();
                Assert.True(command.RequiresAdministrator);
        }

        [Fact]
        public void TestReconfiguredWorkingDirectory()
        {
                //Act
                IProcessConfigurationBuilder processConfigBuilder = new ProcessConfigurationBuilder("foo")
                        .WithWorkingDirectory("dir");
                
                //Arrange
                processConfigBuilder = processConfigBuilder.WithWorkingDirectory("dir2");
                
                //Assert
                ProcessConfiguration command = processConfigBuilder.Build();
                Assert.Equal("dir2",
                        command.WorkingDirectoryPath);
        }
}