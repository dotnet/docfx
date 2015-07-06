namespace Microsoft.DocAsCode
{
    using Microsoft.DocAsCode.EntityModel;
    using System;
    using System.IO;
    using System.Linq;

    class BuildExternalReferenceSubCommand : ISubCommand
    {
        public ParseResult Exec(Options options)
        {
            // 1. build metadata
            var result = ConfigModelHelper.GetConfigModel(options.ExternalVerb);
            var configModel = result.Item2;
            if (configModel == null)
            {
                return result.Item1;
            }

            var inputModel = MetadataSubCommand.ConvertToInputModel(configModel);

            var worker = new ExtractMetadataWorker(inputModel, options.ForceRebuild);
            var extractMetadataResult = worker.ExtractMetadataAsync().Result;

            extractMetadataResult.WriteToConsole();
            if (extractMetadataResult.ResultLevel == ResultLevel.Error)
            {
                return extractMetadataResult;
            }

            // 2. convert.
            var outputFile = Path.Combine(options.ExternalVerb.OutputFolder ?? Environment.CurrentDirectory, options.ExternalVerb.Name ?? "externalreference.rpk");
            var package = new ExternalReferencePackage(outputFile, new Uri(options.ExternalVerb.BaseUrl));
            try
            {
                package.CreatePackage(inputModel.Items.Keys.ToList());
                return ParseResult.SuccessResult;
            }
            catch (Exception ex)
            {
                return new ParseResult(ResultLevel.Error, ex.ToString());
            }
        }
    }
}
