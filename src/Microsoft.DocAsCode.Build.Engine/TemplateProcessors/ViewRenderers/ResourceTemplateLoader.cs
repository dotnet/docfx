// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using Stubble.Core.Interfaces;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Threading.Tasks;

    internal sealed class ResourceTemplateLoader : IStubbleLoader
    {
        private const string PartialTemplateExtension = ".tmpl.partial";
        private readonly ConcurrentDictionary<string, string> _templateCache = new ConcurrentDictionary<string, string>();
        private readonly IResourceFileReader _reader;

        public ResourceTemplateLoader(IResourceFileReader reader)
        {
            _reader = reader;
        }

        public string Load(string name)
        {
            if (_reader == null) return null;
            var resourceName = name + PartialTemplateExtension;

            return _templateCache.GetOrAdd(resourceName, s =>
            {
                lock (_reader)
                {
                    using var stream = _reader.GetResourceStream(s);
                    if (stream == null) return null;

                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
            });
        }

        public ValueTask<string> LoadAsync(string name)
        {
            return new ValueTask<string>(Load(name));
        }

        public IStubbleLoader Clone()
        {
            return new ResourceTemplateLoader(_reader);
        }
    }
}
