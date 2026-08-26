/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Core.Internal;
using Xunit;

namespace CliInvoke.Tests.Internal;

public class ShellArgumentEscaperTests
{
    [Fact]
    public void EscapeForPowerShell_NeutralisesCommandSeparators()
    {
        // A semicolon must be escaped so it is not re-interpreted as a second
        // PowerShell statement.
        string escaped = ShellArgumentEscaper.EscapeForPowerShell("echo hi ; Get-Process");
        Assert.Equal("echo hi `; Get-Process", escaped);
    }

    [Fact]
    public void EscapeForPowerShell_NeutralisesSubexpressionExpansion()
    {
        // $(...) command substitution must be escaped so secrets/env cannot be read.
        string escaped = ShellArgumentEscaper.EscapeForPowerShell("$(Get-Content secret.txt)");
        Assert.Equal("`$`(Get-Content secret.txt`)", escaped);
    }

    [Fact]
    public void EscapeForPowerShell_NeutralisesPipesAndAmpersand()
    {
        string escaped = ShellArgumentEscaper.EscapeForPowerShell("a | b & c");
        Assert.Equal("a `| b `& c", escaped);
    }

    [Fact]
    public void EscapeForCmd_NeutralisesCommandSeparatorsAndRedirection()
    {
        string escaped = ShellArgumentEscaper.EscapeForCmd("dir & del c:\\temp ^ > nul");
        Assert.Equal("dir ^& del c:\\temp ^^ ^> nul", escaped);
    }

    [Fact]
    public void EscapeForCmd_NeutralisesVariableExpansion()
    {
        string escaped = ShellArgumentEscaper.EscapeForCmd("%PATH%");
        Assert.Equal("^%PATH^%", escaped);
    }

    [Fact]
    public void Escape_PreservesPlainArguments()
    {
        Assert.Equal("--version", ShellArgumentEscaper.EscapeForPowerShell("--version"));
        Assert.Equal("--version", ShellArgumentEscaper.EscapeForCmd("--version"));
    }

    [Fact]
    public void Escape_ReturnsEmptyForNullAndEmpty()
    {
        Assert.Equal(string.Empty, ShellArgumentEscaper.EscapeForCmd(null));
        Assert.Equal(string.Empty, ShellArgumentEscaper.EscapeForCmd(string.Empty));
        Assert.Equal(string.Empty, ShellArgumentEscaper.EscapeForPowerShell(null));
        Assert.Equal(string.Empty, ShellArgumentEscaper.EscapeForPowerShell(string.Empty));
    }
}
