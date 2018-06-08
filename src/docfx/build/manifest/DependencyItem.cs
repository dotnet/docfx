// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    internal class DependencyItem
    {
        public Document Source { get; private set; }

        public Document Dest { get; private set; }

        public DependencyType Type { get; private set; }

        public DependencyItem(Document source, Document dest, DependencyType type)
        {
            Debug.Assert(source != null);
            Debug.Assert(dest != null);

            Source = source;
            Dest = dest;
            Type = type;
        }

        public override int GetHashCode()
        {
            return Dest.GetHashCode() + Source.GetHashCode() + Type.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DependencyItem);
        }

        public bool Equals(DependencyItem other)
        {
            if (other == null)
            {
                return false;
            }

            return Source.Equals(other.Source) && Dest.Equals(other.Dest) && Type == other.Type;
        }
    }
}
