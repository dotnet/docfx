// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    public class PostProcessorInfo : ProcessorInfoBase
    {
        public string ContextInfoFile { get; set; }

        public override bool Equals(object obj)
        {
            var another = obj as PostProcessorInfo;
            if (another == null)
            {
                return false;
            }

            return Name == another.Name && IncrementalContextHash == another.IncrementalContextHash;
        }

        public override int GetHashCode()
        {
            return Name?.GetHashCode() ?? 0;
        }
    }
}
