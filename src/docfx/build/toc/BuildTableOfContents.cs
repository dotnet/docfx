// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildTableOfContents
    {
        public static List<Error> Build(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.TableOfContents);

            // load toc tree
            var (errors, node, _, _) = context.TableOfContentsLoader.Load(file);

            var (metadataErrors, metadata) = context.MetadataProvider.GetMetadata(file.FilePath);
            errors.AddRange(metadataErrors);

            var (validationErrors, tocMetadata) = JsonUtility.ToObject<TableOfContentsMetadata>(metadata.RawJObject);
            errors.AddRange(validationErrors);

            var model = new TableOfContentsModel(node.Items.Select(item => item.Value).ToArray(), tocMetadata, file.SitePath);

            // TODO: improve error message for toc monikers overlap
            var (monikerErrors, monikers) = context.MonikerProvider.GetFileLevelMonikers(file.FilePath);
            errors.AddRange(monikerErrors);

            var outputPath = context.DocumentProvider.GetOutputPath(file.FilePath);
            var monikerGroup = MonikerUtility.GetGroup(monikers);

            // enable pdf
            if (context.Config.OutputPdf)
            {
                model.Metadata.PdfAbsolutePath = "/" +
                    UrlUtility.Combine(context.Config.BasePath, "opbuildpdf", monikerGroup ?? string.Empty, LegacyUtility.ChangeExtension(file.SitePath, ".pdf"));
            }

            // TODO: Add experimental and experiment_id to publish item
            var publishItem = new PublishItem(
                file.SiteUrl,
                outputPath,
                context.SourceMap.GetOriginalFilePath(file.FilePath) ?? file.FilePath.Path,
                context.BuildOptions.Locale,
                monikers,
                context.MonikerProvider.GetConfigMonikerRange(file.FilePath),
                file.ContentType,
                file.Mime.Value);

            context.PublishModelBuilder.Add(file.FilePath, publishItem, () =>
            {
                if (!context.Config.DryRun)
                {
                    if (context.Config.OutputType == OutputType.Html)
                    {
                        // Just for current PDF build. toc.json is used for generate PDF outline
                        var output = context.TemplateEngine.RunJavaScript("toc.json.js", JsonUtility.ToJObject(model));
                        context.Output.WriteJson(LegacyUtility.ChangeExtension(outputPath, ".json"), output);

                        var viewModel = context.TemplateEngine.RunJavaScript($"toc.html.js", JsonUtility.ToJObject(model));
                        var html = context.TemplateEngine.RunMustache($"toc.html.tmpl", viewModel);
                        context.Output.WriteText(outputPath, html);
                    }
                    else if (context.Config.Legacy)
                    {
                        var output = context.TemplateEngine.RunJavaScript("toc.json.js", JsonUtility.ToJObject(model));
                        context.Output.WriteJson(LegacyUtility.ChangeExtension(outputPath, ".json"), output);
                        context.Output.WriteJson(LegacyUtility.ChangeExtension(outputPath, ".mta.json"), model.Metadata);
                    }
                    else
                    {
                        context.Output.WriteJson(outputPath, model);
                    }
                }
            });

            return errors;
        }
    }
}
