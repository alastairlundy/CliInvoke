/*
    CliInvoke.Specializations.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Threading.Tasks;
using CliInvoke.Core.Internal;

namespace CliInvoke.Specializations.Tests.Internal;

public class ShellArgumentEscaperTests
{
    [Test]
    public async Task EscapeForPowerShell_NeutralisesCommandSeparators()
    {
        // A semicolon must be escaped so it is not re-interpreted as a second
        // PowerShell statement.
        string escaped = ShellArgumentEscaper.EscapeForPowerShell("echo hi ; Get-Process");
        await Assert.That(escaped).IsEqualTo("echo hi `; Get-Process");
    }

    [Test]
    public async Task EscapeForPowerShell_NeutralisesSubexpressionExpansion()
    {
        // $(...) command substitution must be escaped so secrets/env cannot be read.
        string escaped = ShellArgumentEscaper.EscapeForPowerShell("$(Get-Content secret.txt)");
        await Assert.That(escaped).IsEqualTo("`$`(Get-Content secret.txt`)");
    }

    [Test]
    public async Task EscapeForPowerShell_NeutralisesPipesAndAmpersand()
    {
        string escaped = ShellArgumentEscaper.EscapeForPowerShell("a | b & c");
        await Assert.That(escaped).IsEqualTo("a `| b `& c");
    }

    [Test]
    public async Task EscapeForCmd_NeutralisesCommandSeparatorsAndRedirection()
    {
        string escaped = ShellArgumentEscaper.EscapeForCmd("dir & del c:\\temp ^ > nul");
        await Assert.That(escaped).IsEqualTo("dir ^& del c:\\temp ^^ ^> nul");
    }

    [Test]
    public async Task EscapeForCmd_NeutralisesVariableExpansion()
    {
        string escaped = ShellArgumentEscaper.EscapeForCmd("%PATH%");
        await Assert.That(escaped).IsEqualTo("^%PATH^%");
    }

    [Test]
    public async Task Escape_PreservesPlainArguments()
    {
        await Assert.That(ShellArgumentEscaper.EscapeForPowerShell("--version"))
            .IsEqualTo("--version");
        await Assert.That(ShellArgumentEscaper.EscapeForCmd("--version"))
            .IsEqualTo("--version");
    }
}
