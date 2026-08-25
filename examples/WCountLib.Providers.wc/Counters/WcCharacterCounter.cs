/*
    WCountLib.Providers.wc
    Copyright (C) 2026 Alastair Lundy

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
/// A character counting implementation that utilises the Unix <c>wc</c> command for processing.
/// </summary>
public class WcCharacterCounter : ICharacterCounter
{
	private readonly WcCommandExecutionHelper _wcCommandExecutionHelper;

	/// <summary>
	/// Provides functionality to count the number of characters in a text string
	/// by utilising the underlying `wc` command through command-line invocation.
	/// </summary>
	/// <param name="processInvoker">The CliInvoke process invoker used to run <c>wc</c>.</param>
	/// <param name="filePathResolver">The resolver used to locate the <c>wc</c> executable.</param>
	public WcCharacterCounter(IProcessInvoker processInvoker, IFilePathResolver filePathResolver)
	{
		_wcCommandExecutionHelper = new WcCommandExecutionHelper(processInvoker, filePathResolver);
	}

	/// <summary>
	/// Counts the number of characters in the given text.
	/// </summary>
	/// <param name="text">The input text for which the character count is to be computed.</param>
	/// <param name="textEncodingType">The encoding type of the input text.</param>
	/// <returns>The number of characters in the provided text.</returns>
	[UnsupportedOSPlatform("windows")]
	[SupportedOSPlatform("macos")]
	[SupportedOSPlatform("linux")]
	[SupportedOSPlatform("freebsd")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public int CountCharacters(string text, Encoding textEncodingType)
	{
		return _wcCommandExecutionHelper.RunInt32("-m", text);
	}

	/// <summary>
	/// Asynchronously counts the number of characters in the given text.
	/// </summary>
	/// <param name="text">The input text for which the character count is to be computed.</param>
	/// <returns>A task representing the asynchronous operation. The task result contains the number of characters in the provided text.</returns>
	[UnsupportedOSPlatform("windows")]
	[SupportedOSPlatform("macos")]
	[SupportedOSPlatform("linux")]
	[SupportedOSPlatform("freebsd")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public async Task<int> CountCharactersAsync(string text)
	{
		return await _wcCommandExecutionHelper.RunInt32Async("-m", text);
	}
}