// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    internal class MustacheTemplateRenderer : ITemplateRenderer
    {
        private static readonly Regex IncludeRegex = new Regex(@"{{\s*!\s*include\s*\(:?(:?['""]?)\s*(?<file>(.+?))\1\s*\)\s*}}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private ResourceTemplateLocator _resourceTemplateLocator;
        private Nustache.Core.Template _template = null;
        public MustacheTemplateRenderer(ResourceCollection resourceProvider, string template)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            _resourceTemplateLocator = new ResourceTemplateLocator(resourceProvider);
            _template = new Nustache.Core.Template();
            using (var reader = new StringReader(template))
                _template.Load(reader);
            Dependencies = ExtractDependentFilePaths(template);
        }

        public IEnumerable<string> Dependencies { get; }
        public string Raw { get; }

        public string Render(object model)
        {
            using (var writer = new StringWriter())
            {
                _template.Render(model, writer, _resourceTemplateLocator.GetTemplate);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Dependent files are defined in following syntax in Mustache template leveraging Mustache Comments
        /// {{! include('file') }}
        /// file path can be wrapped by quote ' or double quote " or none
        /// </summary>
        /// <param name="template"></param>
        private IEnumerable<string> ExtractDependentFilePaths(string template)
        {
            foreach (Match match in IncludeRegex.Matches(template))
            {
                var filePath = match.Groups["file"].Value;
                if (string.IsNullOrWhiteSpace(filePath)) yield break;
                yield return filePath;
            }
        }
    }
}
