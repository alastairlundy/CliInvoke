using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading;
using Bogus;
using Spectre.Console;
using Spectre.Console.Cli;
using ValidationResult = Spectre.Console.ValidationResult;

namespace CliInvoke.Benchmarking.MockDataSimTool.Commands;

public class GenerateFakeTextCommand : Command<GenerateFakeTextCommand.Settings>
{
    private readonly Faker _faker;

    private readonly char[] _fakeChars;

    public class Settings : CommandSettings
    {
        [CommandOption("--line-length|-l")]
        [DefaultValue(100_000)]
        [Range(1, 1_000_000)]
        public int FakeTextLineLength { get; init; }

        [CommandOption("--lines|-n")]
        [DefaultValue(100)]
        [Range(1, 1000)]
        public int NumberOfFakeTextLines { get; init; }
    }

    public GenerateFakeTextCommand()
    {
        _faker = new Faker();

        _fakeChars = _faker.Random.Chars(count: 1000);
    }

    protected override int Execute(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        StringBuilder stringBuilder = new StringBuilder();

        try
        {
            for (int line = 0; line < settings.NumberOfFakeTextLines; line++)
            {
                for (int i = 0; i < settings.FakeTextLineLength; i++)
                {
                    stringBuilder.Append(_faker.PickRandom(_fakeChars));
                }

                Console.WriteLine(stringBuilder.ToString());
                stringBuilder.Clear();
            }
            return 0;
        }
        catch(Exception exception)
        {
            AnsiConsole.WriteException(exception);
            return -1;
        }
    }

    public ValidationResult Validate(CommandContext context, CommandSettings settings)
    {
        if (settings is Settings s)
        {
            if (
                s.NumberOfFakeTextLines > 0
                && s is { NumberOfFakeTextLines: < 1001, FakeTextLineLength: > 0 and < 1_000_001 }
            )
            {
                return ValidationResult.Success();
            }

            return ValidationResult.Error();
        }

        return settings.Validate();
    }
}
