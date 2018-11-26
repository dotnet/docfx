// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Exceptions;

    internal class MustacheTemplateRenderer : ITemplateRenderer
    {
        public const string Extension = ".tmpl";

        private static readonly Regex IncludeRegex = new Regex(@"{{\s*!\s*include\s*\(:?(:?['""]?)\s*(?<file>(.+?))\1\s*\)\s*}}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex MasterPageRegex = new Regex(@"{{\s*!\s*master\s*\(:?(:?['""]?)\s*(?<file>(.+?))\1\s*\)\s*}}\s*\n?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex MasterPageBodyRegex = new Regex(@"{{\s*!\s*body\s*}}\s*\n?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly ResourceTemplateLocator _resourceTemplateLocator;
        private readonly IResourceFileReader _reader;
        private readonly Nustache.Core.Template _template;

        public MustacheTemplateRenderer(IResourceFileReader reader, ResourceInfo info, string name = null)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            if (info.Content == null)
            {
                throw new ArgumentNullException(nameof(info.Content));
            }

            if (info.Path == null)
            {
                throw new ArgumentNullException(nameof(info.Path));
            }

            Path = info.Path;
            Name = name ?? System.IO.Path.GetFileNameWithoutExtension(Path);
            _reader = reader;
            _resourceTemplateLocator = new ResourceTemplateLocator(reader);

            _template = new Nustache.Core.Template();
            var processedTemplate = ParseTemplateHelper.ExpandMasterPage(reader, info, MasterPageRegex, MasterPageBodyRegex);
            using (var sr = new StringReader(processedTemplate))
            {
                try
                {
                    _template.Load(sr);
                }
                catch (Nustache.Core.NustacheException e)
                {
                    throw new DocfxException($"Error in mustache template {info.Path}: {e.Message}", e);
                }
            }

            Dependencies = ExtractDependencyResourceNames(processedTemplate).ToList();
        }

        public IEnumerable<string> Dependencies { get; }

        public string Raw { get; }

        public string Path { get; }

        public string Name { get; }

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
        private IEnumerable<string> ExtractDependencyResourceNames(string template)
        {
            foreach (Match match in IncludeRegex.Matches(template))
            {
                var filePath = match.Groups["file"].Value;
                foreach (var name in ParseTemplateHelper.GetResourceName(filePath, Path, _reader))
                {
                    yield return name;
                }
            }
        }
    }
}
