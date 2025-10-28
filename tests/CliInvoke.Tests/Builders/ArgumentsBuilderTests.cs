using AlastairLundy.CliInvoke.Builders;
using Xunit;

namespace AlastairLundy.CliInvoke.Tests.Builders;

public class ArgumentsBuilderTests
{
    [Fact]
    public void BuilderChainingTest()
    {
        ArgumentsBuilder argumentsBuilder = new ArgumentsBuilder()
            .Add("new")
            .Add(["list", "--help"]);

        string expected = "new list --help";
        
        Assert.Equal(expected, argumentsBuilder.ToString());
    }
    
    
}