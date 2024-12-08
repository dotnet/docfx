// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Docfx.MarkdigEngine.Extensions;

public class MarkdownDocumentVisitor
{
    private readonly IMarkdownObjectRewriter _rewriter;

    public MarkdownDocumentVisitor(IMarkdownObjectRewriter rewriter)
    {
        _rewriter = rewriter;
    }

    public void Visit(MarkdownDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (_rewriter == null)
        {
            return;
        }

        _rewriter.PreProcess(document);

        // rewrite root node of AST
        document = _rewriter.Rewrite(document) as MarkdownDocument;
        if (document == null)
        {
            throw new InvalidOperationException("The result of rewriting a root node in AST can't be null.");
        }

        RewriteContainerBlock(document);

        _rewriter.PostProcess(document);
    }

    private void RewriteContainerBlock(ContainerBlock blocks)
    {
        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            if (block is LeafBlock { Inline: not null } leafBlock)
            {
                RewriteContainerInline(leafBlock.Inline);
            }
            else if (block is ContainerBlock containerBlock)
            {
                RewriteContainerBlock(containerBlock);
            }

            var rewrittenToken = _rewriter.Rewrite(block);
            if (rewrittenToken != null && rewrittenToken != blocks[i] && rewrittenToken is Block rewrittenBlock)
            {
                blocks[i] = rewrittenBlock;
            }
        }
    }

    // TODO: support to return a new inline token while rewriting inline token.
    private void RewriteContainerInline(ContainerInline inlines)
    {
        foreach (var inline in inlines)
        {
            if (inline is LeafInline leafInline)
            {
                _rewriter.Rewrite(leafInline);
            }
            else if (inline is ContainerInline containerInline)
            {
                RewriteContainerInline(containerInline);
            }
        }

        _rewriter.Rewrite(inlines);
    }
}
