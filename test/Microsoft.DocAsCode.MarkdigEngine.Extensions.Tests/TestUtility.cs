// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Markdig;
    using Markdig.Syntax;
    using Microsoft.DocAsCode.MarkdigEngine.Extensions;
    using Xunit;

    public static class TestUtility
    {
        public static void VerifyMarkup(
            string markdown,
            string html,
            string[] errors = null,
            string[] dependencies = null,
            bool lineNumber = false,
            string filePath = "test.md",
            Dictionary<string, string> tokens = null,
            Dictionary<string, string> files = null,
            Action<MarkdownObject> verifyAST = null)
        {
            errors = errors ?? Array.Empty<string>();
            tokens = tokens ?? new Dictionary<string, string>();
            files = files ?? new Dictionary<string, string>();

            var actualErrors = new List<string>();
            var actualDependencies = new HashSet<string>();

            var markdownContext = new MarkdownContext(
                getToken: key => tokens.TryGetValue(key, out var value) ? value : null,
                logInfo: (a, b, c, d) => { },
                logSuggestion: Log("suggestion"),
                logWarning: Log("warning"),
                logError: Log("error"),
                readFile: ReadFile);

            var pipelineBuilder = new MarkdownPipelineBuilder()
                .UseDocfxExtensions(markdownContext)
                .UseYamlFrontMatter();

            if (lineNumber)
            {
                pipelineBuilder.UseLineNumber();
            }

            var pipeline = pipelineBuilder.Build();

            using (InclusionContext.PushFile(filePath))
            {
                var actualHtml = Markdown.ToHtml(markdown, pipeline);

                if (html != null)
                {
                    Assert.Equal(
                        html.Replace("\r", "").Replace("\n", ""),
                        actualHtml.Replace("\r", "").Replace("\n", ""));
                }

                Assert.Equal(errors.OrderBy(_ => _), actualErrors.OrderBy(_ => _));

                if (dependencies != null)
                {
                    Assert.Equal(dependencies.OrderBy(_ => _), actualDependencies.OrderBy(_ => _));
                }
            }

            MarkdownContext.LogActionDelegate Log(string level)
            {
                return (code, message, origin, line) => actualErrors.Add(code);
            }

            (string content, object file) ReadFile(string path, object relativeTo, MarkdownObject origin)
            {
                var key = Path.Combine(Path.GetDirectoryName(relativeTo.ToString()), path).Replace('\\', '/');

                if (path.StartsWith("~/"))
                {
                    path = path.Substring(2);
                    key = path;
                }

                actualDependencies.Add(path);
                return files.TryGetValue(key, out var value) ? (value, key) : default;
            }
        }
    }
}
