// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.MarkdownLite;

    public class AzureMigrationIncludeBlockToken : AzureMigrationIncludeBasicToken
    {
        public AzureMigrationIncludeBlockToken(IMarkdownRule rule, IMarkdownContext context, string name, string src, string title, SourceInfo sourceInfo)
            : base(rule, context, name, src, title, sourceInfo)
        {
        }
    }
}
