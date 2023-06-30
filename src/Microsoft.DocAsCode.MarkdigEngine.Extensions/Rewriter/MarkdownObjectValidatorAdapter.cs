// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

using Markdig.Syntax;
using Microsoft.DocAsCode.MarkdigEngine.Validators;

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions;

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
