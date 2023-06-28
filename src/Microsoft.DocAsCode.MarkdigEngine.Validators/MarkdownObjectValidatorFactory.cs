// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Syntax;

namespace Microsoft.DocAsCode.MarkdigEngine.Validators;

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
