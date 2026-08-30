/*
        NOTE:  CliInvoke Example apps are licensed under the MIT license. CliInvoke is licensed under the MPL 2.0 license.

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

using System.Linq;
using System.Threading;
using CliInvoke.Builders;

namespace WCountLib.Providers.wc.Helpers;

internal class WcCommandExecutionHelper
{
    private readonly IProcessInvoker _processInvoker;
    private readonly IFilePathResolver _filePathResolver;

    internal WcCommandExecutionHelper(IProcessInvoker processInvoker, IFilePathResolver filePathResolver)
    {
        _processInvoker = processInvoker;
        _filePathResolver = filePathResolver;
    }

    private static async Task<string> CreateTempFilePathAsync(string text)
    {
        string tempFilePath = Path.GetTempFileName();
        tempFilePath = Path.ChangeExtension(tempFilePath, ".txt");

        await File.WriteAllTextAsync(tempFilePath, text);

        return tempFilePath;
    }

    [UnsupportedOSPlatform("windows")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    private async Task<BufferedProcessResult> ExecuteAsync(string argument, string tempFileName)
    {
        string wcPath = _filePathResolver.ResolveFilePath("wc").FullName;

        ProcessConfiguration processConfiguration = new ProcessConfigurationBuilder(wcPath)
            .ConfigureArguments(args => args
                .Add(argument, escape: false)
                .Add(tempFileName, escape: true))
            .Build();

        try
        {
            return await _processInvoker.ExecuteBufferedAsync(
                processConfiguration,
                ProcessExitConfiguration.CreateGraceful(),
                CancellationToken.None);
        }
        finally
        {
            File.Delete(tempFileName);
        }
    }

    [UnsupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    internal int RunInt32(string argument, string text)
    {
        if (OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException();
        }

        return RunInt32Async(argument, text).GetAwaiter().GetResult();
    }

    [UnsupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    internal async Task<int> RunInt32Async(string argument, string text)
    {
        if (OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException();
        }

        string tempFile = await CreateTempFilePathAsync(text);

        BufferedProcessResult result = await ExecuteAsync(argument, tempFile);

        string resultString = result.StandardOutput
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .First();

        return int.Parse(resultString);
    }
}
