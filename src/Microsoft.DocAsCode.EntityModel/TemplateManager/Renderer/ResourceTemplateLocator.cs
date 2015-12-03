// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.IO;

    internal sealed class ResourceTemplateLocator
    {
        private const string PartialTemplateExtension = ".tmpl.partial";
        private ResourceCollection _resourceProvider;
        public ResourceTemplateLocator(ResourceCollection resourceProvider)
        {
            _resourceProvider = resourceProvider;
        }

        public Nustache.Core.Template GetTemplate(string name)
        {
            if (_resourceProvider == null) return null;
            var resourceName = name + PartialTemplateExtension;
            using (var stream = _resourceProvider.GetResourceStream(resourceName))
            {
                if (stream == null) return null;
                var template = new Nustache.Core.Template(name);
                using (StreamReader reader = new StreamReader(stream))
                    template.Load(reader);
                return template;
            }
        }
    }
}
