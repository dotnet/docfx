// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Common;

    internal class LiquidTemplateRenderer : ITemplateRenderer
    {
        private static readonly object _locker = new object();
        private static readonly Regex MasterPageRegex = new Regex(@"{%\-?\s*master\s*:?(:?['""]?)\s*(?<file>(.+?))\1\s*\-?%}\s*\n?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex MasterPageBodyRegex = new Regex(@"{%\-?\s*body\s*\-?%}\s*\n?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly DotLiquid.Template _template;

        public static LiquidTemplateRenderer Create(ResourceCollection resourceProvider, TemplateRendererResource info)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            if (info.Content == null)
            {
                throw new ArgumentNullException(nameof(info.Content));
            }

            if (info.TemplateName == null)
            {
                throw new ArgumentNullException(nameof(info.TemplateName));
            }

            var processedTemplate = ParseTemplateHelper.ExpandMasterPage(resourceProvider, info, MasterPageRegex, MasterPageBodyRegex);

            // Guarantee that each time returns a new renderer
            // As Dependency is a globally shared object, allow one entry at a time
            lock (_locker)
            {
                DotLiquid.Template.RegisterTag<Dependency>("ref");
                Dependency.PopDependencies();
                var liquidTemplate = DotLiquid.Template.Parse(processedTemplate);
                var dependencies = Dependency.PopDependencies();

                liquidTemplate.Registers.Add("file_system", new ResourceFileSystem(resourceProvider));

                return new LiquidTemplateRenderer(liquidTemplate, info.Content, info.TemplateName, resourceProvider, dependencies);
            }
        }

        private LiquidTemplateRenderer(DotLiquid.Template liquidTemplate, string template, string templateName, ResourceCollection resource, IEnumerable<string> dependencies)
        {
            _template = liquidTemplate;
            Raw = template;
            Dependencies = ParseDependencies(templateName, resource, dependencies).ToList();
        }

        private IEnumerable<string> ParseDependencies(string templateName, ResourceCollection resource, IEnumerable<string> raw)
        {
            return from item in raw
                   from name in ParseTemplateHelper.GetResourceName(item, templateName, resource)
                   select name;
        }

        public string Raw { get; }

        public IEnumerable<string> Dependencies { get; }

        public string Render(object model)
        {
            model = ConvertToObjectHelper.ConvertJObjectToObject(model);
            model = ConvertToObjectHelper.ConvertExpandoObjectToObject(model);
            if (model is IDictionary<string, object>)
            {
                return _template.Render(DotLiquid.Hash.FromDictionary((IDictionary<string, object>)model));
            }

            return _template.Render(DotLiquid.Hash.FromAnonymousObject(model));
        }

        private sealed class Dependency : DotLiquid.Tag
        {
            private static readonly HashSet<string> SharedDependencies = new HashSet<string>();
            public override void Initialize(string tagName, string markup, List<string> tokens)
            {
                base.Initialize(tagName, markup, tokens);
                lock (_locker)
                {
                    SharedDependencies.Add(markup);
                }
            }

            public static ImmutableArray<string> PopDependencies()
            {
                lock (_locker)
                {
                    var array = SharedDependencies.ToImmutableArray();
                    SharedDependencies.Clear();
                    return array;
                }
            }
        }

        /// <summary>
        /// For liquid, follow the same naming convention as Rails partials
        /// ie. with the template name prefixed with an underscore. The extension ".liquid" is also added.
        /// e.g. dir/partial => dir/_partial.liquid
        /// </summary>
        private sealed class ResourceFileSystem : DotLiquid.FileSystems.IFileSystem
        {
            private readonly ConcurrentDictionary<string, string> _templateCache = new ConcurrentDictionary<string, string>();
            private readonly ResourceCollection _resourceProvider;

            public ResourceFileSystem(ResourceCollection resourceProvider)
            {
                _resourceProvider = resourceProvider;
            }

            public string ReadTemplateFile(DotLiquid.Context context, string templateName)
            {
                if (_resourceProvider == null) return null;

                return _templateCache.GetOrAdd(templateName, s =>
                {
                    string resourceName;
                    var slashIndex = templateName.LastIndexOf('/');
                    if (slashIndex > -1)
                    {
                        var fileName = templateName.Substring(slashIndex + 1);
                        resourceName = $"{templateName.Substring(0, slashIndex)}/_{fileName}.liquid";
                    }
                    else
                    {
                        resourceName = $"_{templateName}.liquid";
                    }

                    return _resourceProvider.GetResource(resourceName);
                });
            }
        }
    }
}
