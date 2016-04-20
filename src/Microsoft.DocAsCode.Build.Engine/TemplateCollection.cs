// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Utility;

    public class TemplateCollection : Dictionary<string, TemplateBundle>
    {
        private TemplateBundle _defaultTemplate = null;

        public new TemplateBundle this[string key]
        {
            get
            {
                TemplateBundle template;
                if (key != null && this.TryGetValue(key, out template))
                {
                    return template;
                }

                return _defaultTemplate;
            }
            set
            {
                this[key] = value;
            }
        }

        public TemplateCollection(ResourceCollection provider, int maxParallelism) : base(ReadTemplate(provider, maxParallelism), StringComparer.OrdinalIgnoreCase)
        {
            base.TryGetValue("default", out _defaultTemplate);
        }

        private static Dictionary<string, TemplateBundle> ReadTemplate(ResourceCollection resource, int maxParallelism)
        {
            // type <=> list of template with different extension
            var dict = new Dictionary<string, List<Template>>(StringComparer.OrdinalIgnoreCase);
            if (resource == null || resource.IsEmpty)
            {
                return new Dictionary<string, TemplateBundle>();
            }

            // Template file ends with .tmpl(Mustache) or .liquid(Liquid)
            // Template file naming convention: {template file name}.{file extension}.(tmpl|liquid)
            var templates = resource.GetResources(@".*\.(tmpl|liquid|js)$").ToList();
            if (templates != null)
            {
                foreach (var group in templates.GroupBy(s => Path.GetFileNameWithoutExtension(s.Key), StringComparer.OrdinalIgnoreCase))
                {
                    var currentTemplates =
                        (from i in @group
                         select new
                         {
                             item = i.Value,
                             extension = Path.GetExtension(i.Key)
                         } into item
                         where IsSupportedTemplateFile(item.extension)
                         select item).ToArray();
                    var currentScripts =
                         (from i in @group
                          select new
                          {
                              item = i.Value,
                              extension = Path.GetExtension(i.Key)
                          } into item
                          where IsSupportedScriptFile(item.extension)
                          select item).ToArray();

                    if (currentTemplates.Length == 0 && currentScripts.Length == 0)
                    {
                        continue;
                    }

                    // If template file does not exists, while a js script ends with .tmpl.js exists
                    // we consider .tmpl.js file as a standalone preprocess file
                    if (currentTemplates.Length == 0 && !group.Key.EndsWith(".tmpl", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var currentTemplate = currentTemplates.FirstOrDefault();
                    var currentScript = currentScripts.FirstOrDefault();
                    if (currentTemplates.Length > 1)
                    {
                        Logger.Log(LogLevel.Warning, $"Multiple templates for type '{group.Key}'(case insensitive) are found, the one from '{currentTemplate.item + currentTemplate.extension}' is taken.");
                    }

                    if (currentScripts.Length > 1)
                    {
                        Logger.Log(LogLevel.Warning, $"Multiple template scripts for type '{group.Key}'(case insensitive) are found, the one from '{currentScript.item + currentScript.extension}' is taken.");
                    }

                    var name = group.Key;

                    var template = new Template(group.Key, currentTemplate?.extension, currentTemplate?.item, currentScript?.item, resource, maxParallelism);
                    List<Template> templateList;
                    if (dict.TryGetValue(template.Type, out templateList))
                    {
                        templateList.Add(template);
                    }
                    else
                    {
                        dict[template.Type] = new List<Template> { template };
                    }
                }
            }

            return dict.ToDictionary(s => s.Key, s => new TemplateBundle(s.Key, s.Value));
        }

        private static bool IsSupportedTemplateFile(string extension)
        {
            return extension.Equals(".tmpl", StringComparison.OrdinalIgnoreCase) || extension.Equals(".liquid", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSupportedScriptFile(string extension)
        {
            return extension.Equals(".js", StringComparison.OrdinalIgnoreCase);
        }
    }
}
