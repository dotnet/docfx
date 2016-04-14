// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Linq;

    using Jint;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Utility;

    public class TemplateJintPreprocessor : ITemplatePreprocessor
    {
        private const string TransformFuncVariableName = "transform";
        private const string ConsoleVariableName = "console";

        private static readonly object ConsoleObject = new
        {
            log = new Action<object>(s => Logger.Log(s)),
            info = new Action<object>(s => Logger.LogInfo(s.ToString())),
            warn = new Action<object>(s => Logger.LogWarning(s.ToString())),
            err = new Action<object>(s => Logger.LogError(s.ToString())),
            error = new Action<object>(s => Logger.LogError(s.ToString())),
        };

        private readonly Engine _engine;

        public TemplateJintPreprocessor(string script)
        {
            if (!string.IsNullOrWhiteSpace(script))
            {
                var engine = new Engine();

                engine.SetValue(ConsoleVariableName, ConsoleObject);

                engine.Execute(script);
                _engine = engine;
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
    }
}
