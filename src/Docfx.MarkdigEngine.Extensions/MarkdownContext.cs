// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Syntax;

namespace Docfx.MarkdigEngine.Extensions;

public class MarkdownContext
{
    /// <summary>
    /// Logs an error or warning message.
    /// </summary>
    public delegate void LogActionDelegate(string code, string message, MarkdownObject origin, int? line = null);

    /// <summary>
    /// Reads a file as text based on path relative to an existing file.
    /// </summary>
    /// <param name="path">Path to the file being opened.</param>
    /// <param name="origin">The original markdown element that triggered the read request.</param>
    /// <returns>An stream and the opened file, or default if such file does not exists.</returns>
    public delegate (string content, object file) ReadFileDelegate(string path, MarkdownObject origin);

    /// <summary>
    /// Allows late binding of urls.
    /// </summary>
    /// <param name="path">Path of the link</param>
    /// <param name="origin">The original markdown element that triggered the read request.</param>
    /// <returns>Url bound to the path</returns>
    public delegate string GetLinkDelegate(string path, MarkdownObject origin);

    /// <summary>
    /// Allows late binding of image urls.
    /// </summary>
    /// <param name="path">Path of the link</param>
    /// <param name="origin">The original markdown element that triggered the read request.</param>
    /// <returns>Image url bound to the path</returns>
    public delegate string GetImageLinkDelegate(string path, MarkdownObject origin, string altText);

    /// <summary>
    /// Reads a file as text.
    /// </summary>
    public ReadFileDelegate ReadFile { get; }

    /// <summary>
    /// Get the link for a given url.
    /// </summary>
    public GetLinkDelegate GetLink { get; }

    /// <summary>
    /// Get the image link for a given image url
    /// </summary>
    public GetImageLinkDelegate GetImageLink { get; }

    /// <summary>
    /// Log info
    /// </summary>
    public LogActionDelegate LogInfo { get; }

    /// <summary>
    /// Log suggestion
    /// </summary>
    public LogActionDelegate LogSuggestion { get; }

    /// <summary>
    /// Log warning
    /// </summary>
    public LogActionDelegate LogWarning { get; }

    /// <summary>
    /// Log error
    /// </summary>
    public LogActionDelegate LogError { get; }

    /// <summary>
    /// Gets the localizable text tokens used for rendering notes.
    /// </summary>
    public string GetToken(string key) => _getToken(key);

    private readonly Func<string, string> _getToken;

    public MarkdownContext(
        Func<string, string> getToken = null,
        LogActionDelegate logInfo = null,
        LogActionDelegate logSuggestion = null,
        LogActionDelegate logWarning = null,
        LogActionDelegate logError = null,
        ReadFileDelegate readFile = null,
        GetLinkDelegate getLink = null,
        GetImageLinkDelegate getImageLink = null)
    {
        _getToken = getToken ?? (_ => null);
        ReadFile = readFile ?? ((a, b) => (a, a));
        GetLink = getLink ?? ((a, b) => a);
        GetImageLink = getImageLink ?? ((a, b, c) => a);
        LogInfo = logInfo ?? ((a, b, c, d) => { });
        LogSuggestion = logSuggestion ?? ((a, b, c, d) => { });
        LogWarning = logWarning ?? ((a, b, c, d) => { });
        LogError = logError ?? ((a, b, c, d) => { });
    }
}
