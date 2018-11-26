// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Common;

    public class RendererLoader
    {
        private readonly IResourceFileReader _reader;
        private readonly int _maxParallelism;

        public RendererLoader(IResourceFileReader reader, int maxParallelism)
        {
            _reader = reader;
            _maxParallelism = maxParallelism;
        }

        public IEnumerable<ITemplateRenderer> LoadAll()
        {
            // Only files under root folder are allowed
            foreach (var res in _reader.GetResources($@"^[^/]*({Regex.Escape(MustacheTemplateRenderer.Extension)}|{Regex.Escape(LiquidTemplateRenderer.Extension)})$"))
            {
                var renderer = Load(res);
                if (renderer != null)
                {
                    yield return renderer;
                }
            }
        }

        public ITemplateRenderer Load(string path)
        {
            var content = _reader.GetResource(path);
            if (content == null)
            {
                return null;
            }

            return Load(new ResourceInfo(path, content));
        }

        public ITemplateRenderer Load(ResourceInfo res)
        {
            if (res == null)
            {
                return null;
            }

            using (new LoggerFileScope(res.Path))
            {
                var extension = Path.GetExtension(res.Path);
                if (extension.Equals(MustacheTemplateRenderer.Extension, System.StringComparison.OrdinalIgnoreCase))
                {
                    return new RendererWithResourcePool(() => new MustacheTemplateRenderer(_reader, res), _maxParallelism);
                }
                else if (extension.Equals(LiquidTemplateRenderer.Extension, System.StringComparison.OrdinalIgnoreCase))
                {
                    return new RendererWithResourcePool(() => LiquidTemplateRenderer.Create(_reader, res), _maxParallelism);
                }
                else
                {
                    Logger.LogWarning($"{res.Path} is not a supported template view.");
                    return null;
                }
            }
        }
    }
}

