// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Extensions.AutoLinks;
using Markdig.Extensions.CustomContainers;
using Markdig.Extensions.Emoji;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Extensions.MediaLinks;
using Markdig.Extensions.SmartyPants;
using Markdig.Extensions.Tables;
using Markdig.Parsers;

namespace Docfx.MarkdigEngine.Extensions;

public static class MarkdownExtensions
{
    enum EmojiMappingOption { Default, DefaultAndSmileys }

    public static MarkdownPipelineBuilder UseDocfxExtensions(
        this MarkdownPipelineBuilder pipeline, MarkdownContext context,
        Dictionary<string, string> notes = null, PlantUmlOptions plantUml = null)
    {
        return pipeline
            .UseMathematics()
            .UseEmphasisExtras(EmphasisExtraOptions.Strikethrough)
            .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
            .UseMediaLinks()
            .UsePipeTables()
            .UseAutoLinks()
            .UseHeadingIdRewriter()
            .UseIncludeFile(context)
            .UseCodeSnippet(context)
            .UseDFMCodeInfoPrefix()
            .UseQuoteSectionNote(context, notes)
            .UseXref()
            .UseEmojiAndSmiley(false)
            .UseTabGroup(context)
            .UseMonikerRange(context)
            .UseInteractiveCode()
            .UseRow(context)
            .UseNestedColumn(context)
            .UseTripleColon(context)
            .UseNoloc()
            .UseResolveLink(context)
            .UsePlantUml(context, plantUml)
            .RemoveUnusedExtensions();
    }

    /// <summary>
    /// Enables optional Markdig extensions that are not added by default with DocFX
    /// </summary>
    /// <param name="pipeline">The markdown pipeline builder</param>
    /// <param name="optionalExtensions">The list of optional extensions</param>
    /// <returns>The pipeline with optional extensions enabled</returns>
    public static MarkdownPipelineBuilder UseOptionalExtensions(
        this MarkdownPipelineBuilder pipeline,
        MarkdigExtensionSetting[] optionalExtensions)
    {
        if (!optionalExtensions.Any())
        {
            return pipeline;
        }

        // Process markdig extensions that requires custom handling.
        var results = new List<MarkdigExtensionSetting>();
        foreach (var extension in optionalExtensions)
        {
            if (TryAddOrReplaceMarkdigExtension(pipeline, extension))
                continue;

            // If markdig extension options are specified. These extension should be handled by above method.
            Debug.Assert(extension.Options == null);

            results.Add(extension);
        }
        optionalExtensions = results.ToArray();

        // Enable remaining markdig extensions with default options.
        pipeline.Configure(string.Join('+', optionalExtensions.Select(x => x.Name)));

        return pipeline;
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>Return true when Markdig extension is added or replaced by this method.</returns>
    private static bool TryAddOrReplaceMarkdigExtension(
        MarkdownPipelineBuilder pipeline,
        MarkdigExtensionSetting extension)
    {
        // See: https://github.com/xoofx/markdig/blob/master/src/Markdig/MarkdownExtensions.cs
        switch (extension.Name.ToLowerInvariant())
        {
            // PipeTableExtension
            case "pipetables":
                {
                    var options = extension.GetOptions(fallbackValue: new PipeTableOptions());
                    pipeline.Extensions.ReplaceOrAdd<PipeTableExtension>(new PipeTableExtension(options));
                    return true;
                }

            // PipeTableExtension (with GitHub Flavored Markdown compatible settings)
            case "gfm-pipetables":
                {
                    var options = extension.GetOptions(fallbackValue: new PipeTableOptions { UseHeaderForColumnCount = true });
                    pipeline.Extensions.ReplaceOrAdd<PipeTableExtension>(new PipeTableExtension(options));
                    return true;
                }

            // EmphasisExtraExtension (Docfx default: AutoIdentifierOptions.Strikethrough)
            case "emphasisextras":
                {
                    var options = extension.GetOptions(fallbackValue: EmphasisExtraOptions.Default);
                    pipeline.Extensions.ReplaceOrAdd<EmphasisExtraExtension>(new EmphasisExtraExtension(options));
                    return true;
                }

            // EmojiExtension (Docfx default: enableSmileys: false)
            case "emojis":
                {
                    var emojiMapping = extension.GetOptions(fallbackValue: EmojiMappingOption.DefaultAndSmileys) switch
                    {
                        EmojiMappingOption.DefaultAndSmileys => EmojiMapping.DefaultEmojisAndSmileysMapping,
                        _ => EmojiMapping.DefaultEmojisOnlyMapping,
                    };
                    pipeline.Extensions.ReplaceOrAdd<EmojiExtension>(new EmojiExtension(emojiMapping));
                    return true;
                }

            // MediaLinkExtension
            case "medialinks":
                {
                    var options = extension.GetOptions(fallbackValue: new MediaOptions());
                    pipeline.Extensions.ReplaceOrAdd<MediaLinkExtension>(new MediaLinkExtension(options));
                    return true;
                }

            // SmartyPantsExtension
            case "smartypants":
                {
                    var options = extension.GetOptions(fallbackValue: new SmartyPantOptions());
                    pipeline.Extensions.ReplaceOrAdd<SmartyPantsExtension>(new SmartyPantsExtension(options));
                    return true;
                }

            // AutoIdentifierExtension (Docfx default:AutoIdentifierOptions.GitHub)
            case "autoidentifiers":
                {
                    var options = extension.GetOptions(fallbackValue: AutoIdentifierOptions.Default);
                    pipeline.Extensions.ReplaceOrAdd<AutoIdentifierExtension>(new AutoIdentifierExtension(options));
                    return true;
                }

            // AutoLinkExtension
            case "autolinks":
                {
                    var options = extension.GetOptions(fallbackValue: new AutoLinkOptions());
                    pipeline.Extensions.ReplaceOrAdd<AutoLinkExtension>(new AutoLinkExtension(options));
                    return true;
                }

            // Other builtin markdig extensions.
            case "advanced":
            case "alerts":
            case "listextras":
            case "hardlinebreak":
            case "footnotes":
            case "footers":
            case "citations":
            case "attributes":
            case "gridtables":
            case "abbreviations":
            case "definitionlists":
            case "customcontainers":
            case "figures":
            case "mathematics":
            case "bootstrap":
            case "tasklists":
            case "diagrams":
            case "nofollowlinks":
            case "noopenerlinks":
            case "noreferrerlinks":
            case "nohtml":
            case "yaml":
            case "nonascii-noescape":
            case "globalization":
            case "common":
            default:
                // Throw exception if options are specified.
                if (extension.Options != null)
                {
                    throw new Exception($"Unknown markdig extension({extension.Name}) is specified. {extension.Options}");
                }

                // These extensions are handled by `MarkdownPipelineBuilder.Configure` method.
                return false;
        }
    }

    private static MarkdownPipelineBuilder RemoveUnusedExtensions(this MarkdownPipelineBuilder pipeline)
    {
        pipeline.Extensions.RemoveAll(extension => extension is CustomContainerExtension);
        return pipeline;
    }

    /// <summary>
    /// This extension removes all the block parser except paragraph. Please use this extension in the last.
    /// </summary>
    public static MarkdownPipelineBuilder UseInlineOnly(this MarkdownPipelineBuilder pipeline)
    {
        pipeline.Extensions.AddIfNotAlready(new InlineOnlyExtension());
        return pipeline;
    }

    public static MarkdownPipelineBuilder UseTabGroup(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
    {
        pipeline.Extensions.AddIfNotAlready(new TabGroupExtension(context));
        return pipeline;
    }

    public static MarkdownPipelineBuilder UseHeadingIdRewriter(this MarkdownPipelineBuilder pipeline)
    {
        pipeline.Extensions.AddIfNotAlready(new HeadingIdExtension());
        return pipeline;
    }

    public static MarkdownPipelineBuilder UseDFMCodeInfoPrefix(this MarkdownPipelineBuilder pipeline)
    {
        var fencedCodeBlockParser = pipeline.BlockParsers.FindExact<FencedCodeBlockParser>();
        if (fencedCodeBlockParser != null)
        {
            fencedCodeBlockParser.InfoPrefix = Constants.FencedCodePrefix;
        }
        else
        {
            pipeline.BlockParsers.AddIfNotAlready(new FencedCodeBlockParser { InfoPrefix = Constants.FencedCodePrefix });
        }
        return pipeline;
    }

    public static MarkdownPipelineBuilder UseQuoteSectionNote(this MarkdownPipelineBuilder pipeline, MarkdownContext context, Dictionary<string, string> notes = null)
    {
        pipeline.Extensions.AddIfNotAlready(new QuoteSectionNoteExtension(context, notes));
        return pipeline;
    }

    public static MarkdownPipelineBuilder UseLineNumber(this MarkdownPipelineBuilder pipeline, Func<object, string> getFilePath = null)
    {
        pipeline.Extensions.AddIfNotAlready(new LineNumberExtension(getFilePath));
        return pipeline;
    }

    public static MarkdownPipelineBuilder UsePlantUml(this MarkdownPipelineBuilder pipeline, MarkdownContext context, PlantUmlOptions options = null)
    {
        pipeline.Extensions.AddIfNotAlready(new PlantUmlExtension(context, options));
        return pipeline;
    }

    public static MarkdownPipelineBuilder UseResolveLink(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
    {
        pipeline.Extensions.AddIfNotAlready(new ResolveLinkExtension(context));
        return pipeline;
    }

    public static MarkdownPipelineBuilder UseIncludeFile(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
    {
        pipeline.Extensions.AddIfNotAlready(new InclusionExtension(context));
        return pipeline;
    }

    public static MarkdownPipelineBuilder UseCodeSnippet(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
    {
        pipeline.Extensions.AddIfNotAlready(new CodeSnippetExtension(context));
        return pipeline;
    }

    public static MarkdownPipelineBuilder UseInteractiveCode(this MarkdownPipelineBuilder pipeline)
    {
        pipeline.Extensions.AddIfNotAlready(new InteractiveCodeExtension());
        return pipeline;
    }

    public static MarkdownPipelineBuilder UseXref(this MarkdownPipelineBuilder pipeline)
    {
        pipeline.Extensions.AddIfNotAlready(new XrefInlineExtension());
        return pipeline;
    }

    public static MarkdownPipelineBuilder UseMonikerRange(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
    {
        pipeline.Extensions.AddIfNotAlready(new MonikerRangeExtension(context));
        return pipeline;
    }
    public static MarkdownPipelineBuilder UseRow(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
    {
        pipeline.Extensions.AddIfNotAlready(new RowExtension(context));
        return pipeline;
    }

    public static MarkdownPipelineBuilder UseNestedColumn(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
    {
        pipeline.Extensions.AddIfNotAlready(new NestedColumnExtension(context));
        return pipeline;
    }

    public static MarkdownPipelineBuilder UseTripleColon(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
    {
        pipeline.Extensions.AddIfNotAlready(new TripleColonExtension(context));
        return pipeline;
    }

    public static MarkdownPipelineBuilder UseNoloc(this MarkdownPipelineBuilder pipeline)
    {
        pipeline.Extensions.AddIfNotAlready(new NolocExtension());
        return pipeline;
    }
}
