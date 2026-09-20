/*
    CliInvoke.Specializations.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Threading.Tasks;
using CliInvoke.Specializations.Configurations;
using TUnit.Core;

namespace CliInvoke.Specializations.Tests.Configurations;

/// <summary>
///     Tests that both configuration subclasses emit the deprecation diagnostic.
/// </summary>
public class ConfigurationSubclassDeprecationTests
{
    [Test]
    public async Task PowershellProcessConfiguration_IsMarkedObsolete()
    {
        ObsoleteAttribute? obsolete = Attribute.GetCustomAttribute(
            typeof(PowershellProcessConfiguration),
            typeof(ObsoleteAttribute)) as ObsoleteAttribute;

        await Assert.That(obsolete).IsNotNull();
        await Assert.That(obsolete!.Message).Contains("Use ProcessConfiguration with shell wrapping middleware");
        await Assert.That(obsolete.Message).Contains("PowershellProcessConfiguration will be removed in 4.0");
    }

    [Test]
    public async Task CmdProcessConfiguration_IsMarkedObsolete()
    {
        ObsoleteAttribute? obsolete = Attribute.GetCustomAttribute(
            typeof(CmdProcessConfiguration),
            typeof(ObsoleteAttribute)) as ObsoleteAttribute;

        await Assert.That(obsolete).IsNotNull();
        await Assert.That(obsolete!.Message).Contains("Use ProcessConfiguration with shell wrapping middleware");
        await Assert.That(obsolete.Message).Contains("CmdProcessConfiguration will be removed in 4.0");
    }
}