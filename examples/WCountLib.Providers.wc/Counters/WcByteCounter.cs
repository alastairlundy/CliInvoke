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

namespace WCountLib.Providers.wc.Counters;

/// <summary>
/// A class that implements the <see cref="IByteCounter"/> interface and provides functionality
/// to count the number of bytes in a text string by using the `wc` command-line tool.
/// </summary>
public class WcByteCounter : IByteCounter
{
	private readonly WcCommandExecutionHelper _wcCommandExecutionHelper;

	/// <summary>
	/// Provides functionality to count the number of bytes in a text string
	/// by utilising the underlying `wc` command through command-line invocation.
	/// </summary>
	/// <param name="processInvoker">The CliInvoke process invoker used to run <c>wc</c>.</param>
	/// <param name="filePathResolver">The resolver used to locate the <c>wc</c> executable.</param>
	public WcByteCounter(IProcessInvoker processInvoker, IFilePathResolver filePathResolver)
	{
		_wcCommandExecutionHelper = new WcCommandExecutionHelper(processInvoker, filePathResolver);
	}

	/// <summary>
	/// Counts the number of bytes in the provided text using the specified encoding.
	/// </summary>
	/// <param name="text">The input string whose byte count is to be calculated.</param>
	/// <param name="encoding">The encoding to be used to determine byte representation.</param>
	/// <returns>The number of bytes in the input text based on the specified encoding.</returns>
	[UnsupportedOSPlatform("windows")]
	[SupportedOSPlatform("macos")]
	[SupportedOSPlatform("linux")]
	[SupportedOSPlatform("freebsd")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public int CountBytes(string text, Encoding encoding)
	{
		return _wcCommandExecutionHelper.RunInt32("-c", text);
	}

	/// <summary>
	/// Asynchronously counts the number of bytes in the provided text using the specified encoding.
	/// </summary>
	/// <param name="text">The input string whose byte count is to be calculated.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the number of bytes in the input text based on the specified encoding.</returns>
	[UnsupportedOSPlatform("windows")]
	[SupportedOSPlatform("macos")]
	[SupportedOSPlatform("linux")]
	[SupportedOSPlatform("freebsd")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public async Task<int> CountBytesAsync(string text)
	{
		return await _wcCommandExecutionHelper.RunInt32Async("-c", text);
	}
}