// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    public sealed class TemplateResourceInfo
    {
        public string ResourceKey { get; }
        public TemplateResourceInfo(string resourceKey)
        {
            ResourceKey = resourceKey;
        }

        public override bool Equals(object obj)
        {
            var target = obj as TemplateResourceInfo;
            if (target == null)
            {
                return false;
            }

            if (ReferenceEquals(this, target))
            {
                return true;
            }

            return Equals(ResourceKey, target.ResourceKey);
        }

        public override int GetHashCode()
        {
            return ResourceKey.GetHashCode();
        }
    }
}
