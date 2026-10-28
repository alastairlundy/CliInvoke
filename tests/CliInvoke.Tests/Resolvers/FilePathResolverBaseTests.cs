namespace CliInvoke.Tests.Resolvers;

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
        await Assert.That(resolver.LastReceivedExtensions[0]).IsEqualTo(".exe");
        await Assert.That(resolver.LastReceivedExtensions[1]).IsEqualTo(".cmd");
    }

    [Test]
    public async Task ResolveFilePath_ReturnsFileInfoDirectly_WhenPathIsRooted()
    {
        // Use an existing rooted file: rooted paths are validated for existence.
        string rootedPath = typeof(FilePathResolverBaseTests).Assembly.Location;

        TestableFilePathResolver resolver = new();

        FileInfo result = resolver.ResolveFilePath(rootedPath);

        await Assert.That(result.FullName).IsEqualTo(new FileInfo(rootedPath).FullName);
        await Assert.That(resolver.InvocationOrder).IsEmpty();
    }

    [Test]
    public async Task ResolveFilePath_RootedPathDoesNotExist_ThrowsFileNotFoundException()
    {
        string missingPath = Path.Combine(
            Path.GetTempPath(),
            $"cliinvoke-missing-{Guid.NewGuid():N}.exe");

        TestableFilePathResolver resolver = new();

        await Assert.That(() => resolver.ResolveFilePath(missingPath))
            .Throws<FileNotFoundException>();

        // The rooted path is rejected before any strategy runs.
        await Assert.That(resolver.InvocationOrder).IsEmpty();
    }

    [Test]
    public async Task TryResolveFilePath_RootedPathDoesNotExist_ReturnsFalseWithoutThrowing()
    {
        string missingPath = Path.Combine(
            Path.GetTempPath(),
            $"cliinvoke-missing-{Guid.NewGuid():N}.exe");

        TestableFilePathResolver resolver = new();

        bool result = resolver.TryResolveFilePath(missingPath, out FileInfo? resolved);

        await Assert.That(result).IsFalse();
        await Assert.That(resolved).IsNull();
    }
}
