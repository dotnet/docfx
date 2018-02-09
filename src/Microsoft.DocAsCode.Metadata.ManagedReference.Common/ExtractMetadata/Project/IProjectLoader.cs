
namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    public interface IProjectLoader
    {
        AbstractProject TryLoad(string path, AbstractProjectLoader loader);
    }
}
