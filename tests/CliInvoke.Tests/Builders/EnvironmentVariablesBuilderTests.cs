using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    public void Set_KeyValuePairs_Enumerable_Sequence_Success()
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

    [Fact]
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
    
    
    [Fact]
    public void Set_ReadOnlyDictionary_Success()
    {
        int number = _faker.Random.Int(1, 20);

        Dictionary<string, string> dictionary = new();

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        
        while (dictionary.Count < number)
        {
            if (stopwatch.ElapsedMilliseconds / 1000 > 10)
                throw new Exception("Took to long to generate test data");
            
            string key = _faker.Database.Column();
            string value = _faker.Random.Word();
            
            dictionary.TryAdd(key, value);
        }
        
        stopwatch.Stop();
        
        ReadOnlyDictionary<string, string> readOnlyDictionary = new ReadOnlyDictionary<string, string>(dictionary);
        
        // Act
        IEnvironmentVariablesBuilder builder = new EnvironmentVariablesBuilder()
            .SetReadOnlyDictionary(readOnlyDictionary);
       
        IReadOnlyDictionary<string, string> variables = builder.Build();
       
        // Assert
        foreach (KeyValuePair<string, string> pair in readOnlyDictionary)
        {
            Assert.Equal(pair.Value, variables[pair.Key]);
        }
    }

    [Fact]
    public void Combined_Build_Success()
    {
        // Arrange
        int number = _faker.Random.Int(1, 10);

        Dictionary<string, string> dictionary = new();

        while (dictionary.Count < number)
        {
            string key = _faker.Database.Column();
            string value = _faker.Random.Word();
            
            dictionary.TryAdd(key, value);
        }
        
        // Arrange
        Dictionary<string, string> dictionary2 = new();

        while (dictionary.Count < number)
        {
            string key = _faker.Commerce.Ean13();
            string value = _faker.Random.Word();
            
            dictionary2.TryAdd(key, value);
        }
        
        ReadOnlyDictionary<string, string> readOnlyDictionary = new(dictionary2);

        List<KeyValuePair<string, string>> list = new();

        while (list.Count < number)
        {
            string key = _faker.Internet.Ipv6();
            string value = _faker.Internet.Color();
            
            list.Add(new KeyValuePair<string, string>(key, value));
        }
        
        list = list.DistinctBy(x => x.Key).ToList();

        string pairKey = _faker.Address.ZipCode();
        string pairValue = _faker.Address.City();

        //Act
        IEnvironmentVariablesBuilder builder = new EnvironmentVariablesBuilder()
            .SetPair(pairKey, pairValue)
            .SetEnumerable(list)
            .SetDictionary(dictionary)
            .SetReadOnlyDictionary(readOnlyDictionary);
        
        IReadOnlyDictionary<string, string> variables = builder.Build();
        
        //Assert 
        Assert.Equal(pairValue, variables[pairKey]);

        foreach (KeyValuePair<string, string> pair in dictionary)
        {
            Assert.Equal(pair.Value, variables[pair.Key]);
        }

        foreach (KeyValuePair<string, string> pair2 in dictionary2)
        {
            Assert.Equal(pair2.Value, variables[pair2.Key]);
        }

        foreach (KeyValuePair<string, string> pair3 in list)
        {
            Assert.Equal(pair3.Value, variables[pair3.Key]);
        }
    }
}