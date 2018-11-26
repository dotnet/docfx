// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Concurrent;
    using System.IO;

    internal sealed class ResourceTemplateLocator
    {
        private const string PartialTemplateExtension = ".tmpl.partial";
        private readonly ConcurrentDictionary<string, Nustache.Core.Template> _templateCache = new ConcurrentDictionary<string, Nustache.Core.Template>();
        private readonly IResourceFileReader _reader;
        public ResourceTemplateLocator(IResourceFileReader reader)
        {
            _reader = reader;
        }

        public Nustache.Core.Template GetTemplate(string name)
        {
            if (_reader == null) return null;
            var resourceName = name + PartialTemplateExtension;
            return _templateCache.GetOrAdd(resourceName, s =>
                {
                    lock (_reader)
                    {
                        using (var stream = _reader.GetResourceStream(s))
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
                    }
                });
        }
    }
}
