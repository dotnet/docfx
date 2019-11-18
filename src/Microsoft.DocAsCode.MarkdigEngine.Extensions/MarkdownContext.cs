// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System;
    using System.IO;
    using Markdig.Syntax;

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
        /// <param name="relativeTo">The source file that path is based on.</param>
        /// <param name="origin">The original markdown element that triggered the read request.</param>
        /// <returns>An stream and the opened file, or default if such file does not exists.</returns>
        public delegate (string content, object file) ReadFileDelegate(string path, object relativeTo, MarkdownObject origin);


        /// <summary>
        /// Reads a file as text.
        /// </summary>
        public ReadFileDelegate ReadFile { get; }

        /// <summary>
        /// Log info
        /// </summary>
        public LogActionDelegate LogInfo{ get; }

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
            ReadFileDelegate readFile = null)
        {
            _getToken = getToken ?? (_ => null);
            ReadFile = readFile ?? ReadFileDefault;
            LogInfo = logInfo ?? ((a, b, c, d) => { });
            LogSuggestion = logSuggestion ?? ((a, b, c, d) => { });
            LogWarning = logWarning ?? ((a, b, c, d) => { });
            LogError = logError ?? ((a, b, c, d) => { });
        }

        private static (string content, object file) ReadFileDefault(string path, object relativeTo, MarkdownObject origin)
        {
            var target = relativeTo != null ? path : Path.Combine(relativeTo.ToString(), path);

            if (File.Exists(target))
            {
                return (File.ReadAllText(target), target);
            }

            return (null, null);
        }
    }
}