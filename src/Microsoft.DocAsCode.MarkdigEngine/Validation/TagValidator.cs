// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;

    using Markdig.Syntax;
    using Microsoft.DocAsCode.Plugins;

    internal class TagValidator
    {
        public ImmutableList<MarkdownTagValidationRule> Validators { get; }

        private readonly MarkdownContext _context;

        public TagValidator(ImmutableList<MarkdownTagValidationRule> validators, MarkdownContext context)
        {
            _context = context;
            Validators = validators;
        }

        public void Validate(IMarkdownObject markdownObject)
        {
            var tags = Tag.Convert(markdownObject);
            if (tags == null)
            {
                return;
            }

            foreach (var tag in tags)
            {
                foreach (var validator in Validators)
                {
                    ValidateOne(tag, validator);
                }
            }
        }

        private void ValidateOne(Tag tag, MarkdownTagValidationRule validator)
        {
            if (tag.IsOpening || !validator.OpeningTagOnly)
            {
                var hasTagName = validator.TagNames.Any(tagName => string.Equals(tagName, tag.Name, StringComparison.OrdinalIgnoreCase));
                if (hasTagName ^ (validator.Relation == TagRelation.NotIn))
                {
                    ValidateCore(tag, validator);
                }
            }
        }

        private void ValidateCore(Tag tag, MarkdownTagValidationRule validator)
        {
            switch (validator.Behavior)
            {
                case TagValidationBehavior.Warning:
                    _context.LogWarning("invalid-markdown-tag", string.Format(validator.MessageFormatter, tag.Name, tag.Content), null, line: tag.Line);
                    return;
                case TagValidationBehavior.Error:
                    _context.LogError("invalid-markdown-tag", string.Format(validator.MessageFormatter, tag.Name, tag.Content), null, line: tag.Line);
                    return;
                case TagValidationBehavior.None:
                default:
                    return;
            }
        }
    }
}
