namespace Docfx.Common.EntityMergers;

public interface IMergeHandler
{
    void Merge(ref object source, object overrides, IMergeContext context);
}
