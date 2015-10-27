// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using Nustache.Core;
    using Utility;
    using Jint;
    using System.Linq;

    public class TemplateModelInfo
    {
        public string RelativePath { get; set; }
        public object Model { get; set; }
        public TemplateModelInfo(string path, object model)
        {
            RelativePath = path;
            Model = model;
        }

        public TemplateModelInfo() { }
    }

    public class TemplateProcessor : IDisposable
    {
        private static Regex IncludeRegex = new Regex(@"{{\s*!\s*include\s*\(:?(:?['""]?)\s*(?<file>(.+?))\1\s*\)\s*}}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private string _script = null;

        private Template _template;
        private ResourceCollection _resourceProvider = null;
        private string _extension;

        public ResourceCollection ResourceProvider { get { return _resourceProvider; } }

        public TemplateProcessor(string templateName, ResourceCollection resourceProvider)
        {
            _resourceProvider = resourceProvider;
            _extension = GetProcessedFileExtension(templateName);
            _template = GetTemplate(templateName);
            var scriptName = templateName + ".js";
            var script = resourceProvider.GetResource(scriptName);
            _script = script;
            ParseResult.WriteToConsole(ResultLevel.Info, $"Using template {templateName}" + script == null ? string.Empty : $" and pre-process script {scriptName}");
        }

        public TemplateProcessor(string template, string script, string extension, ResourceCollection resourceProvider)
        {
            _resourceProvider = resourceProvider;
            _extension = extension;
            _template = new Template(template, null);
            _script = script;
        }

        public void Process(IEnumerable<string> modelPaths, string baseDirectory, string outputDirectory)
        {
            if (string.IsNullOrEmpty(outputDirectory)) outputDirectory = Environment.CurrentDirectory;

            var modelInfo = modelPaths.Select(path =>
            {
                try
                {
                    using (var reader = new StreamReader(path))
                    {
                        object model = YamlUtility.Deserialize<object>(reader);

                        var modelRelativePath = FileExtensions.MakeRelativePath(baseDirectory, path);
                        return new TemplateModelInfo
                        {
                            RelativePath = modelRelativePath,
                            Model = model,
                        };
                    }
                }
                catch (Exception e)
                {
                    ParseResult.WriteToConsole(ResultLevel.Warning, $"File {path} is not in valid YAML format: {e.Message}. Ignored.");
                    return null;
                }
            }).Where(s => s != null);

            Process(modelInfo, outputDirectory);
        }

        public void Process(IEnumerable<TemplateModelInfo> modelInfo, string outputDirectory)
        {
            if (string.IsNullOrEmpty(outputDirectory)) outputDirectory = Environment.CurrentDirectory;

            // 1. Copy dependent files with path relative to the base output directory
            ProcessDependencies(outputDirectory);
            if (_template == null)
            {
                ParseResult.WriteToConsole(ResultLevel.Info, "No template will be applied");
                return;
            }
            
            // 2. Process every model and save to output directory
            foreach (var info in modelInfo)
            {
                var transformed = Transform(info.Model);
                var modelOutputPath = Path.ChangeExtension(Path.Combine(outputDirectory, info.RelativePath), _extension);
                if (!string.IsNullOrWhiteSpace(transformed))
                {
                    File.WriteAllText(modelOutputPath, transformed);
                    ParseResult.WriteToConsole(ResultLevel.Success, "Transformed model {0} to {1}.", info.RelativePath, modelOutputPath);
                }
                else
                {
                    ParseResult.WriteToConsole(ResultLevel.Verbose, "Model {0} is transformed to empty string, ignored.", info.RelativePath);
                }
            }
        }

        private Template GetTemplate(string templateName)
        {
            if (_resourceProvider == null) return null;

            // First, get resource with the same name as the template name;
            var template = _resourceProvider.GetResource(templateName);

            // Second, if resource does not exist, try getting resource with <templateName>.tmpl
            if (template == null) template = _resourceProvider.GetResource(templateName + ".tmpl");
            return new Template(template, templateName);
        }

        private void ProcessDependencies(string outputDirectory)
        {
            if (_resourceProvider == null)
            {
                ParseResult.WriteToConsole(ResultLevel.Info, "Resource provider is not specified, dependencies will not be processed.");
                return;
            }

            if (_template != null)
            {
                foreach (var resourceInfo in ExtractDependentFilePaths(_template).Distinct())
                {
                    try
                    {
                        var stream = _resourceProvider.GetResourceStream(resourceInfo.ResourceKey);
                        if (stream != null)
                        {
                            var path = Path.Combine(outputDirectory, resourceInfo.FilePath);
                            var dir = Path.GetDirectoryName(path);
                            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                            using (stream)
                            {
                                using(var writer = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
                                {
                                    stream.CopyTo(writer);
                                }
                            }

                            ParseResult.WriteToConsole(ResultLevel.Info, "Saved resource {0} that template dependants on to {1}", resourceInfo.ResourceKey, path);
                        }
                        else
                        {
                            ParseResult.WriteToConsole(ResultLevel.Info, "Unable to get relative resource for {0}", resourceInfo.ResourceKey);
                        }
                    }
                    catch (Exception e)
                    {
                        ParseResult.WriteToConsole(ResultLevel.Info, "Unable to get relative resource for {0}: {1}", resourceInfo.ResourceKey, e.Message);
                    }
                }
            }
        }

        /// <summary>
        /// 1. Find Template zip file or folder with provided name
        /// 2. If {name}.js file exists, run it
        /// </summary>
        /// <param name="model">The model to be parsed</param>
        private string Transform(object model)
        {
            if (_script == null)
            {
                if (_template == null) return null;
                return Render.StringToString(_template.Content, model);
            }
            else
            {
                var processedModel = ProcessWithJint(model);
                return Render.StringToString(_template.Content, processedModel);
            }
        }

        /// <summary>
        /// Dependent files are defined in following syntax in Mustache template leveraging Mustache Comments
        /// {{! include('file') }}
        /// file path can be wrapped by quote ' or double quote " or none
        /// </summary>
        /// <param name="template"></param>
        private IEnumerable<TemplateResourceInfo> ExtractDependentFilePaths(Template template)
        {
            foreach (Match match in IncludeRegex.Matches(template.Content))
            {
                var filePath = match.Groups["file"].Value;
                if (string.IsNullOrWhiteSpace(filePath)) yield break;
                if (filePath.StartsWith("./")) filePath = filePath.Substring(2);
                yield return new TemplateResourceInfo(template.GetRelativeResourceKey(filePath), filePath);
            }
        }

        private class Template
        {
            public string Content { get; }
            public string Name { get; }
            public string Type { get; }
            public Template(string template, string templateName)
            {
                Name = templateName;
                Content = template;
            }

            public string GetRelativeResourceKey(string relativePath)
            {
                return Path.Combine(Path.GetDirectoryName(this.Name ?? string.Empty) ?? string.Empty, relativePath).ToNormalizedPath();
            }
        }

        private sealed class TemplateResourceInfo
        {
            public string ResourceKey { get; }
            public string FilePath { get; }
            public TemplateResourceInfo(string resourceKey, string filePath)
            {
                ResourceKey = resourceKey;
                FilePath = filePath;
            }
        }
        private object ProcessWithJint(object model)
        {
            using (var stream = new StringWriter())
            {
                JsonUtility.Serialize(stream, model);

                var engine = new Engine();
                // engine.SetValue("model", stream.ToString());
                engine.SetValue("console", new
                {
                    log = new Action<object>(ParseResult.WriteInfo)
                });

                // throw exception when execution fails
                engine.Execute(_script);
                var value = engine.Invoke("transform", stream.ToString()).ToObject();

                // var value = engine.GetValue("model").ToObject();
                // The results generated
                return value;
            }
        }

        /// <summary>
        /// Supports template name formatting: 
        /// 1. {template description}.{template type}
        /// 2. {template type}
        /// </summary>
        /// <param name="templateName">Template name</param>
        /// <returns>The file extension for the generated file</returns>
        private static string GetProcessedFileExtension(string templateName)
        {
            string extension = Path.GetExtension(templateName);
            string fileName = Path.GetFileName(templateName);
            if (extension == string.Empty) extension = fileName;
            return extension;
        }

        public void Dispose()
        {
            _resourceProvider?.Dispose();
        }
    }
}
