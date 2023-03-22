// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Syntax;

namespace Microsoft.DocAsCode.Build.OverwriteDocuments;

[Serializable]
public class MarkdownPropertyModel
{
    public string PropertyName { get; set; }

    public Block PropertyNameSource { get; set; }

    public List<Block> PropertyValue { get; set; }
}
