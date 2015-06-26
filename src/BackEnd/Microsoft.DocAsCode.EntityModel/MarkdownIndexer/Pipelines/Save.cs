namespace Microsoft.DocAsCode.EntityModel.MarkdownIndexer
{
    using System.IO;

    using Microsoft.DocAsCode.Utility;

    public class Save : IIndexerPipeline
    {
        public ParseResult Run(MapFileItemViewModel item, IndexerContext context)
        {
            var filePath = context.MarkdownFileTargetPath;
            var markdownMapFileOutputFolder = context.MarkdownMapFileOutputFolder;
            string markdownMapFileName = Path.GetFileName(filePath) + Constants.MapFileExtension;
            string markdownFolder = Path.GetDirectoryName(filePath);

            // Use the same folder as api.yaml if the output folder is not set
            string markdownMapFileFolder = (string.IsNullOrEmpty(markdownMapFileOutputFolder) ? markdownFolder : markdownMapFileOutputFolder)
                                           ?? string.Empty;
            string markdownMapFileFullPath = Path.Combine(markdownMapFileFolder, markdownMapFileName);

            // Post-process item
            // if references'/overrides count is 0, set it to null
            if (item.References != null && item.References.Count == 0) item.References = null;
            if (item.CustomProperties != null && item.CustomProperties.Count == 0) item.CustomProperties = null;

            if (!item.HasUsefulInfo())
            {
                return new ParseResult(ResultLevel.Info, "Finish processing {0}, Map file will not be generated as no neccessory info is contained", context.MarkdownFileSourcePath);
            }

            MapFileViewModel mapFile = new MapFileViewModel();

            // For map file, always one key corresponding to the value
            mapFile.Add("default", item);
            JsonUtility.Serialize(markdownMapFileFullPath, mapFile);

            return new ParseResult(ResultLevel.Success, "Finish processing {0}, successfully generated {0}", context.MarkdownFileSourcePath, markdownMapFileFullPath);
        }
    }
}
