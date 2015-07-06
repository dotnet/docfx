namespace Microsoft.DocAsCode
{
    using Microsoft.DocAsCode.EntityModel;
    using System;
    using System.IO;
    using System.Linq;

    class ExportSubCommand : ISubCommand
    {
        public ParseResult Exec(Options options)
        {
            // 1. build metadata
            var result = ConfigModelHelper.GetConfigModel(options.ExportVerb);
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
            if (inputModel.Items == null)
            {
                return new ParseResult(ResultLevel.Error, "Cannot find project.");
            }
            var outputFile = Path.Combine(options.ExportVerb.OutputFolder ?? Environment.CurrentDirectory, options.ExportVerb.Name ?? "externalreference.rpk");
            if (string.IsNullOrWhiteSpace(options.ExportVerb.BaseUrl))
            {
                return new ParseResult(ResultLevel.Error, "BaseUrl cannot be empty.");
            }
            try
            {
                var baseUri = new Uri(options.ExportVerb.BaseUrl);
                if (!baseUri.IsAbsoluteUri)
                {
                    return new ParseResult(ResultLevel.Error, "BaseUrl should be absolute url.");
                }
                using (var package = ExternalReferencePackage.Create(outputFile, baseUri))
                {
                    package.AddProjects(inputModel.Items.Keys.ToList());
                }
                return ParseResult.SuccessResult;
            }
            catch (Exception ex)
            {
                return new ParseResult(ResultLevel.Error, ex.ToString());
            }
        }
    }
}
