// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildTableOfContents
    {
        public static void Build(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.TableOfContents);

            // load toc tree
            var (errors, node, _, _) = context.TableOfContentsLoader.Load(file);

            context.ContentValidator.ValidateTocDeprecated(file);

            var (metadataErrors, metadata) = context.MetadataProvider.GetMetadata(file.FilePath);
            errors.AddRange(metadataErrors);

            errors.AddRange(context.MetadataValidator.ValidateMetadata(metadata.RawJObject, file.FilePath));

            var (validationErrors, tocMetadata) = JsonUtility.ToObject<TableOfContentsMetadata>(metadata.RawJObject);
            errors.AddRange(validationErrors);

            var model = new TableOfContentsModel(node.Items.Select(item => item.Value).ToArray(), tocMetadata, file.SitePath);

            // TODO: improve error message for toc monikers overlap
            var (monikerErrors, monikers) = context.MonikerProvider.GetFileLevelMonikers(file.FilePath);
            errors.AddRange(monikerErrors);

            var outputPath = context.DocumentProvider.GetOutputPath(file.FilePath);

            // enable pdf
            if (context.Config.OutputPdf)
            {
                model.Metadata.PdfAbsolutePath = "/" +
                    UrlUtility.Combine(
                        context.Config.BasePath, "opbuildpdf", monikers.MonikerGroup ?? "", LegacyUtility.ChangeExtension(file.SitePath, ".pdf"));
            }

            context.ErrorBuilder.AddRange(errors);

            if (!context.ErrorBuilder.FileHasError(file.FilePath) && !context.Config.DryRun)
            {
                if (context.Config.OutputType == OutputType.Html)
                {
                    // Just for current PDF build. toc.json is used for generate PDF outline
                    var output = context.TemplateEngine.RunJavaScript("toc.json.js", JsonUtility.ToJObject(model));
                    context.Output.WriteJson(Path.ChangeExtension(outputPath, ".json"), output);

                    var viewModel = context.TemplateEngine.RunJavaScript($"toc.html.js", JsonUtility.ToJObject(model));
                    var html = context.TemplateEngine.RunMustache($"toc.html.tmpl", viewModel, file.FilePath);
                    context.Output.WriteText(outputPath, html);
                }
                else
                {
                    var output = context.TemplateEngine.RunJavaScript("toc.json.js", JsonUtility.ToJObject(model));
                    context.Output.WriteJson(outputPath, output);
                }
            }

            context.PublishModelBuilder.SetPublishItem(file.FilePath, null, context.DocumentProvider.GetOutputPath(file.FilePath));
        }
    }
}
