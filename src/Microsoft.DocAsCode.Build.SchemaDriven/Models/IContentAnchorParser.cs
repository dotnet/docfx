// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven
{
    public interface IContentAnchorParser
    {
        string Parse(string input);

        bool ContainsAnchor { get; }

        string Content { get; }
    }
}
