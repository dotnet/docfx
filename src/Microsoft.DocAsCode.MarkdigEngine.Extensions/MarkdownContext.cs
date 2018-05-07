// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;

    public class MarkdownContext
    {
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
        /// Identifies the file that owns this content.
        /// </summary>
        public object File { get; }

        /// <summary>
        /// Whether the content is parsed as inline only.
        /// </summary>
        public bool IsInline { get; }

        /// <summary>
        /// Whether source info is enabled in output.
        /// </summary>
        public bool EnableSourceInfo { get; }

        /// <summary>
        /// Validation rules.
        /// </summary>
        public MarkdownValidatorBuilder Mvb { get; }

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
        /// Converts <see cref="File"/> to a string to access file system.
        /// </summary>
        public Func<object, string> GetFilePath { get; }

        /// <summary>
        /// Used to detect circular references.
        /// </summary>
        public ImmutableStack<object> RecursionDetector { get; }

        /// <summary>
        /// Gets all the dependencies referenced by the root markdown context.
        /// </summary>
        public HashSet<object> Dependencies { get; }

        public MarkdownContext(
            object filePath,
            bool isInline,
            bool enableSourceInfo,
            IReadOnlyDictionary<string, string> tokens,
            MarkdownValidatorBuilder mvb,
            ReadFileDelegate readFile = null,
            GetLinkDelegate getLink = null,
            Func<object, string> getFilePath = null,
            ImmutableStack<object> recursionDetector = null,
            HashSet<object> dependencies = null)
        {
            File = filePath;
            IsInline = isInline;
            EnableSourceInfo = enableSourceInfo;
            Mvb = mvb;

            Tokens = tokens ?? ImmutableDictionary<string, string>.Empty;
            ReadFile = readFile ?? ReadFileDefault;
            GetLink = getLink ?? ((path, relativeTo) => path);
            GetFilePath = getFilePath ?? (file => file.ToString());
            Dependencies = dependencies ?? new HashSet<object>();
            RecursionDetector = (recursionDetector ?? ImmutableStack<object>.Empty).Push(filePath);
        }

        private static (string content, object file) ReadFileDefault(string path, object relativeTo)
        {
            var target = relativeTo != null ? path : Path.Combine(relativeTo.ToString(), path);

            if (System.IO.File.Exists(target))
            {
                return (System.IO.File.ReadAllText(target), target);
            }

            return (null, null);
        }
    }
}