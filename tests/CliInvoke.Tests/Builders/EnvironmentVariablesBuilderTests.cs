using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core.Builders;
using Bogus;
using Xunit;

namespace AlastairLundy.CliInvoke.Tests.Builders;

public class EnvironmentVariablesBuilderTests
{
    private readonly Faker _faker = new Faker();
    [Fact]
    public void Constructor_ShouldInstantiate()
    {
        EnvironmentVariablesBuilder builder = new EnvironmentVariablesBuilder();
        Assert.NotNull(builder);
    }

    [Fact]
    public void Set_SingleVariable_Success()
    {
        // Arrange
        string key = _faker.Database.Column();
        string value = _faker.Database.Random.Word();

        // Act
        IEnvironmentVariablesBuilder builder = new EnvironmentVariablesBuilder()
            .Set(key, value);

        IReadOnlyDictionary<string, string> variables = builder.Build();
        
        // Assert
        Assert.Equal(value,variables[key]);
    }

    [Fact]
    public void Set_KeyValuePairs_Sequence_Success()
    {
        int number = _faker.Random.Int(1, 20);
        HashSet<string> keys = new HashSet<string>();

        while (keys.Count < number)
        {
            keys.Add(_faker.Random.Word().ToLower());
        }
        
        List<string> vals = _faker.Make(number + 50, x =>  _faker.Random.Word())
            .Distinct(StringComparer.InvariantCultureIgnoreCase)
            .Take(number)
            .ToList();

        List<KeyValuePair<string, string>> keyValPairs = (from k in keys
            from v in vals
            select new KeyValuePair<string, string>(k, v))
            .ToList();
       
       // Act
       IEnvironmentVariablesBuilder builder = new EnvironmentVariablesBuilder()
           .Set(keyValPairs);
       
       IReadOnlyDictionary<string, string> variables = builder.Build();
       
       // Assert
       foreach (KeyValuePair<string, string> pair in keyValPairs)
       {
           Assert.Equal(pair.Value, variables[pair.Key]);
       }
    }
}