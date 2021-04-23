// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class TemplateEngineRunner
    {
        private readonly Package _package;
        private readonly LiquidTemplate _liquid;
        private readonly ThreadLocal<JavaScriptEngine> _js;
        private readonly MustacheTemplate _mustacheTemplate;

        public TemplateEngineRunner(Package package, JObject global, string? templateBasePath = null)
        {
            _package = package;
            _liquid = new(_package, templateBasePath, global);
            _js = new(() => JavaScriptEngine.Create(_package, global));
            _mustacheTemplate = new(_package, "ContentTemplate", global);
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
    }
}
