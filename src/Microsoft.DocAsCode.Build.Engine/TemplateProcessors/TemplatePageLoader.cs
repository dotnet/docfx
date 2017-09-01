// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.Common;

    public class TemplatePageLoader
    {
        private readonly IResourceFileReader _reader;
        private readonly int _maxParallelism;
        private readonly RendererLoader _rendererLoader;
        private readonly PreprocessorLoader _preprocessorLoader;

        public TemplatePageLoader(IResourceFileReader reader, DocumentBuildContext context, int maxParallelism)
        {
            _reader = reader;
            _maxParallelism = maxParallelism;
            _rendererLoader = new RendererLoader(reader, maxParallelism);
            _preprocessorLoader = new PreprocessorLoader(reader, context, maxParallelism);
        }

        public IEnumerable<Template> LoadAll()
        {
            foreach(var render in _rendererLoader.LoadAll())
            {
                var preprocessors = _preprocessorLoader.LoadFromRenderer(render).ToList();
                if (preprocessors.Count > 1)
                {
                    Logger.Log(
                        LogLevel.Warning, 
                        $"Multiple template preprocessors '{preprocessors.Select(s => s.Path).ToDelimitedString()}'(case insensitive) are found for template page '{preprocessors[0].Name}', '{preprocessors[0].Path}' is used and others are ignored.");
                }

                yield return new Template(render, preprocessors.FirstOrDefault());
            }

            foreach(var p in _preprocessorLoader.LoadStandalones())
            {
                yield return new Template(null, p);
            }
        }
    }
}

