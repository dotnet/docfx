// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Apex.Validation.Shared;
using Markdig.Syntax;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal class ContentValidationLogger : IValidationLogger
    {
        private readonly MarkdownContext _markdownContext;

        public ContentValidationLogger(MarkdownContext markdownContext)
        {
            _markdownContext = markdownContext;
        }

        public void LogInfo(string code, string message, MarkdownObject? origin = null, string? filePath = null, int? line = null)
        {
            if (!string.IsNullOrEmpty(code))
            {
                _markdownContext.LogInfo(code, message, origin, line - 1);
            }
        }

        public void LogSuggestion(string code, string message, MarkdownObject? origin = null, string? filePath = null, int? line = null)
        {
            if (!string.IsNullOrEmpty(code))
            {
                _markdownContext.LogSuggestion(code, message, origin, line - 1);
            }
        }

        public void LogWarning(string code, string message, MarkdownObject? origin = null, string? filePath = null, int? line = null)
        {
            if (!string.IsNullOrEmpty(code))
            {
                _markdownContext.LogWarning(code, message, origin, line - 1);
            }
        }

        public void LogError(string code, string message, MarkdownObject? origin = null, string? filePath = null, int? line = null)
        {
            if (!string.IsNullOrEmpty(code))
            {
                _markdownContext.LogError(code, message, origin, line - 1);
            }
        }
    }
}
