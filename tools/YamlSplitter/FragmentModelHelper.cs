// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tools.YamlSplitter
{
    using Microsoft.DocAsCode.Build.OverwriteDocuments;
    using Microsoft.DocAsCode.MarkdigEngine;
    using Microsoft.DocAsCode.MarkdigEngine.Extensions;
    using Microsoft.DocAsCode.Plugins;

    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public static class FragmentModelHelper
    {
        public static MarkdigMarkdownService MDService = new MarkdigMarkdownService(
            new MarkdownServiceParameters
            {
                BasePath = ".",
                Extensions = new Dictionary<string, object>
                    {
                        { LineNumberExtension.EnableSourceInfo, false }
                    }
            });

        public static Dictionary<string, MarkdownFragment> LoadMarkdownFragment(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }
            if (!File.Exists(fileName))
            {
                return new Dictionary<string, MarkdownFragment>();
            }
            var markdown = File.ReadAllText(fileName);
            var ast = MDService.Parse(markdown, fileName);
            var models = new MarkdownFragmentsCreater().Create(ast).ToList();
            return models.ToDictionary(m => m.Uid, m => m.ToMarkdownFragment(markdown));
        }

        public static MarkdownFragment ToMarkdownFragment(this MarkdownFragmentModel model, string originalContent)
        {
            Dictionary<string, object> metadata = null;
            if (!string.IsNullOrEmpty(model.YamlCodeBlock))
            {
                using (TextReader sr = new StringReader(model.YamlCodeBlock))
                {
                    metadata = Common.YamlUtility.Deserialize<Dictionary<string, object>>(sr);
                }
            }

            return new MarkdownFragment()
            {
                Uid = model.Uid,
                Metadata = metadata,
                Properties = model.Contents?.Select(prop => prop.ToMarkdownProperty(originalContent)).ToDictionary(p => p.OPath, p => p)
            };
        }

        public static MarkdownProperty ToMarkdownProperty(this MarkdownPropertyModel model, string originalContent)
        {
            var content = "";
            if (model.PropertyValue?.Count > 0)
            {
                var start = model.PropertyValue.First().Span.Start;
                var length = model.PropertyValue.Last().Span.End - start + 1;
                var piece = originalContent.Substring(start, length);
                if (!string.IsNullOrWhiteSpace(piece))
                {
                    content = piece;
                }
            }
            return new MarkdownProperty()
            {
                OPath = model.PropertyName,
                Content = content
            };
        }
    }
}
