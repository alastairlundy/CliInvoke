using AlastairLundy.CliInvoke.Builders;
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
        // TODO: Test WithUsername method
    }

    [Fact]
    public void WithPassword_ShouldSetPassword()
    {
        // TODO: Test WithPassword method
    }

    [Fact]
    public void Build_ShouldReturnCredential()
    {
        // TODO: Test Build method
    }
}