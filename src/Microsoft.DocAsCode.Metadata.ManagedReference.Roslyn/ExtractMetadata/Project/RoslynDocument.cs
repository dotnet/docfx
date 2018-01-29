using Microsoft.CodeAnalysis;


namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    public class RoslynDocument : AbstractDocument
    {
        Document _document;

        public RoslynDocument(Document document)
        {
            _document = document;
        }

        public override string FilePath => _document.FilePath;
    }
}
