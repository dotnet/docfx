// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    internal class DependencyItem
    {
        public Document From { get; private set; }

        public Document To { get; private set; }

        public DependencyType Type { get; private set; }

        public DependencyItem(Document from, Document to, DependencyType type)
        {
            Debug.Assert(from != null);
            Debug.Assert(to != null);

            From = from;
            To = to;
            Type = type;
        }

        public override int GetHashCode()
        {
            return To.GetHashCode() + From.GetHashCode() + Type.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DependencyItem);
        }

        public bool Equals(DependencyItem other)
        {
            if (other is null)
            {
                return false;
            }

            return From.Equals(other.From) && To.Equals(other.To) && Type == other.Type;
        }
    }
}
