// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.DataContracts.Common;

namespace Microsoft.DocAsCode.Dotnet;

internal class XmlCommentParserContext
{
    public Action<string, string> AddReferenceDelegate { get; set; }

    public Func<string, CRefTarget> ResolveCRef { get; set; }

    public SourceDetail Source { get; set; }

    public string CodeSourceBasePath { get; set; }
}
