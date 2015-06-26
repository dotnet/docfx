namespace docfx
{
    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.Utility;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    class MetadataSubCommand : ISubCommand
    {
        public ParseResult Exec(Options options)
        {
            var metadataVerb = options.MetadataVerb;
            var forceRebuild = metadataVerb.ForceRebuild;

            var result = ConfigModelHelper.GetConfigModel(metadataVerb);
            var configModel = result.Item2;
            if (configModel == null) return result.Item1;

            var inputModel = ConvertToInputModel(configModel);

            var worker = new ExtractMetadataWorker(inputModel, forceRebuild);
            return worker.ExtractMetadataAsync().Result;
        }

        internal static ExtractMetadataInputModel ConvertToInputModel(ConfigModel configModel)
        {
            var projects = configModel.Projects;
            var outputFolder = configModel.OutputFolder;
            var inputModel = new ExtractMetadataInputModel();

            var expandedFileMapping = GlobUtility.ExpandFileMapping(configModel.BaseDirectory, configModel.Projects, s =>
            {
                string key = string.IsNullOrWhiteSpace(s) ? Constants.DefaultMetadataOutputFolderName : s.ToValidFilePath();
                return Path.Combine(outputFolder, key).ToNormalizedPath();
            });
            inputModel.Items = new Dictionary<string, List<string>>();

            foreach (var item in expandedFileMapping.Items)
            {
                List<string> existedItems;
                if (inputModel.Items.TryGetValue(item.Name, out existedItems))
                {
                    existedItems.AddRange(item.Files);
                }
                else
                {
                    inputModel.Items.Add(item.Name, item.Files);
                }
            }

            return inputModel;
        }
    }
}
