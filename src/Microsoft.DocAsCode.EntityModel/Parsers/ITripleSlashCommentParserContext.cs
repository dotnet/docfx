// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;

    public interface ITripleSlashCommentParserContext
    {
        bool PreserveRawInlineComments { get; set; }
        Action<string> AddReferenceDelegate { get; set; }
        SourceDetail Source { get; set; }
    }
}
