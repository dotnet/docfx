// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MarkdigEngine.Extensions
{
    using System.Collections.Immutable;


    public class MarkdownContextBuilder
    {
        private string _basePath;
        private string _content;
        private string _filePath;
        private bool _isInline = false;
        private MarkdownValidatorBuilder _mvb;
        private ImmutableHashSet<string> _inclusionSet;

        public MarkdownContextBuilder WithFilePath(string filePath)
        {
            _filePath = filePath;
            return this;
        }

        public MarkdownContextBuilder WithBasePath(string basePath)
        {
            _basePath = basePath;
            return this;
        }

        public MarkdownContextBuilder WithContent(string content)
        {
            _content = content;
            return this;
        }

        public MarkdownContextBuilder WithMvb(MarkdownValidatorBuilder mvb)
        {
            _mvb = mvb;
            return this;
        }

        public MarkdownContextBuilder WithIsInline(bool isInline)
        {
            _isInline = isInline;
            return this;
        }

        public MarkdownContextBuilder WithInclusionSet(ImmutableHashSet<string> inclusionSet)
        {
            _inclusionSet = inclusionSet;
            return this;
        }

        public MarkdownContextBuilder WithAddingIncludedFile(string includedFile)
        {
            var set = _inclusionSet ?? ImmutableHashSet<string>.Empty;
            _inclusionSet = set.Add(includedFile);
            return this;
        }

        public MarkdownContextBuilder WithContext(MarkdownContext context)
        {
            _filePath = context.FilePath;
            _basePath = context.BasePath;
            _mvb = context.Mvb;
            _content = context.Content;
            _isInline = context.IsInline;
            _inclusionSet = context.InclusionSet;
            return this;
        }

        public MarkdownContext Build()
        {
            return new MarkdownContext(_filePath, _basePath, _mvb, _content, _isInline, _inclusionSet);
        }
    }
}
