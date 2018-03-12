// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System;
    using System.Collections.Generic;

    using Markdig.Syntax;
    using Microsoft.DocAsCode.MarkdigEngine.Validators;

    public static class MarkdownObjectRewriterFactory
    {
        public static IMarkdownObjectRewriter FromValidators(
            IEnumerable<IMarkdownObjectValidator> validators,
            Action<IMarkdownObject> preProcess = null,
            Action<IMarkdownObject> postProcess = null)
        {
            if (validators == null)
            {
                throw new ArgumentNullException(nameof(validators));
            }

            return new MarkdownObjectValidatorAdapter(validators, preProcess, postProcess);
        }

        public static IMarkdownObjectRewriter FromValidator(
            IMarkdownObjectValidator validator,
            Action<IMarkdownObject> preProcess = null,
            Action<IMarkdownObject> postProcess = null)
        {
            if (validator == null)
            {
                throw new ArgumentNullException(nameof(validator));
            }

            return new MarkdownObjectValidatorAdapter(validator, preProcess, postProcess);
        }
    }
}
