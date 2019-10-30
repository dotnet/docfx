// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System;
    using System.IO;
    using Markdig;
    using Markdig.Renderers;
    using Markdig.Syntax;
    using Microsoft.DocAsCode.Common;

    public class ValidationExtension : IMarkdownExtension
    {
        private readonly MarkdownValidatorBuilder _mvb;

        private readonly MarkdownContext _context;

        public ValidationExtension(MarkdownValidatorBuilder validationBuilder, MarkdownContext context)
        {
            _mvb = validationBuilder;
            _context = context;
        }

        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            var tokenRewriter = _mvb.CreateRewriter(_context);
            var visitor = new MarkdownDocumentVisitor(tokenRewriter);

            pipeline.DocumentProcessed += document =>
            {
                SetSchemaName(document);
                visitor.Visit(document);
            };
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {

        }

        public static void SetSchemaName(MarkdownDocument document)
        {
            // TODO: add this detection logic in terms of performance optimization, should remove once mime is available in context
            if (InclusionContext.IsInclude
                && (string.Equals(Path.GetExtension(InclusionContext.RootFile?.ToString()), ".yml", StringComparison.OrdinalIgnoreCase)
                || string.Equals(Path.GetExtension(InclusionContext.RootFile?.ToString()), ".yaml", StringComparison.OrdinalIgnoreCase)))
            {
                var schemaName = YamlMime.ReadMime(InclusionContext.RootFile?.ToString());
                if (!string.IsNullOrEmpty(schemaName))
                {
                    document.SetData("SchemaName", schemaName);
                }
            }
        }
    }
}
