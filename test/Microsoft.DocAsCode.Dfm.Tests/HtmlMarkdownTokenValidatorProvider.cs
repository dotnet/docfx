// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.Tests
{
    using System.Collections.Immutable;
    using System.Composition;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;

    [Export(ContractName, typeof(IMarkdownTokenValidatorProvider))]
    public class HtmlMarkdownTokenValidatorProvider : IMarkdownTokenValidatorProvider
    {
        public const string ContractName = "Html";

        public const string WarningMessage = "Html Tag!";

        public ImmutableArray<IMarkdownTokenValidator> GetValidators()
        {
            return ImmutableArray.Create(
                MarkdownTokenValidatorFactory.FromLambda<MarkdownHtmlBlockToken>(
                    token => Logger.LogWarning(WarningMessage)));
        }
    }
}
