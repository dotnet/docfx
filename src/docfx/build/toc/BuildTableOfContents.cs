// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    internal static class BuildTableOfContents
    {
        public static List<Error> Build(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.TableOfContents);

            // load toc model
            var (errors, model, _, _) = context.TableOfContentsLoader.Load(file);

            // enable pdf
            var outputPath = context.DocumentProvider.GetOutputPath(file.FilePath, model.Metadata.Monikers);
            var monikerGroup = MonikerUtility.GetGroup(model.Metadata.Monikers);

            if (context.Config.OutputPdf)
            {
                model.Metadata.PdfAbsolutePath = "/" +
                    UrlUtility.Combine(context.Config.BasePath, "opbuildpdf", monikerGroup ?? string.Empty, LegacyUtility.ChangeExtension(file.SitePath, ".pdf"));
            }

            // TODO: Add experimental and experiment_id to publish item
            var publishItem = new PublishItem(
                file.SiteUrl,
                outputPath,
                file.FilePath.Path,
                context.LocalizationProvider.Locale,
                model.Metadata.Monikers,
                context.MonikerProvider.GetConfigMonikerRange(file.FilePath));

            if (context.PublishModelBuilder.TryAdd(file.FilePath, publishItem) && !context.Config.DryRun)
            {
                if (context.Config.Legacy)
                {
                    var output = context.TemplateEngine.RunJint("toc.json.js", JsonUtility.ToJObject(model));
                    context.Output.WriteJson(outputPath, output);
                    context.Output.WriteJson(LegacyUtility.ChangeExtension(outputPath, ".mta.json"), model.Metadata);
                }
                else
                {
                    context.Output.WriteJson(outputPath, model);
                }
            }

            return errors;
        }
    }
}
