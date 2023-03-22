// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Syntax;

namespace Microsoft.DocAsCode.Build.OverwriteDocuments;

[Serializable]
public class MarkdownFragmentModel
{
    public string Uid { get; set; }

    public Block UidSource { get; set; }

    public string YamlCodeBlock { get; set; }

    public Block YamlCodeBlockSource { get; set; }

    public List<MarkdownPropertyModel> Contents { get; set; }
}
