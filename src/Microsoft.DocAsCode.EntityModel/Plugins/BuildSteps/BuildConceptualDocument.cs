// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;

    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(ConceptualDocumentProcessor), typeof(IDocumentBuildStep))]
    public class BuildConceptualDocument : BaseDocumentBuildStep
    {
        private const string ConceputalKey = "conceptual";
        private const string DocumentTypeKey = "documentType";

        public override string Name => nameof(BuildConceptualDocument);

        public override int BuildOrder => 0;

        public override void Build(FileModel model, IHostService host)
        {
            if (model.Type != DocumentType.Article)
            {
                return;
            }
            var content = (Dictionary<string, object>)model.Content;
            var markdown = (string)content[ConceputalKey];
            var result = host.Markup(markdown, model.FileAndType);
            content[ConceputalKey] = result.Html;
            content["title"] = result.Title;
            if (result.YamlHeader != null && result.YamlHeader.Count > 0)
            {
                foreach (var item in result.YamlHeader)
                {
                    if (item.Key == "uid")
                    {
                        var uid = item.Value as string;
                        if (!string.IsNullOrWhiteSpace(uid))
                        {
                            model.Uids = new[] { uid }.ToImmutableArray();
                        }
                    }
                    else
                    {
                        content[item.Key] = item.Value;
                        if (item.Key == DocumentTypeKey)
                        {
                            model.DocumentType = item.Value as string;
                        }
                    }
                }
            }
            model.Properties.LinkToFiles = result.LinkToFiles;
            model.Properties.LinkToUids = result.LinkToUids;
            model.File = Path.ChangeExtension(model.File, ".json");
        }
    }
}
