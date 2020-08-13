// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;
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

        private readonly Lazy<PublishUrlMap> _publishUrlMap;

        private static readonly ThreadLocal<Stack<Status>> t_status = new ThreadLocal<Stack<Status>>(() => new Stack<Status>());

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
            Lazy<PublishUrlMap> publishUrlMap)
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

            _markdownContext = new MarkdownContext(GetToken, LogInfo, LogSuggestion, LogWarning, LogError, ReadFile, GetLink, GetImageLink);
            var markdownValidationRules = ContentValidator.GetValidationPhysicalFilePath(fileResolver, config.MarkdownValidationRules);
            var allowlists = ContentValidator.GetValidationPhysicalFilePath(fileResolver, config.Allowlists);
            var disallowlists = ContentValidator.GetValidationPhysicalFilePath(fileResolver, config.Disallowlists);

            if (!string.IsNullOrEmpty(markdownValidationRules))
            {
                _validatorProvider = new OnlineServiceMarkdownValidatorProvider(
                    new ContentValidationContext(markdownValidationRules, allowlists, disallowlists),
                    new ContentValidationLogger(_markdownContext));
            }

            _pipelines = new[]
            {
                CreateMarkdownPipeline(),
                CreateInlineMarkdownPipeline(),
                CreateTocMarkdownPipeline(),
            };
        }

        public MarkdownDocument Parse(ErrorBuilder errors, string content, Document file, MarkdownPipelineType pipelineType)
        {
            using (InclusionContext.PushFile(file))
            {
                try
                {
                    var status = new Status(errors);

                    t_status.Value!.Push(status);

                    return Markdown.Parse(content, _pipelines[(int)pipelineType]);
                }
                finally
                {
                    t_status.Value!.Pop();
                }
            }
        }

        public string ToHtml(ErrorBuilder errors, string markdown, Document file, MarkdownPipelineType pipelineType, ConceptualModel? conceptual = null)
        {
            using (InclusionContext.PushFile(file))
            {
                try
                {
                    var status = new Status(errors, conceptual);

                    t_status.Value!.Push(status);

                    return Markdown.ToHtml(markdown, _pipelines[(int)pipelineType]);
                }
                finally
                {
                    t_status.Value!.Pop();
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

                // Place markdown rewriters before `EnsureParent` as rewriters breaks parent chain.
                .UseEnsureParent()
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
                .UseTelemetry()
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
                .UseHtml(GetErrors, GetLink, GetImageLink, GetXref, IsArchived)
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

            builder.UseEnsureParent()
                   .UseFilePath()
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
            // At parser stage, we are unable to reliably access GetPathToRoot method,
            // use InclusionContext to report syntax diagnostics.
            var source = new SourceInfo(
                ((Document)InclusionContext.File).FilePath,
                line is null ? (origin?.Line + 1) ?? 0 : (line.Value + 1),
                line is null ? (origin?.Column + 1) ?? 0 : 0);

            t_status.Value!.Peek().Errors.Add(new Error(level, code, message, source));
        }

        private static ErrorBuilder GetErrors()
        {
            return t_status.Value!.Peek().Errors;
        }

        private static ConceptualModel? GetConceptual()
        {
            return t_status.Value!.Peek().Conceptual;
        }

        private string? GetLayout(FilePath path)
        {
            return _metadataProvider.GetMetadata(GetErrors(), path).Layout;
        }

        private bool IsArchived(FilePath path)
        {
            return _metadataProvider.GetMetadata(GetErrors(), path).IsArchived;
        }

        private (string? content, object? file) ReadFile(string path, MarkdownObject origin)
        {
            var status = t_status.Value!.Peek();
            var (error, file) = _linkResolver.ResolveContent(new SourceInfo<string>(path, origin.GetSourceInfo()), origin.GetFilePath());
            status.Errors.AddIfNotNull(error);

            return file is null ? default : (_input.ReadString(file.FilePath).Replace("\r", ""), file);
        }

        private string GetLink(string path, MarkdownObject origin)
        {
            var status = t_status.Value!.Peek();
            var (error, link, _) =
                _linkResolver.ResolveLink(new SourceInfo<string>(path, origin.GetSourceInfo()), origin.GetFilePath(), (Document)InclusionContext.RootFile);
            status.Errors.AddIfNotNull(error);

            return link;
        }

        private string GetImageLink(string path, MarkdownObject origin, string? altText)
        {
            if (altText is null && origin is LinkInline linkInline && linkInline.IsImage)
            {
                altText = ToPlainText(origin);
            }

            return GetImageLink(new SourceInfo<string>(path, origin.GetSourceInfo()), origin, altText);
        }

        private string GetImageLink(SourceInfo<string> href, MarkdownObject? origin, string? altText)
        {
            _contentValidator.ValidateImageLink((Document)InclusionContext.RootFile, href, origin, altText);
            var link = GetLink(href);
            return link;
        }

        private string GetLink(SourceInfo<string> href)
        {
            var status = t_status.Value!.Peek();
            var (error, link, _) = _linkResolver.ResolveLink(href, GetDocument(href), (Document)InclusionContext.RootFile);
            status.Errors.AddIfNotNull(error);

            return link;
        }

        private (string? href, string display) GetXref(
            SourceInfo<string>? href, SourceInfo<string>? uid, bool suppressXrefNotFound)
        {
            var status = t_status.Value!.Peek();

            var (error, link, display, _) = href.HasValue
                ? _xrefResolver.ResolveXrefByHref(href.Value, GetDocument(href.Value), (Document)InclusionContext.RootFile)
                : uid.HasValue
                    ? _xrefResolver.ResolveXrefByUid(uid.Value, GetDocument(uid.Value), (Document)InclusionContext.RootFile)
                    : default;

            if (!suppressXrefNotFound)
            {
                status.Errors.AddIfNotNull(error);
            }
            return (link, display);
        }

        private Document GetDocument<T>(SourceInfo<T> sourceInfo)
        {
            return sourceInfo.Source?.File is FilePath filePath ? _documentProvider.GetDocument(filePath) : (Document)InclusionContext.File;
        }

        private MonikerList ParseMonikerRange(SourceInfo<string?> monikerRange)
        {
            return _monikerProvider.GetZoneLevelMonikers(GetErrors(), ((Document)InclusionContext.RootFile).FilePath, monikerRange);
        }

        private MonikerList GetFileLevelMonikers()
        {
            return _monikerProvider.GetFileLevelMonikers(GetErrors(), ((Document)InclusionContext.RootFile).FilePath);
        }

        private string? GetCanonicalVersion()
        {
            return _publishUrlMap.Value.GetCanonicalVersion(((Document)InclusionContext.RootFile).SiteUrl);
        }

        private class Status
        {
            public ConceptualModel? Conceptual { get; }

            public ErrorBuilder Errors { get; }

            public Status(ErrorBuilder errors, ConceptualModel? conceptual = null)
            {
                Errors = errors;
                Conceptual = conceptual;
            }
        }
    }
}
