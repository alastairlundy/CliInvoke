using SharpFuzz;
using CliInvoke.Builders;
using CliInvoke.Core.Builders;

Fuzzer.LibFuzzer.Run(stream =>
{
    string input = stream.ToString();

    if (string.IsNullOrEmpty(input))
        return;

    try
    {
        IArgumentsBuilder builder = new ArgumentsBuilder();
                
        // Fuzzing basic Add and EscapeCharacters
        builder = builder.Add(input, true);
       
        // Fuzzing Enumerable Add
        builder = builder.AddEnumerable([input, input], true);
        
        // Fuzzing Clear
        builder.Clear();

        // Fuzzing ProcessConfigurationBuilder
        if (!string.IsNullOrWhiteSpace(input))
        {
            IProcessConfigurationBuilder processConfigurationBuilder = new ProcessConfigurationBuilder(input);
            processConfigurationBuilder = processConfigurationBuilder.SetArguments(input)
                .SetWorkingDirectory(input);
            processConfigurationBuilder.Build();
        }
    }
    catch (ArgumentException) { }
    catch (InvalidOperationException) { }
    catch (NullReferenceException) { }
});