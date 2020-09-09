// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildTableOfContents
    {
        public static void Build(Context context, FilePath file)
        {
            var errors = context.ErrorBuilder;

            // load toc tree
            var (node, _, _, _) = context.TableOfContentsLoader.Load(file);

            context.ContentValidator.ValidateTocDeprecated(file);

            var metadata = context.MetadataProvider.GetMetadata(errors, file);
            context.MetadataValidator.ValidateMetadata(errors, metadata.RawJObject, file);

            var tocMetadata = JsonUtility.ToObject<TableOfContentsMetadata>(errors, metadata.RawJObject);

            var path = context.DocumentProvider.GetSitePath(file);

            var model = new TableOfContentsModel(node.Items.Select(item => item.Value).ToArray(), tocMetadata, path);

            var outputPath = context.DocumentProvider.GetOutputPath(file);

            // enable pdf
            if (context.Config.OutputPdf)
            {
                var monikers = context.MonikerProvider.GetFileLevelMonikers(errors, file);
                model.Metadata.PdfAbsolutePath = "/" +
                    UrlUtility.Combine(
                        context.Config.BasePath, "opbuildpdf", monikers.MonikerGroup ?? "", LegacyUtility.ChangeExtension(path, ".pdf"));
            }

            if (!context.ErrorBuilder.FileHasError(file) && !context.Config.DryRun)
            {
                if (context.Config.OutputType == OutputType.Html)
                {
                    var viewModel = context.TemplateEngine.RunJavaScript($"toc.html.js", JsonUtility.ToJObject(model));
                    var html = context.TemplateEngine.RunMustache($"toc.html", viewModel, file);
                    context.Output.WriteText(outputPath, html);

                    // Just for current PDF build. toc.json is used for generate PDF outline
                    var output = context.TemplateEngine.RunJavaScript("toc.json.js", JsonUtility.ToJObject(model));
                    context.Output.WriteJson(Path.ChangeExtension(outputPath, ".json"), output);
                }
                else
                {
                    var output = context.TemplateEngine.RunJavaScript("toc.json.js", JsonUtility.ToJObject(model));
                    context.Output.WriteJson(outputPath, output);
                }
            }

            context.PublishModelBuilder.SetPublishItem(file, metadata: null, outputPath);
        }
    }
}
