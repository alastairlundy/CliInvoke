/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Core.Internal;

namespace CliInvoke.Tests.Internal;

public class ArgumentEscaperTests
{
    [Test]
    public async Task EscapeInner_Null_ReturnsEmpty()
    {
        await Assert.That(ArgumentEscaper.EscapeInner(null)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task EscapeInner_Empty_ReturnsEmpty()
    {
        await Assert.That(ArgumentEscaper.EscapeInner(string.Empty)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task EscapeInner_PlainText_Unchanged()
    {
        // Arrange
        const string value = "hello_world-123";

        // Act
        string escaped = ArgumentEscaper.EscapeInner(value);

        // Assert
        await Assert.That(escaped).IsEqualTo(value);
    }

    [Test]
    public async Task EscapeInner_EmbeddedQuote_IsDoubled()
    {
        // Arrange
        const string value = "say\"hi";

        // Act
        string escaped = ArgumentEscaper.EscapeInner(value);

        // Assert - both Windows and POSIX branches write "" for an embedded quote
        await Assert.That(escaped).Contains("\"\"");
        await Assert.That(escaped).DoesNotContain("say\"hi");
    }

    [Test]
    public async Task EscapeInner_BackslashesBeforeQuote_AreDoubled()
    {
        // Arrange
        const string value = "a\\\"b";

        // Act
        string escaped = ArgumentEscaper.EscapeInner(value);

        // Assert - the "\" before the quote becomes "\\" and the quote becomes ""
        await Assert.That(escaped).Contains("\\\\\"" + "\"");
    }

    [Test]
    public async Task EscapeInner_TrailingBackslash_IsDoubled()
    {
        // Arrange
        const string value = "path\\";

        // Act
        string escaped = ArgumentEscaper.EscapeInner(value);

        // Assert - trailing backslashes are doubled for the closing quote
        await Assert.That(escaped).IsEqualTo("path\\\\");
    }

    [Test]
    public async Task EscapeInner_NewlinesAreDropped()
    {
        // Arrange
        const string value = "line1\nline2\r\nline3";

        // Act
        string escaped = ArgumentEscaper.EscapeInner(value);

        // Assert - bare newlines must not appear in the escaped output
        await Assert.That(escaped).DoesNotContain("\n");
        await Assert.That(escaped).DoesNotContain("\r");
        await Assert.That(escaped).Contains("line1");
        await Assert.That(escaped).Contains("line2");
        await Assert.That(escaped).Contains("line3");
    }

    [Test]
    public async Task NeedsQuoting_NullOrEmpty_ReturnsFalse()
    {
        await Assert.That(ArgumentEscaper.NeedsQuoting(null)).IsFalse();
        await Assert.That(ArgumentEscaper.NeedsQuoting(string.Empty)).IsFalse();
    }

    [Test]
    public async Task NeedsQuoting_PlainAlphanumeric_ReturnsFalse()
    {
        await Assert.That(ArgumentEscaper.NeedsQuoting("abcdef123")).IsFalse();
    }

    [Test]
    public async Task NeedsQuoting_Whitespace_ReturnsTrue()
    {
        await Assert.That(ArgumentEscaper.NeedsQuoting("a b")).IsTrue();
        await Assert.That(ArgumentEscaper.NeedsQuoting("a\tb")).IsTrue();
    }

    [Test]
    public async Task NeedsQuoting_QuoteOrBackslash_ReturnsTrue()
    {
        await Assert.That(ArgumentEscaper.NeedsQuoting("a\"b")).IsTrue();
        await Assert.That(ArgumentEscaper.NeedsQuoting("a\\b")).IsTrue();
    }

    [Test]
    public async Task NeedsQuoting_ShellMetacharacters_ReturnsTrue()
    {
        foreach (char c in new[] { '&', '|', '<', '>', '^', '%' })
        {
            await Assert.That(ArgumentEscaper.NeedsQuoting($"a{c}b")).IsTrue();
        }
    }
}
