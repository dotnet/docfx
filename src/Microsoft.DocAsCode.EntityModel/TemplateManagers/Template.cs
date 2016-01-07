// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    using Jint;

    using Microsoft.DocAsCode.Utility;

    public class Template
    {
        private static readonly Regex IsRegexPatternRegex = new Regex(@"^\s*/(.*)/\s*$", RegexOptions.Compiled);
        private string _script = null;
        private ITemplateRenderer _renderer  = null;
        public string Name { get; }
        public string Extension { get; }
        public string Type { get; }
        public bool IsPrimary { get; }
        public IEnumerable<TemplateResourceInfo> Resources { get; }
        public Template(string template, string templateName, string script, ResourceCollection resourceProvider)
        {
            if (string.IsNullOrEmpty(templateName)) throw new ArgumentNullException(nameof(templateName));
            if (string.IsNullOrEmpty(template)) throw new ArgumentNullException(nameof(template));
            Name = templateName;
            var typeAndExtension = GetTemplateTypeAndExtension(templateName);
            Extension = typeAndExtension.Item2;
            Type = typeAndExtension.Item1;
            IsPrimary = typeAndExtension.Item3;
            _script = script;
            if (resourceProvider != null)
            {
                if (Path.GetExtension(templateName).Equals(".liquid", StringComparison.OrdinalIgnoreCase))
                {
                    _renderer = LiquidTemplateRenderer.Create(resourceProvider, template);
                }
                else
                {
                    _renderer = new MustacheTemplateRenderer(resourceProvider, template);
                }
            }

            Resources = ExtractDependentResources();
        }

        public string TransformModel(object model, object attrs)
        {
            if (_renderer == null) return null;
            if (_script != null)
            {
                model = ProcessWithJint(JsonUtility.Serialize(model), attrs);
            }

            return _renderer.Render(model);
        }

        public string Transform(string modelPath, object attrs)
        {
            if (_renderer == null) return null;
            object model;
            if (_script == null)
            {
                model = JsonUtility.Deserialize<object>(modelPath);
            }
            else
            {
                model = ProcessWithJint(File.ReadAllText(modelPath), attrs);
            }

            return _renderer.Render(model);
        }

        private string GetRelativeResourceKey(string relativePath)
        {
            // Make sure resource keys are combined using '/'
            return Path.GetDirectoryName(this.Name).ToNormalizedPath().ForwardSlashCombine(relativePath);
        }

        private object ProcessWithJint(string model, object attrs)
        {
            var engine = new Engine();

            // engine.SetValue("model", stream.ToString());
            engine.SetValue("console", new
            {
                log = new Action<object>(Logger.Log)
            });

            // throw exception when execution fails
            engine.Execute(_script);
            var value = engine.Invoke("transform", model, JsonUtility.Serialize(attrs)).ToObject();

            // var value = engine.GetValue("model").ToObject();
            // The results generated
            return value;
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
    }
}
