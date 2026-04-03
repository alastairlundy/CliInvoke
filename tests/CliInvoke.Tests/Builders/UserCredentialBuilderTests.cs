using System.Diagnostics.CodeAnalysis;
using System.Security;

// ReSharper disable JoinDeclarationAndInitializer

namespace CliInvoke.Tests.Builders;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class UserCredentialBuilderTests
{
    private readonly Faker _faker = new();

    [Test]
    public async Task Constructor_ShouldInstantiate()
    {
        UserCredentialBuilder builder = new UserCredentialBuilder();
        await Assert.That(builder).IsNotNull();
    }

    [Test]
    public async Task WithUsername_ShouldSetUsername()
    {
        // Arrange
        // ReSharper disable once JoinDeclarationAndInitializer
        IUserCredentialBuilder builder;
        string? userName = _faker.Internet.UserName();

        // Act
        builder = new UserCredentialBuilder()
            .SetUsername(userName);

        // Assert

        UserCredential credential = builder.Build();

        await Assert.That(credential.UserName).IsEqualTo(userName);
    }

    [Test]
    public async Task WithPassword_ShouldSetPassword()
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
        await Assert.That(credential.Password).IsEqualTo(password);
#pragma warning restore CA1416
    }

    [Test]
    public async Task WithDomain_ShouldSetDomain()
    {
        //Arrange
        string? domain = _faker.Internet.DomainName();


        //Act
        IUserCredentialBuilder builder = new UserCredentialBuilder()
            .SetDomain(domain);

        UserCredential credential = builder.Build();

        //Assert
#pragma warning disable CA1416
        await Assert.That(credential.Domain).IsEqualTo(domain);
#pragma warning restore CA1416
    }

    [Test]
    public async Task LoadUserProfile_True_ShouldSetUserProfile()
    {
        //Arrange
        bool loadUserProfile = true;

        //Arrange
        IUserCredentialBuilder builder = new UserCredentialBuilder()
            .LoadUserProfile(loadUserProfile);

        UserCredential credential = builder.Build();

        //Assert
        await Assert.That(credential.LoadUserProfile).IsTrue();
    }

    [Test]
    public async Task LoadUserProfile_False_ShouldNotSetUserProfile()
    {
        //Arrange
        bool loadUserProfile = false;

        //Arrange
        IUserCredentialBuilder builder = new UserCredentialBuilder()
            .LoadUserProfile(loadUserProfile);

        UserCredential credential = builder.Build();

        //Assert
        await Assert.That(credential.LoadUserProfile).IsFalse();
    }

    [Test]
    public async Task Build_All_ShouldReturnCredential()
    {
        //Arrange
        IUserCredentialBuilder builder;

        SecureString password = new SecureString();
        password.AppendChar('f');
        password.AppendChar('a');
        password.AppendChar('k');
        password.AppendChar('e');

        string? domain = _faker.Internet.DomainName();
        string? userName = _faker.Internet.UserName();
        bool loadUserProfile = true;

        //Act
        builder = new UserCredentialBuilder()
            .SetDomain(domain)
            .SetUsername(userName)
            .SetPassword(password)
            .LoadUserProfile(loadUserProfile);

        UserCredential credential = builder.Build();

        //Assert
        await Assert.That(credential).IsNotNull();
        await Assert.That(credential.Domain).IsNotNull();
        await Assert.That(credential.Password).IsNotNull();
        await Assert.That(credential.UserName).IsNotNull();
        await Assert.That(credential.LoadUserProfile).IsNotNull();

        await Assert.That(credential.Domain).IsEqualTo(domain);
        await Assert.That(credential.UserName).IsEqualTo(userName);
        await Assert.That(credential.Password).IsEqualTo(password);
        await Assert.That(credential.LoadUserProfile).IsEqualTo(loadUserProfile);
    }
}