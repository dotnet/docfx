// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text.RegularExpressions;

using Acornima.Ast;

using Docfx.Common;
using Jint;

namespace Docfx.Build.Engine;

public class PreprocessorLoader
{
    private readonly ResourceFileReader _reader;
    private readonly int _maxParallelism;
    private readonly DocumentBuildContext _context;

    public PreprocessorLoader(ResourceFileReader reader, DocumentBuildContext context, int maxParallelism)
    {
        _reader = reader;
        _maxParallelism = maxParallelism;
        _context = context;
    }

    public IEnumerable<ITemplatePreprocessor> LoadStandalones()
    {
        // Only files under root folder are allowed
        foreach (var res in _reader.GetResources($"^[^/]*{Regex.Escape(TemplateJintPreprocessor.StandaloneExtension)}$"))
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
                // Every preprocessor the pool creates for this template runs the same script sources,
                // so let them share one parsed copy of each.
                var preparedScripts = new ConcurrentDictionary<string, Prepared<Script>>();
                return new PreprocessorWithResourcePool(() => new TemplateJintPreprocessor(_reader, res, _context, name, preparedScripts), _maxParallelism);
            }
            else
            {
                Logger.LogWarning($"{res.Path} is not a supported template preprocessor.");
                return null;
            }
        }
    }
}

