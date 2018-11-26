// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Newtonsoft.Json.Linq;

    using Microsoft.DocAsCode.Common;

    public class Template
    {
        private const string Primary = ".primary";
        private const string Auxiliary = ".aux";

        private readonly object _locker = new object();

        public string Name { get; }
        public string ScriptName { get; }
        public string Extension { get; }
        public string Type { get; }
        public TemplateType TemplateType { get; }
        public IEnumerable<TemplateResourceInfo> Resources { get; }
        public bool ContainsGetOptions { get; }
        public bool ContainsModelTransformation { get; }

        public ITemplateRenderer Renderer { get; }
        public ITemplatePreprocessor Preprocessor { get; }

        public Template(ITemplateRenderer renderer, ITemplatePreprocessor preprocessor)
        {
            if (renderer == null && preprocessor == null)
            {
                throw new ArgumentNullException("Both renderer and preprocessor are null");
            }

            Renderer = renderer;
            Preprocessor = preprocessor;

            Name = renderer?.Name ?? preprocessor?.Name;
            ScriptName = preprocessor?.Path;


            var templateInfo = GetTemplateInfo(Name);
            Extension = templateInfo.Extension;
            Type = templateInfo.DocumentType;
            TemplateType = templateInfo.TemplateType;

            ContainsGetOptions = Preprocessor?.ContainsGetOptions == true;
            ContainsModelTransformation = Preprocessor?.ContainsModelTransformation == true;

            Resources = ExtractDependentResources(Name);

            if (Renderer == null && !ContainsGetOptions && !ContainsModelTransformation)
            {
                Logger.LogWarning($"Template {Name} contains neither preprocessor to process model nor template to render model. Please check if the template is correctly defined. Allowed preprocessor functions are [exports.getOptions] and [exports.transform].");
            }
        }

        /// <summary>
        /// exports.getOptions = function (model) {
        ///     return {
        ///         bookmarks : {
        ///             uid1: "bookmark1"
        ///         },
        ///         isShared: true
        ///     }
        /// 
        /// }
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public TransformModelOptions GetOptions(object model)
        {
            object obj = Preprocessor?.GetOptions(model) ?? null;
            if (obj == null)
            {
                return null;
            }

            return JObject.FromObject(obj).ToObject<TransformModelOptions>();
        }

        /// <summary>
        /// Transform from raw model to view model
        /// TODO: refactor to merge model and attrs into one input model
        /// </summary>
        /// <param name="model">The raw model</param>
        /// <param name="attrs">The system generated attributes</param>
        /// <returns>The view model</returns>
        public object TransformModel(object model)
        {
            if (Preprocessor == null)
            {
                return model;
            }

            return Preprocessor.TransformModel(model);
        }

        /// <summary>
        /// Transform from view model to the final result using template
        /// Supported template languages are mustache and liquid
        /// </summary>
        /// <param name="model">The input view model</param>
        /// <returns>The output after applying template</returns>
        public string Transform(object model)
        {
            if (Renderer == null || model == null)
            {
                return null;
            }

            return Renderer.Render(model);
        }

        /// <summary>
        /// Dependent files are defined in following syntax in Mustache template leveraging Mustache Comments
        /// {{! include('file') }}
        /// file path can be wrapped by quote ' or double quote " or none
        /// </summary>
        /// <param name="template"></param>
        private IEnumerable<TemplateResourceInfo> ExtractDependentResources(string templateName)
        {
            if (Renderer == null || Renderer.Dependencies == null)
            {
                yield break;
            }

            foreach (var dependency in Renderer.Dependencies)
            {
                yield return new TemplateResourceInfo(dependency);
            }
        }

        private static TemplateInfo GetTemplateInfo(string templateName)
        {
            // Remove folder
            templateName = Path.GetFileName(templateName);
            var splitterIndex = templateName.IndexOf('.');
            if (splitterIndex < 0)
            {
                return new TemplateInfo(templateName, string.Empty, TemplateType.Default);
            }

            var type = templateName.Substring(0, splitterIndex);
            var extension = templateName.Substring(splitterIndex);
            TemplateType templateType = TemplateType.Default;
            if (extension.EndsWith(Primary))
            {
                templateType = TemplateType.Primary;
                extension = extension.Substring(0, extension.Length - Primary.Length);
            }
            else if (extension.EndsWith(Auxiliary))
            {
                templateType = TemplateType.Auxiliary;
                extension = extension.Substring(0, extension.Length - Auxiliary.Length);
            }

            return new TemplateInfo(type, extension, templateType);
        }

        private sealed class TemplateInfo
        {
            public string DocumentType { get; }
            public string Extension { get; }
            public TemplateType TemplateType { get; }

            public TemplateInfo(string documentType, string extension, TemplateType type)
            {
                DocumentType = documentType;
                Extension = extension;
                TemplateType = type;
            }
        }
    }
}
