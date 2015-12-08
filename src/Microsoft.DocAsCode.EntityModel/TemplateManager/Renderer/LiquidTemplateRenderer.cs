// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;

    internal class LiquidTemplateRenderer : IRenderer
    {
        private static object locker = new object();
        DotLiquid.Template _template;
        public LiquidTemplateRenderer(ResourceCollection resourceProvider, string template)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            DotLiquid.Template.FileSystem = new ResourceFileSystem(resourceProvider);
            DotLiquid.Template.RegisterTag<Dependency>("ref");
            lock (locker)
            {
                Dependency.PopDependenciesWithNoLock();
                _template = DotLiquid.Template.Parse(template);
                Dependencies = Dependency.PopDependenciesWithNoLock();
            }

            Raw = template;
        }

        public string Raw { get; }

        public IEnumerable<string> Dependencies { get; private set; }

        public string Render(object model)
        {
            if (model is IDictionary<string, object>)
            {
                return _template.Render(DotLiquid.Hash.FromDictionary((IDictionary<string, object>)model));
            }

            return _template.Render(DotLiquid.Hash.FromAnonymousObject(model));
        }

        private class Dependency : DotLiquid.Tag
        {
            private static readonly HashSet<string> SharedDependencies = new HashSet<string>();
            private static object locker = new object();
            public override void Initialize(string tagName, string markup, List<string> tokens)
            {
                base.Initialize(tagName, markup, tokens);
                SharedDependencies.Add(markup);
            }

            public static ImmutableArray<string> PopDependenciesWithNoLock()
            {
                var array = SharedDependencies.ToImmutableArray();
                SharedDependencies.Clear();
                return array;
            }
        }

        /// <summary>
        /// For liquid, follow the same naming convention as Rails partials
        /// ie. with the template name prefixed with an underscore. The extension ".liquid" is also added.
        /// e.g. dir/partial => dir/_partial.liquid
        /// </summary>
        private class ResourceFileSystem : DotLiquid.FileSystems.IFileSystem
        {
            private ResourceCollection _resourceProvider;
            public ResourceFileSystem(ResourceCollection resourceProvider)
            {
                _resourceProvider = resourceProvider;
            }
            public string ReadTemplateFile(DotLiquid.Context context, string templateName)
            {
                if (_resourceProvider == null) return null;
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
            }
        }
    }
}
