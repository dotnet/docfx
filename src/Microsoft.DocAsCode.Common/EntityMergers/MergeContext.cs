namespace Microsoft.DocAsCode.Common.EntityMergers
{
    using System.Collections.Generic;

    internal sealed class MergeContext : IMergeContext
    {
        private readonly IReadOnlyDictionary<string, object> Data;

        public MergeContext(IMerger merger, IReadOnlyDictionary<string, object> data)
        {
            Merger = merger;
            Data = data;
        }

        public IMerger Merger { get; }

        public object this[string key]
        {
            get
            {
                if (Data == null)
                {
                    return null;
                }
                Data.TryGetValue(key, out object result);
                return result;
            }
        }
    }
}
