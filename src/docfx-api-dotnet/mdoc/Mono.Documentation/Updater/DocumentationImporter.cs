namespace Mono.Documentation.Updater
{
    public abstract class DocumentationImporter
    {

        public abstract void ImportDocumentation (DocsNodeInfo info);

        public abstract bool CheckRemoveByMapping(DocsNodeInfo info, string xmlChildName);
    }
}