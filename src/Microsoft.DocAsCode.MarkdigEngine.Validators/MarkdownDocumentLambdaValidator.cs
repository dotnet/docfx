// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Validators
{
    using System;

    using Markdig.Syntax;

    internal class MarkdownDocumentLambdaValidator : IMarkdownDocumentValidator
    {
        private Action<MarkdownDocument> _validator;

        public MarkdownDocumentLambdaValidator(Action<MarkdownDocument> validator)
        {
            _validator = validator;
        }

        public void Validate(MarkdownDocument document)
        {
            _validator?.Invoke(document);
        }
    }
}