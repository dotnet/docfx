// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Common;

    public class PreprocessorLoader
    {
        private readonly IResourceFileReader _reader;
        private readonly int _maxParallelism;
        private readonly DocumentBuildContext _context;

        public PreprocessorLoader(IResourceFileReader reader, DocumentBuildContext context, int maxParallelism)
        {
            _reader = reader;
            _maxParallelism = maxParallelism;
            _context = context;
        }

        public IEnumerable<ITemplatePreprocessor> LoadStandalones()
        {
            // Only files under root folder are allowed
            foreach (var res in _reader.GetResources($@"^[^/]*{Regex.Escape(TemplateJintPreprocessor.StandaloneExtension)}$"))
            {
                var name = Path.GetFileNameWithoutExtension(res.Path.Remove(res.Path.LastIndexOf('.')));
                var preprocessor = Load(res, name);
                if (preprocessor != null)
                {
                    yield return preprocessor;
                }
            }
        }

        public IEnumerable<ITemplatePreprocessor> LoadFromRenderer(ITemplateRenderer renderer)
        {
            var viewPath = renderer.Path;
            var preproceesorPath = Path.ChangeExtension(viewPath, TemplateJintPreprocessor.Extension);
            var res = _reader.GetResource(preproceesorPath);
            var preprocessor = Load(new ResourceInfo(preproceesorPath, res), renderer.Name);
            if (preprocessor != null)
            {
                yield return preprocessor;
            }
        }

        public ITemplatePreprocessor Load(ResourceInfo res, string name = null)
        {
            if (res == null || string.IsNullOrWhiteSpace(res.Content))
            {
                return null;
            }

            using (new LoggerFileScope(res.Path))
            {
                var extension = Path.GetExtension(res.Path);
                if (extension.Equals(TemplateJintPreprocessor.Extension, System.StringComparison.OrdinalIgnoreCase))
                {
                    return new PreprocessorWithResourcePool(() => new TemplateJintPreprocessor(_reader, res, _context, name), _maxParallelism);
                }
                else
                {
                    Logger.LogWarning($"{res.Path} is not a supported template preprocessor.");
                    return null;
                }
            }
        }
    }
}

