// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class TemplateCollection : Dictionary<string, TemplateBundle>
    {
        private TemplateBundle _defaultTemplate = null;

        public IResourceFileReader Reader { get; }

        public int MaxParallelism { get; }

        public new TemplateBundle this[string key]
        {
            get
            {
                if (key != null && TryGetValue(key, out TemplateBundle template))
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

        public TemplateCollection(ResourceFileReader provider, DocumentBuildContext context, int maxParallelism) : base(ReadTemplate(provider, context, maxParallelism), StringComparer.OrdinalIgnoreCase)
        {
            Reader = provider;
            MaxParallelism = maxParallelism;
            base.TryGetValue("default", out _defaultTemplate);
        }

        private static Dictionary<string, TemplateBundle> ReadTemplate(ResourceFileReader reader, DocumentBuildContext context, int maxParallelism)
        {
            // type <=> list of template with different extension
            if (reader == null || reader.IsEmpty)
            {
                return new Dictionary<string, TemplateBundle>(StringComparer.OrdinalIgnoreCase);
            }

            var templates = new TemplatePageLoader(reader, context, maxParallelism).LoadAll();

            return templates.GroupBy(s => s.Type).ToDictionary(s => s.Key, s => new TemplateBundle(s.Key, s.ToList()), StringComparer.OrdinalIgnoreCase);
        }
    }
}
