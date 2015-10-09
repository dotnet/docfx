// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Utility.EntityMergers
{
    using System;

    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class MergeOptionAttribute : Attribute
    {
        public MergeOptionAttribute(MergeOption option)
        {
            Option = option;
        }

        public MergeOption Option { get; }
    }
}
