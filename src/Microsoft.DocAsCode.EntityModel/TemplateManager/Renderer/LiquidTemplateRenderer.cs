// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Dynamic;
    using System.Linq;

    internal class LiquidTemplateRenderer : ITemplateRenderer
    {
        private static object _locker = new object();
        private DotLiquid.Template _template = null;
        public static LiquidTemplateRenderer Create(ResourceCollection resourceProvider, string template)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            lock (_locker)
            {
                DotLiquid.Template.RegisterTag<Dependency>("ref");
                Dependency.PopDependencies();
                var liquidTemplate = DotLiquid.Template.Parse(template);
                liquidTemplate.Registers.Add("file_system", new ResourceFileSystem(resourceProvider));
                var dependencies = Dependency.PopDependencies();
                return new LiquidTemplateRenderer(liquidTemplate, template, dependencies);
            }
        }

        private LiquidTemplateRenderer(DotLiquid.Template liquidTemplate, string template, IEnumerable<string> dependencies)
        {
            _template = liquidTemplate;
            Raw = template;
            Dependencies = dependencies;
        }

        public string Raw { get; }

        public IEnumerable<string> Dependencies { get; }

        public string Render(object model)
        {
            model = ConvertJObjectToObject(model);
            model = ConvertExpandoObjectToObject(model);
            if (model is IDictionary<string, object>)
            {
                return _template.Render(DotLiquid.Hash.FromDictionary((IDictionary<string, object>)model));
            }

            return _template.Render(DotLiquid.Hash.FromAnonymousObject(model));
        }

        private class Dependency : DotLiquid.Tag
        {
            private static readonly HashSet<string> SharedDependencies = new HashSet<string>();
            private static object _locker = new object();
            public override void Initialize(string tagName, string markup, List<string> tokens)
            {
                base.Initialize(tagName, markup, tokens);
                lock (locker)
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

        private static object ConvertExpandoObjectToObject(object raw)
        {
            if (raw is ExpandoObject)
            {
                Dictionary<string, object> model = new Dictionary<string, object>();
                foreach (var prop in (IDictionary<string, object>)raw)
                {
                    model.Add(prop.Key, ConvertExpandoObjectToObject(prop.Value));
                }
                return model;
            }
            if (raw is IEnumerable<object>)
            {
                return ((IEnumerable<object>)raw).Select(s => ConvertExpandoObjectToObject(s)).ToArray();
            }
            return raw;
        }

        private static object ConvertJObjectToObject(object raw)
        {
            var jValue = raw as JValue;
            if (jValue != null) { return jValue.Value; }
            var jArray = raw as JArray;
            if (jArray != null)
            {
                return jArray.Select(s => ConvertJObjectToObject(s)).ToArray();
            }
            var jObject = raw as JObject;
            if (jObject != null)
            {
                return jObject.ToObject<Dictionary<string, object>>().ToDictionary(p => p.Key, p => ConvertJObjectToObject(p.Value));
            }
            return raw;
        }
    }
}
