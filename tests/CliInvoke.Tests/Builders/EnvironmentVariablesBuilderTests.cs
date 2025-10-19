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
            .SetPair(key, value);

        IReadOnlyDictionary<string, string> variables = builder.Build();
        
        // Assert
        Assert.Equal(value,variables[key]);
    }

    [Fact]
    public void Set_KeyValuePairs_Sequence_Success()
    {
        int number = _faker.Random.Int(1, 20);
        List<KeyValuePair<string, string>> list = new();

        while (list.Count < number)
        {
            string key = _faker.Database.Column();
            string value = _faker.Random.Word();
            
            list.Add(new KeyValuePair<string, string>(key, value));
        }
        
        list = list.DistinctBy(x => x.Key).ToList();
        
       // Act
       IEnvironmentVariablesBuilder builder = new EnvironmentVariablesBuilder()
           .SetEnumerable(list);
       
       IReadOnlyDictionary<string, string> variables = builder.Build();
       
       // Assert
       foreach (KeyValuePair<string, string> pair in list)
       {
           Assert.Equal(pair.Value, variables[pair.Key]);
       }
    }

    public void Set_Dictionary_Success()
    {
        int number = _faker.Random.Int(1, 20);

        Dictionary<string, string> dictionary = new();

        while (dictionary.Count < number)
        {
            string key = _faker.Database.Column();
            string value = _faker.Random.Word();
            
            dictionary.TryAdd(key, value);
        }
        
        // Act
        IEnvironmentVariablesBuilder builder = new EnvironmentVariablesBuilder()
            .SetDictionary(dictionary);
       
        IReadOnlyDictionary<string, string> variables = builder.Build();
       
        // Assert
        foreach (KeyValuePair<string, string> pair in dictionary)
        {
            Assert.Equal(pair.Value, variables[pair.Key]);
        }
    }
}