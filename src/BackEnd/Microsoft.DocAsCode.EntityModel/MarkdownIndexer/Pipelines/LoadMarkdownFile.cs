namespace Microsoft.DocAsCode.EntityModel.MarkdownIndexer
{
    using System;
    using System.IO;

    using Microsoft.DocAsCode.Utility;
    using System.Linq;

    public class LoadMarkdownFile : IIndexerPipeline
    {
        public ParseResult Run(MapFileItemViewModel item, IndexerContext context)
        {
            if (string.IsNullOrEmpty(context.MarkdownFileSourcePath))
            {
                return new ParseResult(ResultLevel.Error, "Markdown file source path should be specified!");
            }

            if (string.IsNullOrEmpty(context.CurrentWorkingDirectory)) context.CurrentWorkingDirectory = Environment.CurrentDirectory;

            var targetFiles = new string[] { context.MarkdownFileSourcePath }.CopyFilesToFolder(context.CurrentWorkingDirectory, context.TargetFolder, true, s => ParseResult.WriteToConsole(ResultLevel.Info, s), s => { ParseResult.WriteToConsole(ResultLevel.Warning, "Unable to copy file: {0}, ignored.", s); return true; });
            var targetFile = targetFiles?.FirstOrDefault() ?? context.MarkdownFileSourcePath;

            context.MarkdownContent = File.ReadAllText(targetFile);
            context.MarkdownFileTargetPath = targetFile;

            if (!string.IsNullOrEmpty(context.MarkdownFileSourcePath))
            {
                item.Remote = GitUtility.GetGitDetail(context.MarkdownFileSourcePath);
            }

            return new ParseResult(ResultLevel.Success);
        }
    }
}
