/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Helpers.Processes;
using Xunit;

namespace CliInvoke.Tests.Helpers.Processes;

/// <summary>
///     Verifies that <see cref="ToStartInfoExtensions" /> applies the same argument
///     token rules as the composition layer when a cmd.exe runner or a cmd/batch
///     target is launched from pre-tokenised arguments, while leaving the raw
///     <see cref="ProcessConfiguration.Arguments" /> string path untouched.
/// </summary>
public class ToStartInfoExtensionsTests
{
    private static ProcessConfiguration BuildConfig(string targetFilePath, string arguments)
        => new(targetFilePath, arguments);

    [Theory]
    [InlineData("x&echo more", "&")]
    [InlineData("x<echo more", "<")]
    public void ToProcessStartInfo_CmdRunner_TokenWithSeparator_Throws(
        string arguments,
        string expectedCharacter)
    {
        ProcessConfiguration configuration = BuildConfig("cmd.exe", arguments);

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => configuration.ToProcessStartInfo(false, false));

        Assert.Equal("ArgumentsList", exception.ParamName);
        // The argument string is tokenised on whitespace, so the offending token is
        // the part before the space; the message must name that token and the
        // offending character.
        Assert.Contains(arguments.Split(' ')[0], exception.Message);
        Assert.Contains(expectedCharacter, exception.Message);
        Assert.Contains($"contains \"{expectedCharacter}\"", exception.Message);
    }

    [Fact]
    public void ToProcessStartInfo_CmdRunner_TokenWithExpansion_Throws()
    {
        ProcessConfiguration configuration = BuildConfig("cmd.exe", "value %PATH%");

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => configuration.ToProcessStartInfo(false, false));

        Assert.Equal("ArgumentsList", exception.ParamName);
        Assert.Contains("%PATH%", exception.Message);
        Assert.Contains("%", exception.Message);
        Assert.Contains("contains \"%\"", exception.Message);
    }

    [Fact]
    public void ToProcessStartInfo_NonCmdTarget_TokenWithSeparator_IsAccepted()
    {
        // A discrete token is delivered literally to a target cmd.exe never parses.
        ProcessConfiguration configuration = BuildConfig(@"C:\tools\app.exe", "value&name");

        ProcessStartInfo processStartInfo = configuration.ToProcessStartInfo(false, false);

        Assert.Contains("value&name", processStartInfo.ArgumentList);
    }

    [Theory]
    [InlineData(@"C:\scripts\task.bat")]
    [InlineData(@"C:\scripts\task.cmd")]
    public void ToProcessStartInfo_BatchTarget_TokenWithSeparator_Throws(string targetFilePath)
    {
        // A batch file re-parses its arguments with cmd.exe's rules even when it is
        // launched directly, so its tokens are held to the same rules.
        ProcessConfiguration configuration = BuildConfig(targetFilePath, "value&name");

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => configuration.ToProcessStartInfo(false, false));

        Assert.Equal("ArgumentsList", exception.ParamName);
        Assert.Contains("value&name", exception.Message);
        Assert.Contains("&", exception.Message);
        Assert.Contains("contains \"&\"", exception.Message);
    }

    [Fact]
    public void ToProcessStartInfo_CmdRunner_BenignTokens_AreAccepted()
    {
        ProcessConfiguration configuration = BuildConfig("cmd.exe", "/c echo hi");

        ProcessStartInfo processStartInfo = configuration.ToProcessStartInfo(false, false);

        Assert.Equal(string.Empty, processStartInfo.Arguments);

        string[] expected = ["/c", "echo", "hi"];
        Assert.Equal(expected, processStartInfo.ArgumentList);
    }

    [Fact]
    public void ToProcessStartInfo_ShellExecution_TokenWithSeparator_Throws()
    {
        // Shell delivery changes how values reach the target, not the token rules
        // that apply to a cmd.exe target: the tokens are checked either way.
        ProcessConfiguration configuration =
            new("cmd.exe", "x&echo more", useShellExecution: true);

        Assert.True(configuration.UseShellExecution);

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => configuration.ToProcessStartInfo(false, false));

        Assert.Equal("ArgumentsList", exception.ParamName);
        Assert.Contains("x&echo", exception.Message);
        Assert.Contains("&", exception.Message);
        Assert.Contains("contains \"&\"", exception.Message);
    }

    [Fact]
    public void ToProcessStartInfo_ShellExecution_BenignTokens_KeepRawArgumentString()
    {
        // With shell execution the target receives the raw Arguments string, so the
        // ArgumentList delivery path must stay out of the picture.
        ProcessConfiguration configuration =
            new("cmd.exe", "/c echo hi", useShellExecution: true);

        ProcessStartInfo processStartInfo = configuration.ToProcessStartInfo(false, false);

        Assert.True(processStartInfo.UseShellExecute);
        Assert.Equal("/c echo hi", processStartInfo.Arguments);
        Assert.Empty(processStartInfo.ArgumentList);
    }

    [Fact]
    public void ToProcessStartInfo_RawArgumentsStringPath_IsNotValidated()
    {
        // The raw Arguments string is a by-contract preformatted command line (also
        // used for CmdProcessConfiguration raw /c lines): only the tokenised path is
        // subject to the token rules, so clearing ArgumentsList leaves it untouched.
        ProcessConfiguration configuration = BuildConfig("cmd.exe", "x&y");
        configuration.ArgumentsList = [];

        ProcessStartInfo processStartInfo = configuration.ToProcessStartInfo(false, false);

        Assert.Equal("x&y", processStartInfo.Arguments);
        Assert.Empty(processStartInfo.ArgumentList);
    }

    [Fact]
    public void ToProcessStartInfo_TokenizedPath_Throws_RawArgumentsStringPath_DoesNot()
    {
        // Paired check of the by-contract split with identical benign command text:
        // the tokenised ArgumentsList path is validated token by token and refused,
        // while the raw preformatted Arguments string path is not validated at all
        // and converts with the string kept verbatim.
        ProcessConfiguration tokenized = BuildConfig("cmd.exe", "x&echo more");

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => tokenized.ToProcessStartInfo(false, false));

        Assert.Equal("ArgumentsList", exception.ParamName);
        Assert.Contains("x&echo", exception.Message);
        Assert.Contains("contains \"&\"", exception.Message);

        ProcessConfiguration raw = BuildConfig("cmd.exe", "x&echo more");
        raw.ArgumentsList = [];

        ProcessStartInfo processStartInfo = raw.ToProcessStartInfo(false, false);

        Assert.Equal("x&echo more", processStartInfo.Arguments);
        Assert.Empty(processStartInfo.ArgumentList);
    }
}
