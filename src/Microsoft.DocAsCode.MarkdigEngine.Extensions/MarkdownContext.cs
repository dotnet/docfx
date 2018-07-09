// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System;
    using System.IO;

    public class MarkdownContext
    {
        /// <summary>
        /// Logs an error or warning message.
        /// </summary>
        public delegate void LogActionDelegate(string code, string message, string file = null, int line = 0);

        /// <summary>
        /// Reads a file as text based on path relative to an existing file.
        /// </summary>
        /// <param name="path">Path to the file being opened.</param>
        /// <param name="relativeTo">The source file that path is based on.</param>
        /// <returns>An stream and the opened file, or default if such file does not exists.</returns>
        public delegate (string content, object file) ReadFileDelegate(string path, object relativeTo);

        /// <summary>
        /// Allows late binding of urls.
        /// </summary>
        /// <param name="path">Path of the link</param>
        /// <param name="relativeTo">The source file that path is based on.</param>
        /// <param name="resultRelativeTo">The entry file that returned URL should be rebased upon.</param>
        /// <returns>Url bound to the path</returns>
        public delegate string GetLinkDelegate(string path, object relativeTo, object resultRelativeTo);


        /// <summary>
        /// Reads a file as text.
        /// </summary>
        public ReadFileDelegate ReadFile { get; }

        /// <summary>
        /// Get the link for a given url.
        /// </summary>
        public GetLinkDelegate GetLink { get; }

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
            LogActionDelegate logWarning = null,
            LogActionDelegate logError = null,
            ReadFileDelegate readFile = null,
            GetLinkDelegate getLink = null)
        {
            _getToken = getToken ?? (_ => null);
            ReadFile = readFile ?? ReadFileDefault;
            GetLink = getLink ?? ((path, a, b) => path);

            LogWarning = logWarning ?? ((a, b, c, d) => { });
            LogError = logError ?? ((a, b, c, d) => { });
        }

        private static (string content, object file) ReadFileDefault(string path, object relativeTo)
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