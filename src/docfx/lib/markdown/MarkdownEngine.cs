// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Docs.MarkdigExtensions;
using Microsoft.Docs.Validation;

namespace Microsoft.Docs.Build;

internal class MarkdownEngine
{
    private readonly LinkResolver _linkResolver;
    private readonly XrefResolver _xrefResolver;
    private readonly DocumentProvider _documentProvider;
    private readonly Input _input;
    private readonly MonikerProvider _monikerProvider;
    private readonly TemplateEngine _templateEngine;
    private readonly ContentValidator _contentValidator;

    private readonly MarkdownContext _markdownContext;
    private readonly MarkdownPipeline[] _pipelines;

    private readonly PublishUrlMap _publishUrlMap;
    private readonly HtmlSanitizer _htmlSanitizer;

    private readonly string _hostName;

    private static readonly ThreadLocal<Stack<Status>> s_status = new(() => new());

    public MarkdownEngine(
        Input input,
        LinkResolver linkResolver,
        XrefResolver xrefResolver,
        DocumentProvider documentProvider,
        MonikerProvider monikerProvider,
        TemplateEngine templateEngine,
        ContentValidator contentValidator,
        PublishUrlMap publishUrlMap,
        HtmlSanitizer htmlSanitizer,
        string hostName)
    {
        _input = input;
        _linkResolver = linkResolver;
        _xrefResolver = xrefResolver;
        _documentProvider = documentProvider;
        _monikerProvider = monikerProvider;
        _templateEngine = templateEngine;
        _contentValidator = contentValidator;
        _publishUrlMap = publishUrlMap;
        _htmlSanitizer = htmlSanitizer;
        _hostName = hostName;

        _markdownContext = new(GetToken, LogInfo, LogSuggestion, LogWarning, LogError, ReadFile, GetLink, GetImageLink);
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
            .UseAutoLinks(new() { UseHttpsForWWWLinks = true })
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

            // Extensions before this line sees inclusion AST twice:
            // - Once AST for the entry file without InclusionBlock expanded
            // - Once AST for only the included file.
            .UseExpandInclude(_markdownContext, GetErrors)

            // Extensions after this line sees an expanded inclusion AST only once.
            .UseDocsValidation(this, _contentValidator, GetFileLevelMonikers, GetCanonicalVersion)
            .UseResolveLink(_markdownContext)
            .UseXref(GetXref)
            .UseHtml(GetErrors, GetLink, GetXref, _htmlSanitizer, _documentProvider)
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

    private LinkNode? TransformLinkInfo(LinkInfo link)
    {
        if (link.MarkdownObject is null)
        {
            return null;
        }

        LinkNode node = link.IsImage
        ? new ImageLinkNode
        {
            ImageLinkType = Enum.TryParse(link.ImageType, true, out ImageLinkType type) ? type : ImageLinkType.Default,
            AltText = link.AltText,
            IsInline = link.MarkdownObject.IsInlineImage(link.HtmlSourceIndex),
        }
        : new HyperLinkNode
        {
            IsVisible = MarkdigUtility.IsVisible(link.MarkdownObject),
            HyperLinkType = link.MarkdownObject switch
            {
                AutolinkInline => HyperLinkType.AutoLink,
                HtmlBlock or HtmlInline or TripleColonInline or TripleColonBlock => HyperLinkType.HtmlAnchor,
                _ => HyperLinkType.Default,
            },
        };

        return node with
        {
            UrlLink = link.Href,
            SourceInfo = link.Href.Source,
            ParentSourceInfoList = link.MarkdownObject.GetInclusionStack(),
            Monikers = link.MarkdownObject.GetZoneLevelMonikers(),
            ZonePivots = link.MarkdownObject.GetZonePivots(),
            TabbedConceptualHeader = link.MarkdownObject.GetTabId(),
            HostName = _hostName,
        };
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

    private string GetLink(string path, MarkdownObject origin)
    {
        return GetLink(new()
        {
            Href = new(path, origin.GetSourceInfo()),
            MarkdownObject = origin,
        });
    }

    private string GetImageLink(string path, MarkdownObject origin, string? altText, string? imageType)
    {
        return GetLink(new()
        {
            Href = new(path, origin.GetSourceInfo()),
            TagName = "img",
            AttributeName = "src",
            MarkdownObject = origin,
            AltText = altText ?? (origin is LinkInline linkInline && linkInline.IsImage ? ToPlainText(origin) : null),
            ImageType = imageType,
        });
    }

    private string GetLink(LinkInfo link)
    {
        var status = s_status.Value!.Peek();
        var (linkErrors, result, _) =
            _linkResolver.ResolveLink(link.Href, GetFilePath(link.Href), GetRootFilePath(), TransformLinkInfo(link), tagName: link.TagName);
        status.Errors.AddRange(linkErrors);
        return result;
    }

    private XrefLink GetXref(SourceInfo<string>? href, SourceInfo<string>? uid, bool suppressXrefNotFound)
    {
        var status = s_status.Value!.Peek();

        var (error, xrefLink) = href.HasValue
            ? _xrefResolver.ResolveXrefByHref(href.Value, GetFilePath(href.Value), GetRootFilePath())
            : uid.HasValue
                ? _xrefResolver.ResolveXrefByUid(uid.Value, GetFilePath(uid.Value), GetRootFilePath())
                : default;

        if (!suppressXrefNotFound)
        {
            status.Errors.AddIfNotNull(error);
        }
        return xrefLink;
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
