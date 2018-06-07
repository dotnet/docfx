// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    internal class DependencyItem
    {
        public Document Document { get; private set; }

        public DependencyType Type { get; private set; }

        public DependencyItem(Document referencedDoc, DependencyType type)
        {
            Debug.Assert(referencedDoc != null);

            Document = referencedDoc;
            Type = type;
        }
    }
}
