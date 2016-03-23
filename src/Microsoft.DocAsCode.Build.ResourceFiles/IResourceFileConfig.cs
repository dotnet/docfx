namespace Microsoft.DocAsCode.Build.ResourceFiles
{
    public interface IResourceFileConfig
    {
        bool IsResourceFile(string fileExtension);
    }
}
