// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.Plugins;

    public class TemplateBundle
    {
        public string Extension { get; }

        public IEnumerable<TemplateResourceInfo> Resources { get; }

        public IEnumerable<Template> Templates { get; }

        public string DocumentType { get; }

        public TemplateBundle(string documentType, IEnumerable<Template> templates)
        {
            if (string.IsNullOrEmpty(documentType))
            {
                throw new ArgumentNullException(nameof(documentType));
            }
            if (templates == null)
            {
                throw new ArgumentNullException(nameof(templates));
            }

            DocumentType = documentType;
            Templates = templates.ToArray();

            var defaultTemplate =
                Templates.FirstOrDefault(s => s.TemplateType == TemplateType.Primary)
                ?? Templates.FirstOrDefault(s => s.TemplateType != TemplateType.Auxiliary);
            Extension = defaultTemplate?.Extension ?? string.Empty;
            Resources = Templates.SelectMany(s => s.Resources).Distinct();
        }

        internal TransformModelOptions GetOptions(InternalManifestItem item, IDocumentBuildContext context)
        {
            return MergeOptions(GetOptionsForEachTemplate(item, context));
        }

        private TransformModelOptions MergeOptions(IEnumerable<TransformModelOptions> optionsList)
        {
            var result = new TransformModelOptions();
            var bookmarks = new Dictionary<string, string>();
            foreach (var options in optionsList)
            {
                // The model is shared if options defined in any template is shared
                if (options.IsShared)
                {
                    result.IsShared = true;
                }

                // If one uid is defined in multiple templates, the value is undetermined
                if (options.Bookmarks != null)
                {
                    foreach (var pair in options.Bookmarks)
                    {
                        bookmarks[pair.Key] = pair.Value;
                    }
                }
            }
            result.Bookmarks = bookmarks;
            return result;
        }

        private IEnumerable<TransformModelOptions> GetOptionsForEachTemplate(InternalManifestItem item, IDocumentBuildContext context)
        {
            if (item == null)
            {
                yield break;
            }

            foreach (var template in Templates)
            {
                if (template.ContainsGetOptions)
                {
                    var options = template.GetOptions(item.Model.Content);
                    if (options != null)
                    {
                        yield return options;
                    }
                }
            }
        }
    }
}
