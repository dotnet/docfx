// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class TemplateRendererResource
    {
        public static readonly Dictionary<string, TemplateRendererType> TemplateRenderTypeMapping = new Dictionary<string, TemplateRendererType>(StringComparer.OrdinalIgnoreCase)
        {
            [".liquid"] = TemplateRendererType.Liquid,
            [".tmpl"] = TemplateRendererType.Mustache
        };

        public string Content { get; }
        public string ResourceName { get; }
        public string TemplateName { get; }
        public TemplateRendererType Type { get; }

        public TemplateRendererResource(string resourceName, string content, string templateName)
        {
            var extension = Path.GetExtension(resourceName);
            if (!TemplateRenderTypeMapping.TryGetValue(extension, out TemplateRendererType type))
            {
                throw new NotSupportedException($"The template extension {extension} is not supported.");
            }
            Type = type;
            TemplateName = templateName;
            ResourceName = resourceName;
            Content = content;
        }
    }
}
