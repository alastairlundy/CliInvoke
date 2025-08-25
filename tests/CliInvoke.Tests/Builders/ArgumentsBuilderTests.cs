using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core.Builders;
using Xunit;

namespace AlastairLundy.CliInvoke.Tests.Builders
{
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

        [Fact]
        public void Add_ShouldAddArgument()
        {
            // TODO: Test Add method
        }

        [Fact]
        public void Remove_ShouldRemoveArgument()
        {
            // TODO: Test Remove method
        }

        [Fact]
        public void Build_ShouldReturnArguments()
        {
            // TODO: Test Build method
        }
    }
}