// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmParser : MarkdownParser
    {
        public ImmutableStack<string> FilePathStack => (ImmutableStack<string>)Context.Variables[DfmEngine.FilePathStackKey];

        public DfmParser(IMarkdownContext context, Options options, Dictionary<string, LinkObj> links)
            : base(context, options, links)
        {
        }
    }
}
