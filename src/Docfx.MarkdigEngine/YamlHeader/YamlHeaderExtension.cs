// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Docfx.MarkdigEngine.Extensions;

public class YamlHeaderExtension : IMarkdownExtension
{
    private readonly MarkdownContext _context;

    public bool AllowInMiddleOfDocument { get; init; }

    public YamlHeaderExtension(MarkdownContext context)
    {
        _context = context;
    }

    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.BlockParsers.Contains<YamlFrontMatterParser>())
        {
            // Insert the YAML parser before the thematic break parser, as it is also triggered on a --- dash
            pipeline.BlockParsers.InsertBefore<ThematicBreakParser>(new YamlHeaderParser { AllowInMiddleOfDocument = AllowInMiddleOfDocument });
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (!renderer.ObjectRenderers.Contains<YamlHeaderRenderer>())
        {
            renderer.ObjectRenderers.InsertBefore<CodeBlockRenderer>(new YamlHeaderRenderer(_context));
        }
    }

    private sealed class YamlHeaderParser : YamlFrontMatterParser
    {
        public override BlockState TryOpen(BlockProcessor processor)
        {
            var state = base.TryOpen(processor);
            if (state == BlockState.None || !AllowInMiddleOfDocument || processor.LineIndex == 0)
            {
                return state;
            }

            try
            {
                using var reader = new SourceReader(processor.Line.Text, processor.Start);
                var parser = new Parser(reader);
                parser.Consume<StreamStart>();
                parser.Consume<DocumentStart>();
                if (parser.Accept<MappingStart>(out _))
                {
                    return state;
                }
            }
            catch (YamlException)
            {
                // Keep malformed headers on the renderer's existing diagnostic path.
                return state;
            }

            // Only mappings can introduce another overwrite section.
            processor.NewBlocks.Pop();
            return BlockState.None;
        }

        // Read ahead without copying the rest of the document for every candidate header.
        private sealed class SourceReader(string text, int position) : TextReader
        {
            public override int Peek() => position < text.Length ? text[position] : -1;

            public override int Read() => position < text.Length ? text[position++] : -1;

            public override int Read(char[] buffer, int index, int count) => Read(buffer.AsSpan(index, count));

            public override int Read(Span<char> buffer)
            {
                var length = Math.Min(buffer.Length, text.Length - position);
                text.AsSpan(position, length).CopyTo(buffer);
                position += length;
                return length;
            }
        }
    }
}
