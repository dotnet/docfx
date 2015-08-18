namespace Microsoft.DocAsCode.EntityModel.YamlConverters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class YamlLoader
        : IPipelineItem<FileCollection, object, ConverterModel>
    {
        public ConverterModel Exec(FileCollection arg, object context)
        {
            var result = new ConverterModel(arg.BaseDir);
            foreach (var item in CreateDispatcher()
                .Dispatch(arg.EnumerateFiles())
                .Where(x => x != null))
            {
                result.Add(item.FileAndType, item);
            }
            return result;
        }

        private static Dispatcher<FileAndType, FileModel> CreateDispatcher()
        {
            var dispatcher = new Dispatcher<FileAndType, FileModel>();
            var yaml = from item in dispatcher
                       where ".yml".Equals(Path.GetExtension(item.File), StringComparison.OrdinalIgnoreCase) ||
                             ".yaml".Equals(Path.GetExtension(item.File), StringComparison.OrdinalIgnoreCase)
                       select item;
            // read yaml files in api document.
            dispatcher = from item in yaml
                         where item.Type == DocumentType.ApiDocument
                         select ReadYamlFile(item);
            // ignore other yaml files.
            dispatcher = from item in yaml
                         select null;
            var markdown = from item in dispatcher
                           where ".md".Equals(Path.GetExtension(item.File), StringComparison.OrdinalIgnoreCase) ||
                                 ".markdown".Equals(Path.GetExtension(item.File), StringComparison.OrdinalIgnoreCase)
                           select item;
            // read markdown files in override document.
            dispatcher = from item in markdown
                         where item.Type == DocumentType.OverrideDocument
                         select new FileModel(item, MarkdownReader.ReadMarkdownAsOverride(item.BaseDir, item.File));
            // read markdown files in conceptual document.
            dispatcher = from item in markdown
                         where item.Type == DocumentType.ConceptualDocument
                         select new FileModel(item, MarkdownReader.ReadMarkdownAsConceptual(item.BaseDir, item.File));
            // ignore other markdown files.
            dispatcher = from item in markdown
                         select null;
            // for all other extension files, treat them as resources.
            dispatcher = from item in dispatcher
                         where true
                         select new FileModel(item.ChangeType(DocumentType.Resource), null);
            return dispatcher;
        }

        private static FileModel ReadYamlFile(FileAndType item)
        {
            try
            {
                return new FileModel(item, YamlUtility.Deserialize<Dictionary<object, object>>(Path.Combine(item.BaseDir, item.File)));
            }
            catch (Exception ex)
            {
                // todo : log
                Console.WriteLine($"Unable to read file({item.File}): {ex.ToString()}");
                return null;
            }
        }
    }
}
