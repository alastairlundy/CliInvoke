/*
    CliInvoke.Tests
    Copyright (C) 2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Processes.Internal;

namespace CliInvoke.Tests.Processes;

/// <summary>
///     Verifies the launch-conversion rules for configurations whose target is cmd.exe
///     or a batch file. Tokenized argument lists are checked before a child is started;
///     the raw preformatted Arguments string is left to the caller.
/// </summary>
public class ProcessStartInfoArgumentValidationTests
{
    private static ProcessConfiguration Tokenized(string character)
        => new("cmd.exe", new[] { "/c", "echo", "A", character, "echo", "B" });

    [Test]
    [Arguments("&")]
    [Arguments("|")]
    [Arguments("<")]
    [Arguments(">")]
    [Arguments("^")]
    [Arguments("%")]
    [Arguments("\"")]
    [Arguments("!")]
    public async Task TokenizedArgumentList_WithControlCharacter_IsRefusedBeforeLaunch(string character)
    {
        ProcessConfiguration configuration = Tokenized(character);

        ArgumentException? exception = await Assert
            .That(() => new ProcessWrapper(configuration, new FileInfo(configuration.TargetFilePath)))
            .Throws<ArgumentException>();

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception!.ParamName).IsEqualTo("ArgumentList");
        await Assert.That(exception!.Message).Contains($"contains \"{character}\"");
    }

    [Test]
    public async Task RawArgumentsString_IsLeftToTheCaller()
    {
        ProcessConfiguration configuration = new("cmd.exe", "/c echo A");

        using ProcessWrapper process =
            new(configuration, new FileInfo(configuration.TargetFilePath));

        await Assert.That(process.StartInfo.ArgumentList.Count).IsEqualTo(0);
        await Assert.That(process.StartInfo.Arguments).IsEqualTo("/c echo A");
    }

    [Test]
    public async Task NonCmdTarget_TokenizedArgumentList_IsNotRefused()
    {
        ProcessConfiguration configuration = new("app.exe", new[] { "value&name" });

        using ProcessWrapper process =
            new(configuration, new FileInfo(configuration.TargetFilePath));

        await Assert.That(process.StartInfo.ArgumentList).Contains("value&name");
    }
}
