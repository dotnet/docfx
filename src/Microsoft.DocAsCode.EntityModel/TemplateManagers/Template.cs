// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    using Jint;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Utility;

    public class Template
    {
        private static readonly Regex IsRegexPatternRegex = new Regex(@"^\s*/(.*)/\s*$", RegexOptions.Compiled);
        private readonly ITemplateRenderer _renderer;
        private readonly Engine _engine;
        private readonly string _script;

        public string Name { get; }
        public string Extension { get; }
        public string Type { get; }
        public bool IsPrimary { get; }
        public IEnumerable<TemplateResourceInfo> Resources { get; }

        public Template(string template, string templateName, string script, ResourceCollection resourceCollection)
        {
            if (string.IsNullOrEmpty(templateName)) throw new ArgumentNullException(nameof(templateName));
            if (string.IsNullOrEmpty(template)) throw new ArgumentNullException(nameof(template));
            Name = templateName;
            var typeAndExtension = GetTemplateTypeAndExtension(templateName);
            Extension = typeAndExtension.Item2;
            Type = typeAndExtension.Item1;
            IsPrimary = typeAndExtension.Item3;
            _script = script;
            _engine = CreateEngine(script);

            _renderer = CreateRenderer(resourceCollection, templateName, template);
            Resources = ExtractDependentResources();
        }

        public TemplateTransformedResult TransformModel(object model, object attrs)
        {
            if (_renderer == null) return null;
            if (_engine != null)
            {
                model = ProcessWithJint(model, attrs);
            }

            return new TemplateTransformedResult(model, _renderer.Render(model));
        }

        private object ProcessWithJint(object model, object attrs)
        {
            var argument1 = JintProcessorHelper.ConvertStrongTypeToJsValue(model);
            var argument2 = JintProcessorHelper.ConvertStrongTypeToJsValue(attrs);
            return _engine.Invoke("transform", argument1, argument2).ToObject();
        }

        private string GetRelativeResourceKey(string relativePath)
        {
            // Make sure resource keys are combined using '/'
            return Path.GetDirectoryName(this.Name).ToNormalizedPath().ForwardSlashCombine(relativePath);
        }

        private static Tuple<string, string, bool> GetTemplateTypeAndExtension(string templateName)
        {
            // Remove folder and .tmpl
            templateName = Path.GetFileNameWithoutExtension(templateName);
            var splitterIndex = templateName.IndexOf('.');
            if (splitterIndex < 0) return Tuple.Create(templateName, string.Empty, false);
            var type = templateName.Substring(0, splitterIndex);
            var extension = templateName.Substring(splitterIndex);
            var isPrimary = false;
            if (extension.EndsWith(".primary"))
            {
                isPrimary = true;
                extension = extension.Substring(0, extension.Length - 8);
            }
            return Tuple.Create(type, extension, isPrimary);
        }


        /// <summary>
        /// Dependent files are defined in following syntax in Mustache template leveraging Mustache Comments
        /// {{! include('file') }}
        /// file path can be wrapped by quote ' or double quote " or none
        /// </summary>
        /// <param name="template"></param>
        private IEnumerable<TemplateResourceInfo> ExtractDependentResources()
        {
            if (_renderer == null || _renderer.Dependencies == null) yield break;
            foreach (var dependency in _renderer.Dependencies)
            {
                string filePath = dependency;
                if (string.IsNullOrWhiteSpace(filePath)) continue;
                if (filePath.StartsWith("./")) filePath = filePath.Substring(2);
                var regexPatternMatch = IsRegexPatternRegex.Match(filePath);
                if (regexPatternMatch.Groups.Count > 1)
                {
                    filePath = regexPatternMatch.Groups[1].Value;
                    yield return new TemplateResourceInfo(GetRelativeResourceKey(filePath), filePath, true);
                }
                else
                {
                    yield return new TemplateResourceInfo(GetRelativeResourceKey(filePath), filePath, false);
                }
            }
        }

        private static Engine CreateEngine(string script)
        {
            if (string.IsNullOrEmpty(script)) return null;
            var engine = new Engine();

            engine.SetValue("console", new
            {
                log = new Action<object>(Logger.Log)
            });

            // throw exception when execution fails
            engine.Execute(script);
            return engine;
        }

        private static ITemplateRenderer CreateRenderer(ResourceCollection resourceCollection, string templateName, string template)
        {
            if (resourceCollection == null) return null;
            if (Path.GetExtension(templateName).Equals(".liquid", StringComparison.OrdinalIgnoreCase))
            {
                return LiquidTemplateRenderer.Create(resourceCollection, template);
            }
            else
            {
                return new MustacheTemplateRenderer(resourceCollection, template);
            }
        }
    }
}
