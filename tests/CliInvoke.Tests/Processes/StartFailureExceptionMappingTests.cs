using System.ComponentModel;

using CliInvoke.Processes.Internal;

namespace CliInvoke.Tests.Processes;

public class StartFailureExceptionMappingTests
{
    [Test]
    public async Task ErrorCode2_FileNotFound()
    {
        Win32Exception exception = new Win32Exception(2);

        Exception result =
            ProcessWrapper.MapWin32ExceptionToStartFailureException(
                exception, "C:\\test\\app.exe");

        await Assert.That(result).IsTypeOf<FileNotFoundException>();
        await Assert.That(result.Message)
            .Contains("C:\\test\\app.exe");
    }

    [Test]
    public async Task ErrorCode3_PathNotFound()
    {
        Win32Exception exception = new Win32Exception(3);

        Exception result =
            ProcessWrapper.MapWin32ExceptionToStartFailureException(
                exception, "/usr/bin/tool");

        await Assert.That(result).IsTypeOf<FileNotFoundException>();
        await Assert.That(result.Message)
            .Contains("/usr/bin/tool");
    }

    [Test]
    public async Task ErrorCode5_UnauthorizedAccess()
    {
        Win32Exception exception = new Win32Exception(5);

        Exception result =
            ProcessWrapper.MapWin32ExceptionToStartFailureException(
                exception, "C:\\test\\app.exe");

        await Assert.That(result).IsTypeOf<UnauthorizedAccessException>();
        await Assert.That(result.Message)
            .Contains("C:\\test\\app.exe");
    }

    [Test]
    public async Task ErrorCode193_BadImageFormat()
    {
        Win32Exception exception = new Win32Exception(193);

        Exception result =
            ProcessWrapper.MapWin32ExceptionToStartFailureException(
                exception, "C:\\test\\app.exe");

        await Assert.That(result).IsTypeOf<BadImageFormatException>();
        await Assert.That(result.Message)
            .Contains("C:\\test\\app.exe");
    }

    [Test]
    public async Task ErrorCode193_MessageContainsFilePath()
    {
        string filePath = "/opt/bin/script";

        Win32Exception exception = new Win32Exception(193);

        Exception result =
            ProcessWrapper.MapWin32ExceptionToStartFailureException(
                exception, filePath);

        await Assert.That(result.Message)
            .Contains(filePath);
    }

    [Test]
    public async Task ErrorCode206_UnknownCode_Fallback()
    {
        Win32Exception exception = new Win32Exception(206);

        Exception result =
            ProcessWrapper.MapWin32ExceptionToStartFailureException(
                exception, "C:\\test\\app.exe");

        await Assert.That(result.Message).Contains("206");
        await Assert.That(result.Message)
            .Contains("C:\\test\\app.exe");
    }

    [Test]
    public async Task ErrorCode206_UnknownCode_FallbackIsGenericException()
    {
        Win32Exception exception = new Win32Exception(206);

        Exception result =
            ProcessWrapper.MapWin32ExceptionToStartFailureException(
                exception, "C:\\test\\app.exe");

        await Assert.That(result).IsTypeOf<Exception>();
    }
}
