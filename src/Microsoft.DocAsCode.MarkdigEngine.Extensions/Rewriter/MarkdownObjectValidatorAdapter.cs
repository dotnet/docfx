// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Markdig.Syntax;
    using Microsoft.DocAsCode.MarkdigEngine.Validators;

    internal class MarkdownObjectValidatorAdapter : IMarkdownObjectRewriter
    {
        private Action<IMarkdownObject> _preProcess;
        private Action<IMarkdownObject> _postProcess;

        public ImmutableArray<IMarkdownObjectValidator> Validators { get; }

        public MarkdownObjectValidatorAdapter(
            IEnumerable<IMarkdownObjectValidator> validators, 
            Action<IMarkdownObject> preProcess, 
            Action<IMarkdownObject> postProcess)
        {
            Validators = validators.ToImmutableArray();
            _preProcess = preProcess;
            _postProcess = postProcess;
        }

        public MarkdownObjectValidatorAdapter(
            IMarkdownObjectValidator validator,
            Action<IMarkdownObject> preProcess,
            Action<IMarkdownObject> postProcess)
        {
            Validators = new[] { validator }.ToImmutableArray();
            _preProcess = preProcess;
            _postProcess = postProcess;
        }

        public IMarkdownObject Rewrite(IMarkdownObject markdownObject)
        {
            foreach (var validator in Validators)
            {
                validator.Validate(markdownObject);
            }

            return markdownObject;
        }

        public void PreProcess(IMarkdownObject markdownObject)
        {
            _preProcess?.Invoke(markdownObject);
            foreach (var validator in Validators)
            {
                validator.PreValidate(markdownObject);
            }
        }

        public void PostProcess(IMarkdownObject markdownObject)
        {
            foreach (var validator in Validators)
            {
                validator.PostValidate(markdownObject);
            }
            _postProcess?.Invoke(markdownObject);
        }
    }
}