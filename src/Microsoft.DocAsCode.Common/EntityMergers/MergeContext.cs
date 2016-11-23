namespace Microsoft.DocAsCode.Utility.EntityMergers
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
                object result;
                Data.TryGetValue(key, out result);
                return result;
            }
        }
    }
}
