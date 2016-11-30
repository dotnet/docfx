namespace Microsoft.DocAsCode.Common.EntityMergers
{
    public interface IMergeContext
    {
        IMerger Merger { get; }
        object this[string key] { get; }
    }
}
