// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Syntax;

namespace Docfx.MarkdigEngine.Validators;

public interface IMarkdownObjectValidator
{
    void PreValidate(IMarkdownObject markdownObject);

    void Validate(IMarkdownObject markdownObject);

    void PostValidate(IMarkdownObject markdownObject);
}
