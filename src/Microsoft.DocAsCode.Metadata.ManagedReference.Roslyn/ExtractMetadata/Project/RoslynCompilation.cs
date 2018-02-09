using Microsoft.CodeAnalysis;


namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    public class RoslynCompilation : AbstractCompilation
    {
        Compilation _compilation;

        public RoslynCompilation(Compilation compilation)
        {
            _compilation = compilation;
        }

        public Compilation Compilation => _compilation;

        public override IBuildController GetBuildController()
        {
            return new RoslynSourceFileBuildController(this.Compilation);
        }
    }
}
