// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Validators
{
    using System;
    using System.Collections.Immutable;
    
    using Markdig.Syntax;

    public static class MarkdownDocumentValidationFactory
    {
        public static IMarkdownDocumentValidator FromLambda(Action<MarkdownDocument> validator)
        {
            return new MarkdownDocumentLambdaValidator(validator);
        }
    }
}
