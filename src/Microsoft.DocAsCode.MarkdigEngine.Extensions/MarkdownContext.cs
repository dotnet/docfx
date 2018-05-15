// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;

    public class MarkdownContext
    {
        /// <summary>
        /// Log delegate
        /// </summary>
        public delegate void LogActionDelegate(string message, string phase = null, string file = null, string line = null, string code = null);

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
        /// <returns>Url bound to the path</returns>
        public delegate string GetLinkDelegate(string path, object relativeTo);

        /// <summary>
        /// Localizable text tokens used for rendering notes.
        /// </summary>
        public IReadOnlyDictionary<string, string> Tokens { get; }

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

        public MarkdownContext(
            IReadOnlyDictionary<string, string> tokens,
            LogActionDelegate logWarning,
            LogActionDelegate logError,
            ReadFileDelegate readFile = null,
            GetLinkDelegate getLink = null)
        {
            Tokens = tokens ?? ImmutableDictionary<string, string>.Empty;
            ReadFile = readFile ?? ReadFileDefault;
            GetLink = getLink ?? ((path, relativeTo) => path);

            LogWarning = logWarning;
            LogError = logError;
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