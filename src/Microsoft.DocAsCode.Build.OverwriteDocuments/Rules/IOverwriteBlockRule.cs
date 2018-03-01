// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.OverwriteDocuments
{
    using Markdig.Syntax;

    public interface IOverwriteBlockRule
    {
        string TokenName{ get; }

        bool Parse(Block block, out string value);
    }
}
