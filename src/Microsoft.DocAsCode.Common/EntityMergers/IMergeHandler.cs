namespace Microsoft.DocAsCode.Utility.EntityMergers
{
    public interface IMergeHandler
    {
        void Merge(ref object source, object overrides, IMergeContext context);
    }
}
