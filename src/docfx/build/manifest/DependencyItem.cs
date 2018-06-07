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

        public override int GetHashCode()
        {
            return Document.GetHashCode() + Type.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DependencyItem);
        }

        public bool Equasl(DependencyItem other)
        {
            if (other == null)
            {
                return false;
            }

            return Document.Equals(other.Document) && Type == other.Type;
        }
    }
}
