// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;
using Microsoft.Docs.Validation;
using Validations.DocFx.Adapter;

namespace Microsoft.Docs.Build
{
    internal class MarkdownEngine
    {
        private readonly LinkResolver _linkResolver;
        private readonly XrefResolver _xrefResolver;
        private readonly DocumentProvider _documentProvider;
        private readonly Input _input;
        private readonly MetadataProvider _metadataProvider;
        private readonly MonikerProvider _monikerProvider;
        private readonly TemplateEngine _templateEngine;
        private readonly ContentValidator _contentValidator;

        private readonly MarkdownContext _markdownContext;
        private readonly OnlineServiceMarkdownValidatorProvider? _validatorProvider;
        private readonly MarkdownPipeline[] _pipelines;

        private readonly PublishUrlMap _publishUrlMap;

        private static readonly ThreadLocal<Stack<Status>> s_status = new(() => new());

        public MarkdownEngine(
            Config config,
            Input input,
            FileResolver fileResolver,
            LinkResolver linkResolver,
            XrefResolver xrefResolver,
            DocumentProvider documentProvider,
            MetadataProvider metadataProvider,
            MonikerProvider monikerProvider,
            TemplateEngine templateEngine,
            ContentValidator contentValidator,
            PublishUrlMap publishUrlMap)
        {
            _input = input;
            _linkResolver = linkResolver;
            _xrefResolver = xrefResolver;
            _documentProvider = documentProvider;
            _metadataProvider = metadataProvider;
            _monikerProvider = monikerProvider;
            _templateEngine = templateEngine;
            _contentValidator = contentValidator;
            _publishUrlMap = publishUrlMap;

            _markdownContext = new(GetToken, LogInfo, LogSuggestion, LogWarning, LogError, ReadFile, GetLink, GetImageLink);

            if (!string.IsNullOrEmpty(config.MarkdownValidationRules))
            {
                _validatorProvider = new(
                    new ContentValidationContext(
                        fileResolver.ResolveFilePath(config.MarkdownValidationRules),
                        fileResolver.ResolveFilePath(config.Allowlists),
                        ""),
                    new ContentValidationLogger(_markdownContext));
            }

            _pipelines = new[]
            {
                CreateMarkdownPipeline(),
                CreateInlineMarkdownPipeline(),
                CreateTocMarkdownPipeline(),
            };
        }

        public MarkdownDocument Parse(ErrorBuilder errors, string content, SourceInfo sourceInfo, MarkdownPipelineType pipelineType)
        {
            using (InclusionContext.PushFile(sourceInfo))
            {
                try
                {
                    var status = new Status(errors);

                    s_status.Value!.Push(status);

                    return Markdown.Parse(content, _pipelines[(int)pipelineType]);
                }
                finally
                {
                    s_status.Value!.Pop();
                }
            }
        }

        public string ToHtml(
            ErrorBuilder errors,
            string markdown,
            SourceInfo sourceInfo,
            MarkdownPipelineType pipelineType,
            ConceptualModel? conceptual = null,
            bool contentFallback = true)
        {
            using (InclusionContext.PushFile(sourceInfo))
            {
                try
                {
                    var status = new Status(errors, contentFallback, conceptual);

                    s_status.Value!.Push(status);

                    return Markdown.ToHtml(markdown, _pipelines[(int)pipelineType]);
                }
                finally
                {
                    s_status.Value!.Pop();
                }
            }
        }

        public string ToHtml(MarkdownObject markdownObject)
        {
            var sb = new StringBuilder();
            using var writer = new StringWriter(sb);
            var renderer = new HtmlRenderer(writer);

            _pipelines[(int)MarkdownPipelineType.Markdown].Setup(renderer);
            renderer.Render(markdownObject);
            writer.Flush();

            // Trim trailing \n
            if (sb.Length > 0 && sb[^1] == '\n')
            {
                sb.Length--;
            }

            return sb.ToString();
        }

        public string ToPlainText(MarkdownObject markdownObject)
        {
            var sb = new StringBuilder();
            using var writer = new StringWriter(sb);
            var renderer = new HtmlRenderer(writer)
            {
                EnableHtmlForBlock = false,
                EnableHtmlForInline = false,
                EnableHtmlEscape = false,
            };
            renderer.ObjectRenderers.Replace<CodeInlineRenderer>(new NewCodeInlineRenderer());

            _pipelines[(int)MarkdownPipelineType.Markdown].Setup(renderer);
            renderer.Render(markdownObject);
            writer.Flush();

            // Trim trailing \n
            if (sb.Length > 0 && sb[^1] == '\n')
            {
                sb.Length--;
            }

            return sb.ToString();
        }

        private MarkdownPipeline CreateMarkdownPipeline()
        {
            return CreateMarkdownPipelineBuilder().Build();
        }

        private MarkdownPipeline CreateInlineMarkdownPipeline()
        {
            return CreateMarkdownPipelineBuilder().UseInlineOnly().Build();
        }

        private MarkdownPipelineBuilder CreateMarkdownPipelineBuilder()
        {
            return new MarkdownPipelineBuilder()
                .UseHeadingIdRewriter()
                .UseTabGroup(_markdownContext)
                .UseInteractiveCode()
                .UseFilePath()
                .UseYamlFrontMatter()
                .UseEmphasisExtras(EmphasisExtraOptions.Strikethrough)
                .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
                .UseMediaLinks()
                .UsePipeTables()
                .UseAutoLinks()
                .UseIncludeFile(_markdownContext)
                .UseCodeSnippet(_markdownContext)
                .UseFencedCodeLangPrefix()
                .UseQuoteSectionNote(_markdownContext)
                .UseXref()
                .UseEmojiAndSmiley(false)
                .UseMonikerRange(_markdownContext)
                .UseRow(_markdownContext)
                .UseNestedColumn(_markdownContext)
                .UseTripleColon(_markdownContext)
                .UseNoloc()
                .UseTelemetry(_documentProvider)
                .UseMonikerZone(ParseMonikerRange)
                .UseApexValidation(_validatorProvider, GetLayout)

                // Extensions before this line sees inclusion AST twice:
                // - Once AST for the entry file without InclusionBlock expanded
                // - Once AST for only the included file.
                .UseExpandInclude(_markdownContext, GetErrors)

                // Extensions after this line sees an expanded inclusion AST only once.
                .UseDocsValidation(this, _contentValidator, GetFileLevelMonikers, GetCanonicalVersion)
                .UseResolveLink(_markdownContext)
                .UseXref(GetXref)
                .UseHtml(_documentProvider, _metadataProvider, GetErrors, GetLink, GetImageLink, GetXref)
                .UseExtractTitle(this, GetConceptual);
        }

        private static MarkdownPipeline CreateTocMarkdownPipeline()
        {
            var builder = new MarkdownPipelineBuilder();

            // Only supports heading block and link inline
            builder.BlockParsers.RemoveAll(parser => !(
                parser is HeadingBlockParser || parser is ParagraphBlockParser ||
                parser is ThematicBreakParser || parser is HtmlBlockParser));

            builder.InlineParsers.RemoveAll(parser => !(parser is LinkInlineParser || parser is EscapeInlineParser));

            builder.BlockParsers.Find<HeadingBlockParser>().MaxLeadingCount = int.MaxValue;

            builder.UseFilePath()
                   .UseYamlFrontMatter()
                   .UseXref()
                   .UsePreciseSourceLocation();

            return builder.Build();
        }

        private string? GetToken(string key)
        {
            return _templateEngine.GetToken(key);
        }

        private static void LogInfo(string code, string message, MarkdownObject? origin, int? line)
        {
            Log.Write($"{code}: {message}");
        }

        private static void LogError(string code, string message, MarkdownObject? origin, int? line)
        {
            LogItem(ErrorLevel.Error, code, message, origin, line);
        }

        private static void LogWarning(string code, string message, MarkdownObject? origin, int? line)
        {
            LogItem(ErrorLevel.Warning, code, message, origin, line);
        }

        private static void LogSuggestion(string code, string message, MarkdownObject? origin, int? line)
        {
            LogItem(ErrorLevel.Suggestion, code, message, origin, line);
        }

        private static void LogItem(ErrorLevel level, string code, string message, MarkdownObject? origin, int? line)
        {
            var source = (SourceInfo)InclusionContext.File;
            if (origin != null)
            {
                try
                {
                    // After parse stage, where the markdown object tree has fully constructed
                    source = source.WithOffset(origin.GetSourceInfo(line));
                }
                catch (InvalidOperationException)
                {
                    // In parse stage, where the markdown object tree hasn't been constructed yet
                    source = source.WithOffset(origin.Line + 1, origin.Column + 1);
                }
            }
            else if (line != null)
            {
                source = source.WithOffset(line.Value + 1, 0);
            }

            s_status.Value!.Peek().Errors.Add(new Error(level, code, $"{message}", source));
        }

        private static ErrorBuilder GetErrors()
        {
            return s_status.Value!.Peek().Errors;
        }

        private static ConceptualModel? GetConceptual()
        {
            return s_status.Value!.Peek().Conceptual;
        }

        private string? GetLayout(FilePath path)
        {
            return _metadataProvider.GetMetadata(GetErrors(), path).Layout;
        }

        private (string? content, object? file) ReadFile(string path, MarkdownObject origin, bool? contentFallback = null)
        {
            var status = s_status.Value!.Peek();
            var (error, file) = _linkResolver.ResolveContent(
                new SourceInfo<string>(path, origin.GetSourceInfo()),
                origin.GetFilePath(),
                GetRootFilePath(),
                contentFallback ?? status.ContentFallback);
            status.Errors.AddIfNotNull(error);

            return file is null ? default : (_input.ReadString(file).Replace("\r", ""), new SourceInfo(file));
        }

        private string GetImageLink(string path, MarkdownObject origin, string? altText, string? imageType)
        {
            if (altText is null && origin is LinkInline linkInline && linkInline.IsImage)
            {
                altText = ToPlainText(origin);
            }
            var node = new ImageLinkNode
            {
                UrlLink = path,
                ImageLinkType = Enum.TryParse(imageType, true, out ImageLinkType type) ? type : ImageLinkType.Default,
                AltText = altText,
                IsInline = origin.IsInlineImage(-1),
            };
            return GetLink(new SourceInfo<string>(path, origin.GetSourceInfo()), origin, node);
        }

        private string GetImageLink(SourceInfo<string> href, MarkdownObject origin, string? altText, int imageIndex)
        {
            var node = new ImageLinkNode
            {
                UrlLink = href,
                ImageLinkType = ImageLinkType.Default,
                AltText = altText,
                IsInline = origin.IsInlineImage(imageIndex),
            };
            return GetLink(href, origin, node);
        }

        private string GetLink(SourceInfo<string> link, MarkdownObject origin)
        {
            if (origin is LinkInline linkInline)
            {
                Debug.Assert(!linkInline.IsImage);
            }
            var node = new HyperLinkNode
            {
                UrlLink = link,
                IsVisible = MarkdigUtility.IsVisible(origin),
                HyperLinkType = origin switch
                {
                    AutolinkInline => HyperLinkType.AutoLink,
                    HtmlBlock or HtmlInline or TripleColonInline or TripleColonBlock => HyperLinkType.HtmlAnchor,
                    _ => HyperLinkType.Default,
                },
            };
            return GetLink(link, origin, node);
        }

        private string GetLink(string path, MarkdownObject origin) => GetLink(new SourceInfo<string>(path, origin.GetSourceInfo()), origin);

        private string GetLink(SourceInfo<string> href, MarkdownObject origin, LinkNode outer)
        {
            // fill common field of LinkNode
            var node = outer with
            {
                SourceInfo = href.Source,
                ParentSourceInfoList = origin.GetInclusionStack(),
                Monikers = origin.GetZoneLevelMonikers(),
                ZonePivots = origin.GetZonePivots(),
                TabbedConceptualHeader = origin.GetTabId(),
            };
            _contentValidator.ValidateLink(GetRootFilePath(), node);
            var status = s_status.Value!.Peek();
            var (error, link, _) = _linkResolver.ResolveLink(href, GetFilePath(href), GetRootFilePath());
            status.Errors.AddIfNotNull(error);
            return link;
        }

        private (string? href, string display) GetXref(SourceInfo<string>? href, SourceInfo<string>? uid, bool suppressXrefNotFound)
        {
            var status = s_status.Value!.Peek();

            var (error, link, display, _) = href.HasValue
                ? _xrefResolver.ResolveXrefByHref(href.Value, GetFilePath(href.Value), GetRootFilePath())
                : uid.HasValue
                    ? _xrefResolver.ResolveXrefByUid(uid.Value, GetFilePath(uid.Value), GetRootFilePath())
                    : default;

            if (!suppressXrefNotFound)
            {
                status.Errors.AddIfNotNull(error);
            }
            return (link, display);
        }

        private static FilePath GetFilePath<T>(SourceInfo<T> sourceInfo)
        {
            return sourceInfo.Source?.File ?? ((SourceInfo)InclusionContext.File).File;
        }

        private static FilePath GetRootFilePath()
        {
            return ((SourceInfo)InclusionContext.RootFile).File;
        }

        private MonikerList ParseMonikerRange(SourceInfo<string?> monikerRange)
        {
            return _monikerProvider.GetZoneLevelMonikers(GetErrors(), GetRootFilePath(), monikerRange);
        }

        private MonikerList GetFileLevelMonikers()
        {
            return _monikerProvider.GetFileLevelMonikers(GetErrors(), GetRootFilePath());
        }

        private string? GetCanonicalVersion()
        {
            return _publishUrlMap.GetCanonicalVersion(GetRootFilePath());
        }

        private class Status
        {
            public ConceptualModel? Conceptual { get; }

            public ErrorBuilder Errors { get; }

            public bool ContentFallback { get; }

            public Status(ErrorBuilder errors, bool contentFallback = true, ConceptualModel? conceptual = null)
            {
                Errors = errors;
                Conceptual = conceptual;
                ContentFallback = contentFallback;
            }
        }

        // patch of CodeInlineRenderer, will be fixed in upstream. todo
        private class NewCodeInlineRenderer : CodeInlineRenderer
        {
            protected override void Write(HtmlRenderer renderer, CodeInline obj)
            {
                if (renderer.EnableHtmlForInline)
                {
                    renderer.Write("<code").WriteAttributes(obj).Write(">");
                }
                if (renderer.EnableHtmlEscape)
                {
                    renderer.WriteEscape(obj.Content);
                }
                else
                {
                    renderer.Write(obj.Content);
                }
                if (renderer.EnableHtmlForInline)
                {
                    renderer.Write("</code>");
                }
            }
        }
    }
}
