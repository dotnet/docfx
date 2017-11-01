// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;

    public interface ICodeSnippetExtractor
    {
        Dictionary<string, DfmTagNameResolveResult> GetAll(string[] lines);
    }
}