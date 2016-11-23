namespace Microsoft.DocAsCode.Utility.EntityMergers
{
    public interface IMergeContext
    {
        IMerger Merger { get; }
        object this[string key] { get; }
    }
}
