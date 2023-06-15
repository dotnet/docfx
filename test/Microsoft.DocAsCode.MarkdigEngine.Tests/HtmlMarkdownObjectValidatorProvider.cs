// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;

using Markdig.Syntax;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.MarkdigEngine.Validators;

namespace Microsoft.DocAsCode.MarkdigEngine.Tests;

[Export(ContractName, typeof(IMarkdownObjectValidatorProvider))]
public class HtmlMarkdownObjectValidatorProvider : IMarkdownObjectValidatorProvider
{
    public const string ContractName = "Html";

    public const string WarningMessage = "Html Tag!";

    ImmutableArray<IMarkdownObjectValidator> IMarkdownObjectValidatorProvider.GetValidators()
    {
        return ImmutableArray.Create(
            MarkdownObjectValidatorFactory.FromLambda<HtmlBlock>(
                block => Logger.LogWarning(WarningMessage)));
    }
}
