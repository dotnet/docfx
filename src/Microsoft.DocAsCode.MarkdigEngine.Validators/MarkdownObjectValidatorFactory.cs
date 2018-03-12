// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Validators
{
    using System;

    using Markdig.Syntax;

    public static class MarkdownObjectValidatorFactory
    {
        public static IMarkdownObjectValidator FromLambda<TObject>(
            Action<TObject> validator,
            Action<IMarkdownObject> preAction = null,
            Action<IMarkdownObject> postAction = null)
                where TObject : class, IMarkdownObject
        {
            return new MarkdownLambdaObjectValidator<TObject>(validator, preAction, postAction);
        }
    }
}
