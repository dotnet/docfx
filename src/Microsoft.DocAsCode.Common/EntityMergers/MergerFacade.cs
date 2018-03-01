// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.EntityMergers
{
    using System.Collections.Generic;

    public class MergerFacade
    {
        private readonly IMerger _merger;

        public MergerFacade(IMerger merger)
        {
            _merger = merger;
        }

        public void Merge<T>(ref T source, T overrides, IReadOnlyDictionary<string, object> data = null) where T : class
        {
            object s = source;
            var context = new MergeContext(_merger, data);
            context.Merger.Merge(ref s, overrides, typeof(T), context);
            source = (T)s;
        }
    }
}
