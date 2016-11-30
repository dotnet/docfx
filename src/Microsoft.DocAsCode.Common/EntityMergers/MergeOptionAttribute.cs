// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.EntityMergers
{
    using System;

    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class MergeOptionAttribute : Attribute
    {
        public MergeOptionAttribute(MergeOption option = MergeOption.Merge)
        {
            Option = option;
        }

        /// <summary>
        /// Hint merger use custom merge handler.
        /// </summary>
        /// <param name="handlerType">the type of custom merge handler, it should implement <see cref="IMergeHandler"/>.</param>
        public MergeOptionAttribute(Type handlerType)
        {
            Option = MergeOption.Merge;
            if (handlerType == null)
            {
                throw new ArgumentNullException(nameof(handlerType));
            }
            Handler = (IMergeHandler)Activator.CreateInstance(handlerType);
        }

        public MergeOption Option { get; }

        public IMergeHandler Handler { get; }
    }
}
