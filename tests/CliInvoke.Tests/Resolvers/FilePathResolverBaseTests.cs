using System.Collections.Generic;
using System.Linq;

namespace CliInvoke.Tests.Resolvers;

internal sealed class TestableFilePathResolver : FilePathResolverBase
{
    public const string PathStrategyName = "ResolveFromPathEnvironmentVariable";
    public const string DirectoryStrategyName = "LocateFileFromDirectory";

    public List<string> InvocationOrder { get; } = [];

    public bool ResolveFromPathReturnValue { get; set; }
    public FileInfo? ResolveFromPathResult { get; set; }

    public FileInfo? LocateFileFromDirectoryResult { get; set; }
    public bool LocateFileFromDirectoryThrows { get; set; }

    public string[]? PathExtensionsToReturn { get; set; }

    public string[]? LastReceivedExtensions { get; private set; }

    protected override bool ResolveFromPathEnvironmentVariable(string filePathToResolve, out FileInfo? resolvedFilePath)
    {
        InvocationOrder.Add(PathStrategyName);
        resolvedFilePath = ResolveFromPathResult;
        return ResolveFromPathReturnValue;
    }

    protected override FileInfo LocateFileFromDirectory(string filePathToResolve)
    {
        InvocationOrder.Add(DirectoryStrategyName);

        if (LocateFileFromDirectoryThrows)
        {
            throw new FileNotFoundException("Test exception");
        }

        return LocateFileFromDirectoryResult!;
    }

    protected override string[] GetPathFileExtensions()
    {
        if (PathExtensionsToReturn is not null)
        {
            string[] copy = (string[])PathExtensionsToReturn.Clone();

            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = copy[i].ToLowerInvariant();
            }

            return copy;
        }

        return base.GetPathFileExtensions();
    }

    public string[] InvokeGetPathFileExtensions()
    {
        return GetPathFileExtensions();
    }

    public void RecordReceivedExtensions(string[] extensions)
    {
        LastReceivedExtensions = extensions;
    }
}

public class FilePathResolverBaseTests
{
    [Test]
    public async Task ResolveFilePath_ReturnsPathStrategyResult_WhenPathStrategySucceeds()
    {
        FileInfo expected = new("/fake/path/dotnet.exe");

        TestableFilePathResolver resolver = new()
        {
            ResolveFromPathReturnValue = true,
            ResolveFromPathResult = expected,
        };

        FileInfo actual = resolver.ResolveFilePath("dotnet.exe");

        await Assert.That(actual).IsEqualTo(expected);
        await Assert.That(resolver.InvocationOrder).Count().IsEqualTo(1);
        await Assert.That(resolver.InvocationOrder[0]).IsEqualTo(TestableFilePathResolver.PathStrategyName);
    }

    [Test]
    public async Task ResolveFilePath_FallsThroughToDirectoryStrategy_WhenPathStrategyFails()
    {
        FileInfo expected = new("/fake/dir/dotnet.exe");

        TestableFilePathResolver resolver = new()
        {
            ResolveFromPathReturnValue = false,
            ResolveFromPathResult = null,
            LocateFileFromDirectoryResult = expected,
        };

        FileInfo actual = resolver.ResolveFilePath("dotnet.exe");

        await Assert.That(actual).IsEqualTo(expected);
        await Assert.That(resolver.InvocationOrder).Count().IsEqualTo(2);
        await Assert.That(resolver.InvocationOrder[0]).IsEqualTo(TestableFilePathResolver.PathStrategyName);
        await Assert.That(resolver.InvocationOrder[1]).IsEqualTo(TestableFilePathResolver.DirectoryStrategyName);
    }

    [Test]
    public async Task TryResolveFilePath_ReturnsTrue_AndResolvedFileInfo_OnSuccess()
    {
        FileInfo expected = new("/fake/path/dotnet.exe");

        TestableFilePathResolver resolver = new()
        {
            ResolveFromPathReturnValue = true,
            ResolveFromPathResult = expected,
        };

        bool result = resolver.TryResolveFilePath("dotnet.exe", out FileInfo? resolved);

        await Assert.That(result).IsTrue();
        await Assert.That(resolved).IsEqualTo(expected);
    }

    [Test]
    public async Task TryResolveFilePath_ReturnsFalse_AndNull_WhenStrategyThrows()
    {
        TestableFilePathResolver resolver = new()
        {
            ResolveFromPathReturnValue = false,
            ResolveFromPathResult = null,
            LocateFileFromDirectoryThrows = true,
        };

        bool result = resolver.TryResolveFilePath("dotnet.exe", out FileInfo? resolved);

        await Assert.That(result).IsFalse();
        await Assert.That(resolved).IsNull();
    }

    [Test]
    public async Task Strategies_AreCalledInDocumentedOrder_PathFirst_ThenDirectory()
    {
        FileInfo expected = new("/fake/dir/dotnet.exe");

        TestableFilePathResolver resolver = new()
        {
            ResolveFromPathReturnValue = false,
            ResolveFromPathResult = null,
            LocateFileFromDirectoryResult = expected,
        };

        resolver.ResolveFilePath("dotnet.exe");

        await Assert.That(resolver.InvocationOrder).Count().IsEqualTo(2);
        await Assert.That(resolver.InvocationOrder[0]).IsEqualTo(TestableFilePathResolver.PathStrategyName);
        await Assert.That(resolver.InvocationOrder[1]).IsEqualTo(TestableFilePathResolver.DirectoryStrategyName);
    }

    [Test]
    public async Task GetPathFileExtensions_ReturnsLowercasedExtensions()
    {
        TestableFilePathResolver resolver = new()
        {
            PathExtensionsToReturn = [".EXE", ".Cmd", ".BAT"],
        };

        string[] extensions = resolver.InvokeGetPathFileExtensions();

        await Assert.That(extensions).Count().IsEqualTo(3);
        await Assert.That(extensions[0]).IsEqualTo(".exe");
        await Assert.That(extensions[1]).IsEqualTo(".cmd");
        await Assert.That(extensions[2]).IsEqualTo(".bat");
    }

    [Test]
    public async Task GetPathFileExtensions_DoesNotCallToLowerPerIteration_InInnerLoop()
    {
        TestableFilePathResolver resolver = new()
        {
            PathExtensionsToReturn = [".EXE", ".CMD"],
        };

        string[] extensions = resolver.InvokeGetPathFileExtensions();

        resolver.RecordReceivedExtensions(extensions);

        await Assert.That(resolver.LastReceivedExtensions).IsNotNull();
        await Assert.That(resolver.LastReceivedExtensions![0]).IsEqualTo(".exe");
        await Assert.That(resolver.LastReceivedExtensions[1]).IsEqualTo(".cmd");
    }

    [Test]
    public async Task ResolveFilePath_ReturnsFileInfoDirectly_WhenPathIsRooted()
    {
        string rootedPath = Path.Combine(Path.GetTempPath(), "somefile.exe");

        TestableFilePathResolver resolver = new();

        FileInfo result = resolver.ResolveFilePath(rootedPath);

        await Assert.That(result.FullName).IsEqualTo(new FileInfo(rootedPath).FullName);
        await Assert.That(resolver.InvocationOrder).IsEmpty();
    }
}
