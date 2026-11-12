using System.Threading.Tasks;

namespace CliInvoke.Specializations.Tests;

/// <summary>
///     Marks a test as Windows-only so that it is reported as skipped on other
///     platforms instead of reporting a pass without ever running its assertions.
/// </summary>
public sealed class WindowsOnlyAttribute() : SkipAttribute("This test is only supported on Windows")
{
    /// <summary>
    ///     Decides at test-registration time whether the current platform can run
    ///     the test.
    /// </summary>
    /// <param name="context">The registered test context.</param>
    /// <returns>True when the test should be skipped; otherwise false.</returns>
    public override Task<bool> ShouldSkip(TestRegisteredContext context)
        => Task.FromResult(!OperatingSystem.IsWindows());
}
