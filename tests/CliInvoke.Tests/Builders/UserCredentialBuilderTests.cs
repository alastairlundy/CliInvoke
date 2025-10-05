using System.Diagnostics.CodeAnalysis;
using System.Security;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.CliInvoke.Core.Primitives;
using Bogus;
using Xunit;
// ReSharper disable JoinDeclarationAndInitializer

namespace AlastairLundy.CliInvoke.Tests.Builders;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class UserCredentialBuilderTests
{
    private readonly Faker _faker = new Faker();
    
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
        string userName = _faker.Internet.UserName();
       
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
    public void WithDomain_ShouldSetDomain()
    {
        //Arrange
        string domain = _faker.Internet.DomainName();


        //Act
        IUserCredentialBuilder builder = new UserCredentialBuilder()
            .SetDomain(domain);

        UserCredential credential = builder.Build();
        
        //Assert
#pragma warning disable CA1416
        Assert.Equal(expected: credential.Domain, actual: domain);
#pragma warning restore CA1416
    }

    [Fact]
    public void LoadUserProfile_True_ShouldSetUserProfile()
    {
        //Arrange
        bool loadUserProfile = true;
        
        //Arrange
        IUserCredentialBuilder builder = new UserCredentialBuilder()
            .LoadUserProfile(loadUserProfile);
        
        UserCredential credential = builder.Build();
        
        //Assert
        Assert.True(credential.LoadUserProfile);
    }
    
    [Fact]
    public void LoadUserProfile_False_ShouldNotSetUserProfile()
    {
        //Arrange
        bool loadUserProfile = false;
        
        //Arrange
        IUserCredentialBuilder builder = new UserCredentialBuilder()
            .LoadUserProfile(loadUserProfile);
        
        UserCredential credential = builder.Build();
        
        //Assert
        Assert.False(credential.LoadUserProfile);
    }
    
    [Fact]
    public void Build_All_ShouldReturnCredential()
    {
        //Arrange
        IUserCredentialBuilder builder;
        
        SecureString password = new SecureString();
        password.AppendChar('f');
        password.AppendChar('a');
        password.AppendChar('k');
        password.AppendChar('e');
       
        string domain =  _faker.Internet.DomainName();
        string userName = _faker.Internet.UserName();
        bool loadUserProfile = true;
        
        //Act
        builder = new UserCredentialBuilder()
            .SetDomain(domain)
            .SetUsername(userName)
            .SetPassword(password)
            .LoadUserProfile(loadUserProfile);
        
        UserCredential credential = builder.Build();
        
        //Assert
        Assert.NotNull(credential);
        Assert.NotNull(credential.Domain);
        Assert.NotNull(credential.Password);
        Assert.NotNull(credential.UserName);
        Assert.NotNull(credential.LoadUserProfile);
        
        Assert.Equal(credential.Domain, domain);
        Assert.Equal(credential.UserName, userName);
        Assert.Equal(credential.Password, password);
        Assert.Equal(credential.LoadUserProfile, loadUserProfile);
    }
}