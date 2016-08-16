// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Incrementals
{
    public sealed class ProcessorStepInfo
    {
        /// <summary>
        /// The name of processor step.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The context hash for incremental.
        /// </summary>
        public string IncrementalContextHash { get; set; }

        public override bool Equals(object obj)
        {
            var another = obj as ProcessorStepInfo;
            if (another == null)
            {
                return false;
            }
            return Name == another.Name &&
                IncrementalContextHash == another.IncrementalContextHash;
        }

        public override int GetHashCode()
        {
            return Name?.GetHashCode() ?? 0;
        }
    }
}
