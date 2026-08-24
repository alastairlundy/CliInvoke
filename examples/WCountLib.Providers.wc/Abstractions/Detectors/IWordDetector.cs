/*
    WCountLib.Abstractions
    Copyright (C) 2024-2026 Alastair Lundy

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

// ReSharper disable InconsistentNaming

namespace WCountLib.Providers.wc.Abstractions.Detectors;

/// <summary>
/// An interface for a word detecting service.
/// </summary>
public interface IWordDetector
{
    /// <summary>
    /// Determines whether a string is a word or not.
    /// </summary>
    /// <param name="s">The string to be searched for a word.</param>
    /// <param name="countStringsWithSpacesAsWords">Whether to count strings that contain spaces as words. Set to false by default.</param>
    /// <returns>True if the string is a word; false otherwise.</returns>
    bool IsStringAWord(string s, bool countStringsWithSpacesAsWords = false);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="source"></param>
    /// <param name="countStringsWithSpacesAsWords"></param>
    /// <returns></returns>
    bool IsStringAWord(char[] source, bool countStringsWithSpacesAsWords = false);

    
    /// <summary>
    /// Determines whether a string contains one or more words.
    /// </summary>
    /// <param name="s">The string to be searched for a word.</param>
    /// <param name="wordSeparator">The separator char to look for between words.</param>
    /// <param name="countStringsWithSpacesAsWords">Whether to count strings with spaces in them as words.</param>
    /// <returns>True if one or more words were found, false otherwise.</returns>
    bool DoesStringContainWords(string s, char wordSeparator, bool countStringsWithSpacesAsWords = false);
}