using CliInvoke.Builders;
using CliInvoke.Builders.Abstractions;

namespace CliInvoke.Tests.Builders;

public class ArgumentsBuilderTests
{
    [Fact]
    public void BuilderChainingTest()
    {
        IArgumentsBuilder argumentsBuilder = new ArgumentsBuilder()
            .Add("new")
            .Add(["list", "--help"]);

        string expected = "new list --help";
        
        Assert.Equal(expected, argumentsBuilder.ToString());
    }
    
     
}