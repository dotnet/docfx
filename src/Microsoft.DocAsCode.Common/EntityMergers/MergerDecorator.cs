// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.EntityMergers
{
    using System;

    public abstract class MergerDecorator : IMerger
    {
        private readonly IMerger _inner;

        protected MergerDecorator(IMerger inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public virtual void Merge(ref object source, object overrides, Type type, IMergeContext context)
        {
            _inner.Merge(ref source, overrides, type, context);
        }

        public virtual bool TestKey(object source, object overrides, Type type, IMergeContext context)
        {
            return _inner.TestKey(source, overrides, type, context);
        }
    }
}
