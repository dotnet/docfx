// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;

#nullable enable

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
            var publishItem = new PublishItem
            {
                Url = file.SiteUrl,
                Path = outputPath,
                SourcePath = file.FilePath.Path,
                Locale = context.LocalizationProvider.Locale,
                Monikers = model.Metadata.Monikers,
                MonikerGroup = monikerGroup,
                ConfigMonikerRange = context.MonikerProvider.GetConfigMonikerRange(file.FilePath),
            };

            if (context.PublishModelBuilder.TryAdd(file, publishItem) && !context.Config.DryRun)
            {
                if (context.Config.Legacy)
                {
                    var output = context.TemplateEngine.RunJint("toc.json.js", JsonUtility.ToJObject(model));
                    context.Output.WriteJson(output, outputPath);
                    context.Output.WriteJson(model.Metadata, LegacyUtility.ChangeExtension(outputPath, ".mta.json"));
                }
                else
                {
                    context.Output.WriteJson(model, outputPath);
                }
            }

            return errors;
        }
    }
}
