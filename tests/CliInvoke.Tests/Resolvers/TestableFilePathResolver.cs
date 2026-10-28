namespace CliInvoke.Tests.Resolvers;

internal sealed class TestableFilePathResolver : FilePathResolver
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