/*
        MIT License

       Copyright (c) 2026 Alastair Lundy

       Permission is hereby granted, free of charge, to any person obtaining a copy
       of this software and associated documentation files (the "Software"), to deal
       in the Software without restriction, including without limitation the rights
       to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
       copies of the Software, and to permit persons to whom the Software is
       furnished to do so, subject to the following conditions:

       The above copyright notice and this permission notice shall be included in all
       copies or substantial portions of the Software.

       THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
       IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
       FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
       AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
       LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
       OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
       SOFTWARE.
   */
// Ported from DotExtensions.Tests/VersionParseTests.cs (same author, 2026).

using CliInvoke.Internal.Versions;

namespace CliInvoke.Tests.Internal.Versions;

public class VersionParseExtensionsTests
{
    [Test]
    [Arguments("1.2.3.4", 1, 2, 3, 4)]
    [Arguments("1.2.3", 1, 2, 3)]
    [Arguments("1.2", 1, 2)]
    [Arguments("10.20.30.40", 10, 20, 30, 40)]
    [Arguments("1000.2000", 1000, 2000)]
    public async Task GracefulParse_StandardVersions_ReturnsCorrectVersion(string input, int major, int minor,
        int build = -1, int revision = -1)
    {
        Version expected = build == -1 ? new Version(major, minor) :
            revision == -1 ? new Version(major, minor, build) :
            new Version(major, minor, build, revision);

        Version actual = Version.GracefulParse(input);

        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    [Arguments("v1.2.3", 1, 2, 3)]
    [Arguments("ver1.2.3.4", 1, 2, 3, 4)]
    [Arguments("version 1.2.3", 1, 2, 3)]
    [Arguments("version 10.20.30.40", 10, 20, 30, 40)]
    public async Task GracefulParse_WithPrefixes_ReturnsCorrectVersion(string input, int major, int minor,
        int build, int revision = -1)
    {
        Version expected = revision == -1 ? new Version(major, minor, build) : new Version(major, minor, build, revision);

        Version actual = Version.GracefulParse(input);

        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    [Arguments("1 . 2 . 3", 1, 2, 3)]
    [Arguments(" 1.2.3 ", 1, 2, 3)]
    [Arguments("v 1 . 2 . 3", 1, 2, 3)]
    public async Task GracefulParse_WithSpaces_ReturnsCorrectVersion(string input, int major, int minor,
        int build)
    {
        Version expected = new Version(major, minor, build);

        Version actual = Version.GracefulParse(input);

        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    [Arguments("1.2.3-beta.1", 1, 2, 3, 1)]
    [Arguments("1.2.3.4-alpha", 1, 2, 3, 4)]
    [Arguments("1.2.3.4.5", 1, 2, 3, 4)]
    [Arguments("10.20.300-beta.3", 10, 20, 300, 3)]
    public async Task GracefulParse_WithSuffixesOrExtraComponents_ReturnsCorrectVersion(string input, int major, int minor,
        int build, int revision)
    {
        Version expected = new Version(major, minor, build, revision);

        Version actual = Version.GracefulParse(input);

        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    public async Task GracefulParse_SingleComponent_ReturnsVersionWithZeroMinor()
    {
        Version actual = Version.GracefulParse("1");

        await Assert.That(actual)
            .IsEqualTo(new Version(1, 0));
    }

    [Test]
    public async Task GracefulParse_Null_ThrowsArgumentNullException()
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        await Assert.ThrowsAsync<ArgumentNullException>(() => Task.FromResult(Version.GracefulParse(null)));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }

    [Test]
    public async Task GracefulParse_Empty_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Task.FromResult(Version.GracefulParse("")));
    }

    [Test]
    [Arguments("v1", 1, 0)]
    [Arguments("1.0.0.0.0", 1, 0, 0, 0)]
    public async Task GracefulParse_EdgeCases_ReturnsCorrectVersion(string input, int major, int minor, int build = -1, int revision = -1)
    {
        Version expected;

        if (build != -1 && revision != -1)
        {
            expected = new Version(major, minor, build, revision);
        }
        else
        {
            expected = new Version(major, minor);
        }

        Version actual = Version.GracefulParse(input);

        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    public async Task GracefulParse_NoDigits_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Task.FromResult(Version.GracefulParse("abc.def")));
    }
}
