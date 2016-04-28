// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Jint;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Utility;

    public class TemplateJintPreprocessor : ITemplatePreprocessor
    {
        private const string TransformFuncVariableName = "transform";

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

        private const string ExportsVariableName = "exports";

        private static readonly object ConsoleObject = new
        {
            log = new Action<object>(s => Logger.Log(s)),
            info = new Action<object>(s => Logger.LogInfo(s.ToString())),
            warn = new Action<object>(s => Logger.LogWarning(s.ToString())),
            err = new Action<object>(s => Logger.LogError(s.ToString())),
            error = new Action<object>(s => Logger.LogError(s.ToString())),
        };

        private readonly Engine _engine;

        public TemplateJintPreprocessor(ResourceCollection resourceCollection, TemplatePreprocessorResource scriptResource)
        {
            if (!string.IsNullOrWhiteSpace(scriptResource.Content))
            {
                
                _engine = SetupEngine(resourceCollection, scriptResource);
            }
            else
            {
                _engine = null;
            }
        }

        public object Process(params object[] args)
        {
            if (_engine == null)
            {
                return args.FirstOrDefault();
            }

            var model = args.Select(s => (object)JintProcessorHelper.ConvertStrongTypeToJsValue(s)).ToArray();
            return _engine.Invoke(TransformFuncVariableName, model).ToObject();
        }

        private Engine SetupEngine(ResourceCollection resourceCollection, TemplatePreprocessorResource scriptResource)
        {
            var rootPath = (RelativePath)scriptResource.ResourceName;
            var engineCache = new Dictionary<string, Engine>();

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
            return engine;
        }

        private static Engine CreateEngine(Engine engine, params string[] sharedVariables)
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

        private static Engine CreateDefaultEngine()
        {
            var engine = new Engine();
            engine.SetValue(ExportsVariableName, engine.Object.Construct(Jint.Runtime.Arguments.Empty));
            engine.SetValue(ConsoleVariableName, ConsoleObject);
            return engine;
        }
    }
}
