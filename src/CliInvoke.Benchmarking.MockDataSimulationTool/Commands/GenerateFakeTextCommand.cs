﻿using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Bogus;

using Spectre.Console.Cli;

using ValidationResult = Spectre.Console.ValidationResult;

namespace CliInvoke.Benchmarking.MockDataSimulationTool.Commands;

public class GenerateFakeTextCommand : ICommand<GenerateFakeTextCommand.Settings>
{
    private Faker _faker;

    private char[] fakeChars;
    
    public class Settings : CommandSettings
    {
        [CommandOption("--line-length|-ll")]
        [DefaultValue(100_000)]
        [Range(1, 1_000_000)]
        public int FakeTextLineLength { get; init; }
        
        [CommandOption("--lines|-ln")]
        [DefaultValue(100)]
        [Range(1, 1000)]
        public int NumberOfFakeTextLines { get; init; }
    }

    public GenerateFakeTextCommand()
    {
        _faker = new Faker();
        
       fakeChars = _faker.Random.Chars(count: 1000);
    }
    
    public async Task<int> Execute(CommandContext context, Settings settings)
    {
        StringBuilder stringBuilder = new StringBuilder();

        for (int line = 0; line < settings.NumberOfFakeTextLines; line++)
        {
            
            for (int i = 0; i < settings.FakeTextLineLength; i++)
            {
                stringBuilder.Append(_faker.PickRandom(fakeChars));
            }
            
            stringBuilder.AppendLine();
        }

        try
        {
            await Console.Out.WriteLineAsync(stringBuilder.ToString());
            return await new ValueTask<int>(0);
        }
        catch
        {
            return await new ValueTask<int>(-1);
        }
    }

    public ValidationResult Validate(CommandContext context, CommandSettings settings)
    {
        if (settings is Settings s)
        {
            if (s.NumberOfFakeTextLines > 0 && s.NumberOfFakeTextLines < 1001 && s.FakeTextLineLength > 0 &&
                s.FakeTextLineLength < 1_000_001)
            {
                return ValidationResult.Success();
            }
            else
            {
                return ValidationResult.Error();
            }
        }
        
        return settings.Validate();
    }

    public async Task<int> Execute(CommandContext context, CommandSettings settings)
    {
        Settings settingsActual = new Settings()
        {
            FakeTextLineLength = 100_000,
            NumberOfFakeTextLines = 100,
        };
        
        return await Execute(context, settingsActual);
    }
}