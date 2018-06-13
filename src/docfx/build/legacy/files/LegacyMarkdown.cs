// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class LegacyMarkdown
    {
        public static void Convert(
            Docset docset,
            Context context,
            Document doc,
            LegacyManifestOutput legacyManifestOutput)
        {
            var rawPageOutputPath = legacyManifestOutput.PageOutput.ToLegacyOutputPath(docset);
            File.Move(Path.Combine(docset.Config.Output.Path, doc.OutputPath), Path.Combine(docset.Config.Output.Path, rawPageOutputPath));

            var pageModel = JsonUtility.Deserialize<PageModel>(File.ReadAllText(Path.Combine(docset.Config.Output.Path, rawPageOutputPath)));
            if (!string.IsNullOrEmpty(pageModel.Content))
            {
                pageModel.Content = HtmlUtility.TransformHtml(
                    pageModel.Content,
                    node => node.AddLinkType(docset.Config.Locale)
                                .RemoveRerunCodepenIframes());
            }

            context.WriteJson(pageModel, rawPageOutputPath);
        }
    }
}
