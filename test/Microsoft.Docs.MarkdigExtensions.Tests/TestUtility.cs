// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Syntax;
using Xunit;

namespace Microsoft.Docs.MarkdigExtensions.Tests;

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
        Dictionary<string, string> files = null)
    {
        errors ??= Array.Empty<string>();
        tokens ??= new Dictionary<string, string>();
        files ??= new Dictionary<string, string>();

        var actualErrors = new List<string>();
        var actualDependencies = new HashSet<string>();

        var markdownContext = new MarkdownContext(
            getToken: key => tokens.TryGetValue(key, out var value) ? value : null,
            logInfo: (a, b, c, d) => { },
            logSuggestion: Log(),
            logWarning: Log(),
            logError: Log(),
            readFile: ReadFile);
            //getLink: GetLink);

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

        MarkdownContext.LogActionDelegate Log()
        {
            return (code, message, origin, line) => actualErrors.Add(code);
        }

        (string content, object file) ReadFile(string path, MarkdownObject origin, bool? contentFallback = null)
        {
            var key = Path.Combine(Path.GetDirectoryName(InclusionContext.File.ToString()), path).Replace('\\', '/');

            if (path.StartsWith("~/"))
            {
                path = path[2..];
                key = path;
            }

            actualDependencies.Add(path);
            return files.TryGetValue(key, out var value) ? (value, key) : default;
        }

        //string GetLink(LinkInfo link)
        //{
        //    var status = s_status.Value!.Peek();
        //    var (linkErrors, result, _) =
        //        _linkResolver.ResolveLink(link.Href, GetFilePath(link.Href), GetRootFilePath(), TransformLinkInfo(link), tagName: link.TagName);
        //    status.Errors.AddRange(linkErrors);
        //    return result;
        //}

        //string GetLink(string path, MarkdownObject origin)
        //{
        //    return GetLink(new()
        //    {
        //        Href = new(path, origin.GetSourceInfo()),
        //        MarkdownObject = origin,
        //    });
        //}

    }
}
