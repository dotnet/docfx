// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class BuildTableOfContents
    {
        public static List<Error> Build(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.TableOfContents);

            // load toc model
            var (errors, model, _, _) = context.Cache.LoadTocModel(context, file);

            // enable pdf
            var outputPath = file.GetOutputPath(model.Metadata.Monikers, file.Docset.SiteBasePath);

            if (file.Docset.Config.Output.Pdf)
            {
                var siteBasePath = file.Docset.SiteBasePath;
                var relativePath = PathUtility.NormalizeFile(Path.GetRelativePath(siteBasePath, LegacyUtility.ChangeExtension(outputPath, ".pdf")));
                model.Metadata.PdfAbsolutePath = $"/{siteBasePath}/opbuildpdf/{relativePath}";
            }

            // TODO: Add experimental and experiment_id to publish item
            var publishItem = new PublishItem
            {
                Url = file.SiteUrl,
                Path = outputPath,
                SourcePath = file.FilePath,
                Locale = file.Docset.Locale,
                Monikers = model.Metadata.Monikers,
                MonikerGroup = MonikerUtility.GetGroup(model.Metadata.Monikers),
            };

            if (context.PublishModelBuilder.TryAdd(file, publishItem))
            {
                if (file.Docset.Legacy)
                {
                    var output = context.TemplateEngine.TransformTocMetadata(JsonUtility.ToJObject(model));
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
