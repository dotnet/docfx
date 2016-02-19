// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Collections.Concurrent;
    using System.IO;

    internal sealed class ResourceTemplateLocator
    {
        private const string PartialTemplateExtension = ".tmpl.partial";
        private readonly ConcurrentDictionary<string, Nustache.Core.Template> _templateCache = new ConcurrentDictionary<string, Nustache.Core.Template>();
        private readonly ResourceCollection _resourceProvider;
        public ResourceTemplateLocator(ResourceCollection resourceProvider)
        {
            _resourceProvider = resourceProvider;
        }

        public Nustache.Core.Template GetTemplate(string name)
        {
            if (_resourceProvider == null) return null;
            var resourceName = name + PartialTemplateExtension;
            return _templateCache.GetOrAdd(resourceName, s =>
                {
                    using (var stream = _resourceProvider.GetResourceStream(s))
                    {
                        if (stream == null)
                        {
                            return null;
                        }

                        var template = new Nustache.Core.Template(name);
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            template.Load(reader);
                        }
                        return template;
                    }
                });
        }
    }
}
