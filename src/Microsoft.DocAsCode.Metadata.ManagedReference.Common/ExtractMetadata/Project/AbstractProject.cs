using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    public abstract class AbstractProject
    {
        public abstract string FilePath { get; }
        public abstract bool HasDocuments { get; }
        public abstract IEnumerable<AbstractDocument> Documents { get; }
        public abstract IEnumerable<string> PortableExecutableMetadataReferences { get; }
        public abstract IEnumerable<AbstractProject> ProjectReferences { get; }
        public abstract Task<AbstractCompilation> GetCompilationAsync();
    }
}
