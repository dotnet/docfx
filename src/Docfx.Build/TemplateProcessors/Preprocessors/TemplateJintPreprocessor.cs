// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

using Acornima.Ast;

using Docfx.Common;
using Jint;
using Jint.Native;
using Jint.Native.Function;
using Jint.Native.Object;

namespace Docfx.Build.Engine;

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

    /// <summary>
    /// Wall clock budget for a single preprocessor call, that is one <c>getOptions</c> or
    /// <c>transform</c> for one document. Without it an accidental infinite loop in a template script
    /// hangs a build thread forever instead of failing the document.
    /// </summary>
    private static readonly TimeSpan ExecutionTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Statement budget for a single preprocessor call. Deliberately far above what transforming even a
    /// very large model costs, so it only ever catches a runaway script. A saturated value such as
    /// <see cref="int.MaxValue"/> would register no limit at all, so this has to be a real number.
    /// </summary>
    private const int MaxExecutionStatements = 50_000_000;

    private object _utilityObject;

    private readonly CancellationToken _cancellationToken;

    /// <summary>
    /// Parsed sources, keyed by resource path, shared by every preprocessor instance created for the same
    /// template. A <see cref="Prepared{TProgram}"/> is immutable and safe to execute from several engines
    /// and threads, so the template and everything it <c>require</c>s is parsed once instead of once per
    /// engine in the preprocessor pool.
    /// </summary>
    private readonly ConcurrentDictionary<string, Prepared<Script>> _preparedScripts;

    private static readonly object ConsoleObject = new
    {
        log = new Action<object>(s => Logger.Log(s ?? NullString)),
        info = new Action<object>(s => Logger.LogInfo((s ?? NullString).ToString())),
        warn = new Action<object>(s => Logger.LogWarning((s ?? NullString).ToString())),
        err = new Action<object>(s => Logger.LogError((s ?? NullString).ToString())),
        error = new Action<object>(s => Logger.LogError((s ?? NullString).ToString())),
    };

    private Func<object, object> _transformFunc;

    private Func<object, object> _getOptionsFunc;

    public TemplateJintPreprocessor(ResourceFileReader resourceCollection, ResourceInfo scriptResource, DocumentBuildContext context, string name = null)
        : this(resourceCollection, scriptResource, context, name, new ConcurrentDictionary<string, Prepared<Script>>())
    {
    }

    internal TemplateJintPreprocessor(
        ResourceFileReader resourceCollection,
        ResourceInfo scriptResource,
        DocumentBuildContext context,
        string name,
        ConcurrentDictionary<string, Prepared<Script>> preparedScripts)
    {
        _preparedScripts = preparedScripts;
        _cancellationToken = context?.CancellationToken ?? CancellationToken.None;

        if (!string.IsNullOrWhiteSpace(scriptResource.Content))
        {
            SetupEngine(resourceCollection, scriptResource, context);
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

    private Jint.Engine SetupEngine(ResourceFileReader resourceCollection, ResourceInfo scriptResource, DocumentBuildContext context)
    {
        var rootPath = (RelativePath)scriptResource.Path;
        var engineCache = new Dictionary<string, Jint.Engine>();

        var utility = new TemplateUtility(context);
        _utilityObject = new
        {
            resolveSourceRelativePath = new Func<string, string, string>(utility.ResolveSourceRelativePath),
            getHrefFromRoot = new Func<string, string, string>(utility.GetHrefFromRoot),
            markup = new Func<string, string, string>(utility.Markup),
        };

        // Each engine registers `require` from this delegate itself. Copying the function object out of one
        // engine and into another - which is what `CreateEngine(engine, RequireFuncVariableName)` used to do -
        // hands a JsValue to an engine that did not create it; a JsValue holds a hard reference to the engine
        // and realm that created it and passing one across is not a supported arrangement.
        object Require(string s)
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

            if (!engineCache.TryGetValue(s, out Jint.Engine cachedEngine))
            {
                cachedEngine = CreateDefaultEngine();
                cachedEngine.SetValue(RequireFuncVariableName, (Func<string, object>)Require);
                engineCache[s] = cachedEngine;
                cachedEngine.Execute(Prepare(s, script));
            }

            return cachedEngine.GetValue(ExportsVariableName);
        }

        var engine = CreateDefaultEngine();
        engine.SetValue(RequireFuncVariableName, (Func<string, object>)Require);
        engineCache[rootPath] = engine;
        engine.Execute(Prepare(scriptResource.Path, scriptResource.Content));

        var value = engine.GetValue(ExportsVariableName);
        if (value.IsObject())
        {
            var exports = value.AsObject();
            _getOptionsFunc = GetFunc(engine, GetOptionsFuncVariableName, exports);
            _transformFunc = GetFunc(engine, TransformFuncVariableName, exports);
        }
        else
        {
            throw new InvalidPreprocessorException("Invalid 'exports' variable definition. 'exports' MUST be an object.");
        }

        return engine;
    }

    /// <summary>
    /// Parses <paramref name="content"/> once per <paramref name="path"/> and reuses the result. The
    /// preprocessor pool builds one instance - and therefore one engine - per parallelism slot, and they
    /// all run the same sources, so without this the template and every module it requires would be
    /// re-parsed for each slot.
    /// </summary>
    private Prepared<Script> Prepare(string path, string content)
    {
        return _preparedScripts.GetOrAdd(path, static (source, code) => Jint.Engine.PrepareScript(code, source), content);
    }

    private Jint.Engine CreateDefaultEngine()
    {
        var utilityObject = _utilityObject;

        var engine = new Jint.Engine(options => options
            // Template scripts come from the docset being built, so a faulty one must fail the document
            // instead of pinning a render thread. All three limits apply per call into the engine.
            .TimeoutInterval(ExecutionTimeout)
            .MaxStatements(MaxExecutionStatements)
            .CancellationToken(_cancellationToken)
            // `console` and `templateUtility` are ambient conveniences that most template scripts never
            // mention, so build the wrapper only for the engines whose script actually reads the name. That
            // is worth doing here because engines are not scarce: the preprocessor pool creates one per
            // parallelism slot, and `require` creates one more per module.
            .AddLazyGlobal(ConsoleVariableName, static e => JsValue.FromObject(e, ConsoleObject))
            .AddLazyGlobal(UtilityVariableName, e => JsValue.FromObject(e, utilityObject)));

        // `exports` stays eager: it is read back off the engine after the script has run, whether or not the
        // script itself ever mentioned the name.
        engine.SetValue(ExportsVariableName, new JsObject(engine));

        return engine;
    }

    private static Func<object, object> GetFunc(Jint.Engine engine, string funcName, ObjectInstance exports)
    {
        var func = exports.Get(funcName);
        if (func.IsUndefined() || func.IsNull())
        {
            return null;
        }
        if (func is Function)
        {
            return s =>
            {
                var model = JintProcessorHelper.ConvertObjectToJsValue(engine, s);
                return engine.Invoke(func, model).ToObject();
            };
        }
        else
        {
            throw new InvalidPreprocessorException($"Invalid '{funcName}' variable definition. '{funcName} MUST be a function");
        }
    }
}
