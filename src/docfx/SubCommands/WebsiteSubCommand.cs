// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.EntityModel.MarkdownIndexer;
    using Microsoft.DocAsCode.Utility;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    class WebsiteSubCommand : ISubCommand
    {
        public ParseResult Exec(Options options)
        {
            // 1. Conceptual files, follow grunt:copy parameters
            // 2. Website template

            var websiteVerb = options.WebsiteVerb;
            var forceRebuild = websiteVerb.ForceRebuild;
            var result = ConfigModelHelper.GetConfigModel(websiteVerb);

            var configModel = result.Item2;
            if (configModel == null) return result.Item1;


            var inputModel = MetadataSubCommand.ConvertToInputModel(configModel);

            var worker = new ExtractMetadataWorker(inputModel, forceRebuild);
            var extractMetadataResult = worker.ExtractMetadataAsync().Result;

            extractMetadataResult.WriteToConsole();
            if (extractMetadataResult.ResultLevel == ResultLevel.Error) return extractMetadataResult;

            // 2. Process Conceputal files
            // TODO: Issue1: There could be multiple index.yml files per folder => Use all index.yml? What if markdown file wants specific index.yml? apply the same data structure of index.yml to external references?
            // TODO: Issue2: Should Conceputal files keep relative path? consider Grunt:copy?
            // Current: keep relative path to current folder, and resolve api to all available index.yml
            // STEP1. copy mdFiles to outputFolder, so that the link path is correct
            // TODO: CONFIRM THE BEHAVIOR: BY DEFAULT USE "**/*.md"?
            var outputFolder = configModel.OutputFolder;
            var templateFolder = configModel.TemplateFolder;
            var mdFiles = GlobUtility.ExpandFileMapping(configModel.BaseDirectory, configModel.Conceptuals, s =>
            {
                // If name is empty, use current outputFolder
                string key = string.IsNullOrWhiteSpace(s) ? string.Empty : s.ToValidFilePath();
                return Path.Combine(outputFolder, key).ToNormalizedPath();
            });
            if (mdFiles != null)
            {
                var apiIndices = inputModel.Items.Select(s => Constants.GetIndexFilePathFunc(s.Key)).ToArray();
                var referenceOutputFolder = string.IsNullOrEmpty(outputFolder) ? Constants.WebsiteReferenceFolderName : Path.Combine(outputFolder, Constants.WebsiteReferenceFolderName);
                foreach (var mdFile in mdFiles.Items)
                {
                    var targetFolder = mdFile.Name;
                    var files = mdFile.Files;

                    foreach (var file in files)
                    {
                        IndexerContext context = new IndexerContext
                        {
                            ApiIndexFiles = apiIndices,
                            MarkdownFileSourcePath = file,
                            CurrentWorkingDirectory = mdFile.CurrentWorkingDirectory,
                            TargetFolder = targetFolder,
                            ReferenceOutputFolder = referenceOutputFolder,
                        };
                        var indexerResult = MarkdownIndexer.Exec(context);
                        indexerResult.WriteToConsole();
                    }
                }
            }

            // 3. Copy website template files
            var toc = TemplateManager.GenerateDefaultToc(inputModel.Items.Where(s => s.Value != null && s.Value.Any()).Select(s => s.Key), mdFiles?.Items.Where(s => s.Files != null && s.Files.Any()).Select(s => s.Name), outputFolder);

            // typeof(Program).Assembly is not available in DNX Core 5.0
            var assembly = typeof(Program).GetTypeInfo().Assembly;

            // TODO: Pass in theme name
            TemplateManager.CopyToOutput(configModel.BaseDirectory, "Template", assembly, templateFolder, outputFolder, toc, configModel.TemplateTheme);

            return ParseResult.SuccessResult;
        }
    }
}
