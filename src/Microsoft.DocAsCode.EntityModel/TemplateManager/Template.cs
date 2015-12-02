// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Jint;
    using Nustache.Core;

    using Microsoft.DocAsCode.Utility;

    internal class Template
    {
        private string _script = null;

        public string Content { get; }
        public string Name { get; }
        public string Extension { get; }
        public string Type { get; }
        public bool IsPrimary { get; }
        public Template(string template, string templateName, string script)
        {
            if (string.IsNullOrEmpty(templateName)) throw new ArgumentNullException(nameof(templateName));
            Name = templateName;
            Content = template;
            var typeAndExtension = GetTemplateTypeAndExtension(templateName);
            Extension = typeAndExtension.Item2;
            Type = typeAndExtension.Item1;
            IsPrimary = typeAndExtension.Item3;
            _script = script;
        }

        public string GetRelativeResourceKey(string relativePath)
        {
            // Make sure resource keys are combined using '/'
            return Path.GetDirectoryName(this.Name).ToNormalizedPath().ForwardSlashCombine(relativePath);
        }

        public string Transform(string modelPath, object attrs, TemplateLocator templateLocator)
        {
            if (_script == null)
            {
                var entity = JsonUtility.Deserialize<object>(modelPath);
                return Render.StringToString(Content, entity, templateLocator);
            }
            else
            {

                var processedModel = ProcessWithJint(File.ReadAllText(modelPath), attrs);
                return Render.StringToString(Content, processedModel, templateLocator);
            }
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
    }
}
