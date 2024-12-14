// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Syntax;

namespace Docfx.Build.OverwriteDocuments;

public class MarkdownFragmentsCreator
{

    /*
     * EBNF ():
     * <overwrite document> := { <markdown fragment> }
     * <markdown fragment> := L1 inline code heading block, [ YAML code block ], { <property section> }
     * <property section> := L2 inline code heading block, { <markdown content section> }
     * <markdown content section> := paragraph content block - inline code heading block
     */

    private MarkdownDocument _document;

    private int _position;

    private readonly InlineCodeHeadingRule _inlineCodeHeadingRule = new();

    private readonly L1InlineCodeHeadingRule _l1InlineCodeHeadingRule = new();

    private readonly L2InlineCodeHeadingRule _l2InlineCodeHeadingRule = new();

    private readonly YamlCodeBlockRule _yamlCodeBlockRule = new();

    public IEnumerable<MarkdownFragmentModel> Create(MarkdownDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _document = document;
        _position = 0;

        while (More())
        {
            yield return MarkdownFragment();
        }
    }

    private MarkdownFragmentModel MarkdownFragment()
    {
        var result = new MarkdownFragmentModel
        {
            UidSource = Eat(_l1InlineCodeHeadingRule, out var uid),
            Uid = uid,
        };

        if (_yamlCodeBlockRule.Parse(Peek(), out var yamlCodeBlock))
        {
            result.YamlCodeBlock = yamlCodeBlock;
            result.YamlCodeBlockSource = Next();
        }

        result.Contents = PropertySection();
        return result;
    }

    private List<MarkdownPropertyModel> PropertySection()
    {
        var result = new List<MarkdownPropertyModel>();

        while (More() && _l2InlineCodeHeadingRule.Parse(Peek(), out var key))
        {
            var item = new MarkdownPropertyModel
            {
                PropertyName = key,
                PropertyNameSource = Next(),
                PropertyValue = [],
            };
            Block block;
            while ((block = Peek()) != null && !_inlineCodeHeadingRule.Parse(block, out var _))
            {
                item.PropertyValue.Add(Next());
            }
            result.Add(item);
        }

        return result;
    }

    #region helper

    private Block Peek()
    {
        return More() ? _document[_position] : null;
    }

    private Block Eat(IOverwriteBlockRule parser, out string value)
    {
        var block = Peek();
        if (block == null)
        {
            throw new MarkdownFragmentsException($"Expect {parser.TokenName}, but end reached");
        }
        if (parser.Parse(block, out value))
        {
            _position++;
            return block;
        }
        throw new MarkdownFragmentsException($"Expect {parser.TokenName}", block.Line);
    }

    private Block Next()
    {
        var block = Peek();
        if (More())
        {
            _position++;
        }
        return block;
    }

    private bool More()
    {
        return _position < _document.Count;
    }

    #endregion
}
