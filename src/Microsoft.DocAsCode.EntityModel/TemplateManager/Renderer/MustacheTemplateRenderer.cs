// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class MustacheTemplateRenderer : IRenderer
    {
        private ResourceTemplateLocator _resourceTemplateLocator;
        private Nustache.Core.Template _template = null;
        public MustacheTemplateRenderer(ResourceCollection resourceProvider, string template)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            _resourceTemplateLocator = new ResourceTemplateLocator(resourceProvider);
            _template = new Nustache.Core.Template();
            using (var reader = new StringReader(template))
                _template.Load(reader);
        }

        public IEnumerable<string> Dependencies { get; private set; }

        public string Render(object model)
        {
            using (var writer = new StringWriter())
            {
                _template.Render(model, writer, _resourceTemplateLocator.GetTemplate);
                return writer.ToString();
            }
        }
    }
}
