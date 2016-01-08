// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Utility;

    public class TemplateCollection : Dictionary<string, List<Template>>
    {
        private List<Template> _defaultTemplate = null;

        public new List<Template> this[string key]
        {
            get
            {
                List<Template> template;
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

        public TemplateCollection(ResourceCollection provider) : base(ReadTemplate(provider), StringComparer.OrdinalIgnoreCase)
        {
            base.TryGetValue("default", out _defaultTemplate);
        }

        private static Dictionary<string, List<Template>> ReadTemplate(ResourceCollection resource)
        {
            // type <=> list of template with different extension
            var dict = new Dictionary<string, List<Template>>(StringComparer.OrdinalIgnoreCase);
            if (resource == null || resource.IsEmpty) return dict;
            // Template file ends with .tmpl(Mustache) or .liquid(Liquid)
            // Template file naming convention: {template file name}.{file extension}.(tmpl|liquid)
            var templates = resource.GetResources(@".*\.(tmpl|liquid|js)$").ToList();
            if (templates != null)
            {
                foreach (var group in templates.GroupBy(s => Path.GetFileNameWithoutExtension(s.Key), StringComparer.OrdinalIgnoreCase))
                {
                    var currentTemplates = group.Where(s => Path.GetExtension(s.Key).Equals(".tmpl", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(s.Key).Equals(".liquid", StringComparison.OrdinalIgnoreCase)).ToArray();
                    var currentScripts = group.Where(s => Path.GetExtension(s.Key).Equals(".js", StringComparison.OrdinalIgnoreCase)).ToArray();
                    var currentTemplate = currentTemplates.FirstOrDefault();
                    var currentScript = currentScripts.FirstOrDefault();
                    if (currentTemplates.Length > 1)
                    {
                        Logger.Log(LogLevel.Warning, $"Multiple templates for type '{group.Key}'(case insensitive) are found, the one from '{currentTemplates[0].Key}' is taken.");
                    }
                    else if (currentTemplates.Length == 0)
                    {
                        // If template does not exist, ignore
                        continue;
                    }

                    if (currentScripts.Length > 1)
                    {
                        Logger.Log(LogLevel.Warning, $"Multiple template scripts for type '{group.Key}'(case insensitive) are found, the one from '{currentScripts[0].Key}' is taken.");
                    }

                    var template = new Template(currentTemplate.Value, currentTemplate.Key, currentScript.Value, resource);
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

            return dict;
        }
    }
}
