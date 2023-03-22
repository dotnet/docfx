// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Syntax;

namespace Microsoft.DocAsCode.Build.OverwriteDocuments;

public interface IOverwriteBlockRule
{
    string TokenName{ get; }

    bool Parse(Block block, out string value);
}
