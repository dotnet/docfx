namespace Docfx.Common.EntityMergers;

interface IMergeHandler
{
    void Merge(ref object source, object overrides, IMergeContext context);
}
