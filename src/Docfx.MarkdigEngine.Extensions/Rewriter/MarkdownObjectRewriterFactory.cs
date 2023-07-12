// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Syntax;
using Docfx.MarkdigEngine.Validators;

namespace Docfx.MarkdigEngine.Extensions;

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
