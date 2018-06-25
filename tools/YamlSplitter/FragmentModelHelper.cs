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
                        { "EnableSourceInfo", false }
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
            var ast = MDService.Parse(markdown, Path.GetFileName(fileName));
            var models = new MarkdownFragmentsCreater().Create(ast).ToList();
            return models.ToDictionary(m => m.Uid, m => m.ToMarkdownFragment(markdown));
        }
    }
}
