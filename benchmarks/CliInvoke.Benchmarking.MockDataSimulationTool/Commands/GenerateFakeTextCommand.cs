/*
    CliInvoke

    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Text;
using Bogus;

namespace CliInvoke.Benchmarking.MockDataSimTool.Commands;

public static class GenerateFakeTextCommand
{
    public const int MinLineLength = 1;
    public const int MaxLineLength = 1_000_000;
    public const int DefaultLineLength = 100_000;

    public const int MinLineCount = 1;
    public const int MaxLineCount = 1000;
    public const int DefaultLineCount = 100;

    public static int Execute(int lineLength, int lineCount)
    {
        Faker faker = new Faker();
        char[] fakeChars = faker.Random.Chars(count: 1000);

        StringBuilder stringBuilder = new StringBuilder();

        try
        {
            for (int line = 0; line < lineCount; line++)
            {
                for (int i = 0; i < lineLength; i++)
                    stringBuilder.Append(faker.PickRandom(fakeChars));

                Console.WriteLine(stringBuilder.ToString());
                stringBuilder.Clear();
            }

            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}