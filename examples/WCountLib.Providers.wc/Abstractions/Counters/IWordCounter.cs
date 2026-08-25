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

namespace WCountLib.Providers.wc.Abstractions.Counters;

/// <summary>
/// An interface for a service that counts the number of words in strings.
/// </summary>
/// <remarks><para>Implementing classes should be stateless and avoid containing any properties or fields that aren't related to configurations or settings for word counting.</para>
/// <para>Implementers are responsible for determining how to handle punctuation, special characters, how to detect what a word is, and other text processing details.</para>
/// </remarks>
public interface IWordCounter
{
	/// <summary>
	/// Synchronously reads from the provided string and counts total the number of words.
	/// </summary>
	/// <param name="source">The string from which to count words.</param>
	/// <returns>The total number of words counted.</returns>
	int CountWords(string source);
}