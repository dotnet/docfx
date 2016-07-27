// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Generic;

    public interface IMarkdownExpression : IMarkdownToken
    {
        IEnumerable<IMarkdownToken> GetChildren();
    }
}
