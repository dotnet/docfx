// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Jint;
    using Jint.Native;
    using Jint.Native.Object;

    using Microsoft.DocAsCode.Common;

    public class TemplateJintPreprocessor : ITemplatePreprocessor
    {
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

        public Func<object, object> TransformModelFunc { get; private set; }

        public Func<object, object> GetOptionsFunc { get; private set; }

        public TemplateJintPreprocessor(ResourceCollection resourceCollection, TemplatePreprocessorResource scriptResource, DocumentBuildContext context)
        {
            if (!string.IsNullOrWhiteSpace(scriptResource.Content))
            {
                _engine = SetupEngine(resourceCollection, scriptResource, context);
            }
            else
            {
                _engine = null;
            }
        }

        private Engine SetupEngine(ResourceCollection resourceCollection, TemplatePreprocessorResource scriptResource, DocumentBuildContext context)
        {
            var rootPath = (RelativePath)scriptResource.ResourceName;
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
                    if (!s.StartsWith(RequireRelativePathPrefix))
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

                    Engine cachedEngine;
                    if (!engineCache.TryGetValue(s, out cachedEngine))
                    {
                        cachedEngine = CreateEngine(engine, RequireFuncVariableName);
                        engineCache[s] = cachedEngine;
                        cachedEngine.Execute(script);
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
                GetOptionsFunc = GetFunc(GetOptionsFuncVariableName, exports);
                TransformModelFunc = GetFunc(TransformFuncVariableName, exports);
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
                    var model = JintProcessorHelper.ConvertStrongTypeToJsValue(s);
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
