namespace Microsoft.DocAsCode.Utility.EntityMergers
{
    using System;

    public interface IMerger
    {
        void Merge(ref object source, object overrides, Type type, IMergeContext context);
        bool TestKey(object source, object overrides, Type type, IMergeContext context);
    }
}
