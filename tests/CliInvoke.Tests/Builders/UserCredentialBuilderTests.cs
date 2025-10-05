using System.Security;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.CliInvoke.Core.Primitives;
using Xunit;

namespace AlastairLundy.CliInvoke.Tests.Builders;

public class UserCredentialBuilderTests
{
    [Fact]
    public void Constructor_ShouldInstantiate()
    {
        var builder = new UserCredentialBuilder();
        Assert.NotNull(builder);
    }

    [Fact]
    public void WithUsername_ShouldSetUsername()
    {
        // Arrange
        // ReSharper disable once JoinDeclarationAndInitializer
        IUserCredentialBuilder builder;
       string userName = "fakeusername";
       
       // Act
       builder = new UserCredentialBuilder()
           .SetUsername(userName);
       
       // Assert

      UserCredential credential = builder.Build();
       
       Assert.Equal(credential.UserName, userName);
    }

    [Fact]
    public void WithPassword_ShouldSetPassword()
    {
        // Arrange
        // ReSharper disable once JoinDeclarationAndInitializer
        IUserCredentialBuilder builder;
        SecureString password = new SecureString();
        password.AppendChar('f');
        password.AppendChar('a');
        password.AppendChar('k');
        password.AppendChar('e');
        
        // Act
        builder = new UserCredentialBuilder()
            .SetPassword(password);
        
        UserCredential credential = builder.Build();
       
        // Assert
#pragma warning disable CA1416
        Assert.Equal(credential.Password, password);
#pragma warning restore CA1416
    }

    [Fact]
    public void Build_ShouldReturnCredential()
    {
        // TODO: Test Build method
    }
}