// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class TemplateEngine
    {
        private readonly Package _package;
        private readonly Lazy<TemplateDefinition> _templateDefinition;
        private readonly JObject _global;
        private readonly LiquidTemplate _liquid;
        private readonly ThreadLocal<JavaScriptEngine> _js;
        private readonly MustacheTemplate _mustacheTemplate;
        private readonly BuildOptions _buildOptions;

        public TemplateEngine(ErrorBuilder errors, Config config, PackageResolver packageResolver, BuildOptions buildOptions)
        {
            _buildOptions = buildOptions;

            var template = config.Template;
            var templateFetchOptions = PackageFetchOptions.DepthOne;
            if (template.Type == PackageType.None)
            {
                template = new("_themes");
                templateFetchOptions |= PackageFetchOptions.IgnoreDirectoryNonExistedError;
            }

            _package = packageResolver.ResolveAsPackage(template, templateFetchOptions);

            _templateDefinition = new(() => _package.TryLoadYamlOrJson<TemplateDefinition>(errors, "template") ?? new());

            _global = LoadGlobalTokens(errors);

            _liquid = new(_package, config.TemplateBasePath, _global);
            _js = new(() => JavaScriptEngine.Create(_package, _global));
            _mustacheTemplate = new(_package, "ContentTemplate", _global);
        }

        public string RunLiquid(ErrorBuilder errors, SourceInfo<string?> mime, TemplateModel model)
        {
            var layout = model.RawMetadata?.Value<string>("layout") ?? mime.Value ?? throw new InvalidOperationException();

            var liquidModel = new JObject
            {
                ["content"] = model.Content,
                ["page"] = model.RawMetadata,
                ["metadata"] = model.PageMetadata,
            };

            return _liquid.Render(errors, layout, mime, liquidModel);
        }

        public string RunMustache(ErrorBuilder errors, string templateName, JToken pageModel)
        {
            return _mustacheTemplate.Render(errors, templateName, pageModel);
        }

        public JToken RunJavaScript(string scriptName, JObject model, string methodName = "transform")
        {
            var scriptPath = new PathString($"ContentTemplate/{scriptName}");
            if (!_package.Exists(scriptPath))
            {
                return model;
            }

            var result = _js.Value!.Run(scriptPath, methodName, model);
            if (result is JObject obj && obj.TryGetValue("content", out var token) &&
                token is JValue value && value.Value is string content)
            {
                try
                {
                    return JToken.Parse(content);
                }
                catch
                {
                    return result;
                }
            }
            return result;
        }

        public void CopyAssetsToOutput(Output output, bool selfContained = true)
        {
            if (!selfContained || _templateDefinition.Value.Assets.Length <= 0)
            {
                return;
            }

            var glob = GlobUtility.CreateGlobMatcher(_templateDefinition.Value.Assets);

            Parallel.ForEach(_package.GetFiles(), file =>
            {
                if (glob(file))
                {
                    output.Copy(file, _package, file);
                }
            });
        }

        public string? GetToken(string key)
        {
            return _global[key]?.ToString();
        }

        private JObject LoadGlobalTokens(ErrorBuilder errors)
        {
            var defaultTokens = _package.TryLoadYamlOrJson<JObject>(errors, "ContentTemplate/token");
            var localeTokens = _package.TryLoadYamlOrJson<JObject>(errors, $"ContentTemplate/token.{_buildOptions.Locale}");
            if (defaultTokens == null)
            {
                return localeTokens ?? new JObject();
            }
            JsonUtility.Merge(defaultTokens, localeTokens);
            return defaultTokens;
        }
    }
}
