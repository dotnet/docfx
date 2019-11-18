// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;

    using Jint;
    using Jint.Native;
    using Jint.Native.Object;
    using Jint.Parser;
    using Microsoft.DocAsCode.Common;

    public class TemplateJintPreprocessor : ITemplatePreprocessor
    {
        public const string Extension = ".js";

        // If template file does not exists, while a js script ends with .tmpl.js exists
        // we consider .tmpl.js file as a standalone preprocess file
        public const string StandaloneExtension = ".tmpl.js";

        /// <summary>
        /// Support
        ///     console.log
        ///     console.info
        ///     console.warn
        ///     console.err
        ///     console.error
        /// in preprocessor script
        /// </summary>
        private const string ConsoleVariableName = "console";
        private const string UtilityVariableName = "templateUtility";
        private const string ExportsVariableName = "exports";
        private const string GetOptionsFuncVariableName = "getOptions";
        private const string TransformFuncVariableName = "transform";

        /// <summary>
        /// Support require functionality as similar to NodeJS and RequireJS:
        /// use `exports` to export the properties for one module
        /// use `require` to use the exported module
        /// 
        /// Sample:
        ///
        /// 1. A common script file common.js:
        /// ```
        /// exports.util = function(){};
        /// ```
        /// 2. The main script file main.js:
        /// ```js
        /// var common = require('./common.js');
        /// common.util();
        /// ```
        /// Comparing to NodeJS, only relative path starting with `./` is supported.
        /// The circular reference handler is similar to NodeJS: **unfinished copy**.
        /// https://nodejs.org/api/modules.html#modules_cycles
        /// </summary>
        private const string RequireFuncVariableName = "require";
        private const string RequireRelativePathPrefix = "./";

        private const string NullString = "null";

        private object _utilityObject;
        private static readonly object ConsoleObject = new
        {
            log = new Action<object>(s => Logger.Log(s ?? NullString)),
            info = new Action<object>(s => Logger.LogInfo((s ?? NullString).ToString())),
            warn = new Action<object>(s => Logger.LogWarning((s ?? NullString).ToString())),
            err = new Action<object>(s => Logger.LogError((s ?? NullString).ToString())),
            error = new Action<object>(s => Logger.LogError((s ?? NullString).ToString())),
        };

        private readonly Engine _engine;

        private Func<object, object> _transformFunc;

        private Func<object, object> _getOptionsFunc;

        public TemplateJintPreprocessor(IResourceFileReader resourceCollection, ResourceInfo scriptResource, DocumentBuildContext context, string name = null)
        {
            if (!string.IsNullOrWhiteSpace(scriptResource.Content))
            {
                _engine = SetupEngine(resourceCollection, scriptResource, context);
            }
            else
            {
                _engine = null;
            }

            ContainsGetOptions = _getOptionsFunc != null;
            ContainsModelTransformation = _transformFunc != null;
            Path = scriptResource.Path;
            Name = name ?? System.IO.Path.GetFileNameWithoutExtension(Path);
        }

        public bool ContainsGetOptions { get; }

        public bool ContainsModelTransformation { get; }

        public string Path { get; }

        public string Name { get; }

        public object GetOptions(object model)
        {
            if (_getOptionsFunc != null)
            {
                return _getOptionsFunc(model);
            }

            return null;
        }

        public object TransformModel(object model)
        {
            if (_transformFunc != null)
            {
                return _transformFunc(model);
            }

            return model;
        }

        private Engine SetupEngine(IResourceFileReader resourceCollection, ResourceInfo scriptResource, DocumentBuildContext context)
        {
            var rootPath = (RelativePath)scriptResource.Path;
            var engineCache = new Dictionary<string, Engine>();

            var utility = new TemplateUtility(context);
            _utilityObject = new
            {
                resolveSourceRelativePath = new Func<string, string, string>(utility.ResolveSourceRelativePath),
                getHrefFromRoot = new Func<string, string, string>(utility.GetHrefFromRoot),
                markup = new Func<string, string, string>(utility.Markup),
            };

            var engine = CreateDefaultEngine();

            var requireAction = new Func<string, object>(
                s =>
                {
                    if (!s.StartsWith(RequireRelativePathPrefix, StringComparison.Ordinal))
                    {
                        throw new ArgumentException($"Only relative path starting with `{RequireRelativePathPrefix}` is supported in require");
                    }
                    var relativePath = (RelativePath)s.Substring(RequireRelativePathPrefix.Length);
                    s = relativePath.BasedOn(rootPath);

                    var script = resourceCollection?.GetResource(s);
                    if (string.IsNullOrWhiteSpace(script))
                    {
                        return null;
                    }

                    if (!engineCache.TryGetValue(s, out Engine cachedEngine))
                    {
                        cachedEngine = CreateEngine(engine, RequireFuncVariableName);
                        engineCache[s] = cachedEngine;
                        cachedEngine.Execute(script, new ParserOptions { Source = s });
                    }

                    return cachedEngine.GetValue(ExportsVariableName);
                });

            engine.SetValue(RequireFuncVariableName, requireAction);
            engineCache[rootPath] = engine;
            engine.Execute(scriptResource.Content);

            var value = engine.GetValue(ExportsVariableName);
            if (value.IsObject())
            {
                var exports = value.AsObject();
                _getOptionsFunc = GetFunc(GetOptionsFuncVariableName, exports);
                _transformFunc = GetFunc(TransformFuncVariableName, exports);
            }
            else
            {
                throw new InvalidPreprocessorException("Invalid 'exports' variable definition. 'exports' MUST be an object.");
            }

            return engine;
        }

        private Engine CreateEngine(Engine engine, params string[] sharedVariables)
        {
            var newEngine = CreateDefaultEngine();
            if (sharedVariables != null)
            {
                foreach(var sharedVariable in sharedVariables)
                {
                    newEngine.SetValue(sharedVariable, engine.GetValue(sharedVariable));
                }
            }

            return newEngine;
        }

        private Engine CreateDefaultEngine()
        {
            var engine = new Engine();

            engine.SetValue(ExportsVariableName, engine.Object.Construct(Jint.Runtime.Arguments.Empty));
            engine.SetValue(ConsoleVariableName, ConsoleObject);
            engine.SetValue(UtilityVariableName, _utilityObject);

            return engine;
        }

        private static Func<object, object> GetFunc(string funcName, ObjectInstance exports)
        {
            var func = exports.Get(funcName);
            if (func.IsUndefined() || func.IsNull())
            {
                return null;
            }
            if (func.Is<ICallable>())
            {
                return s =>
                {
                    var model = JintProcessorHelper.ConvertObjectToJsValue(s);
                    return func.Invoke(model).ToObject();
                };
            }
            else
            {
                throw new InvalidPreprocessorException($"Invalid '{funcName}' variable definition. '{funcName} MUST be a function");
            }
        }
    }
}
