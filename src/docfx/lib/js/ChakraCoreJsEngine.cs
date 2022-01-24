// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Runtime.InteropServices;
using ChakraHost.Hosting;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

/// <summary>
/// Javascript engine based on https://github.com/microsoft/ChakraCore
/// </summary>
internal sealed class ChakraCoreJsEngine : JavaScriptEngine
{
    private static int s_currentSourceContext;

    private readonly Package _package;
    private readonly JavaScriptRuntime _runtime;
    private readonly JavaScriptContext _context;
    private readonly JavaScriptValue _global;
    private readonly JavaScriptNativeFunction _requireFunction;
    private readonly Stack<string> _dirnames = new();
    private readonly Dictionary<PathString, JavaScriptValue> _modules = new();

    public ChakraCoreJsEngine(Package package, JObject? global = null)
    {
        _package = package;
        _requireFunction = new(Require);

        var flags = JavaScriptRuntimeAttributes.DisableBackgroundWork |
                    JavaScriptRuntimeAttributes.DisableEval |
                    JavaScriptRuntimeAttributes.EnableIdleProcessing;

        _runtime = JavaScriptRuntime.Create(flags);
        _context = _runtime.CreateContext();

        if (global != null)
        {
            _global = RunInContext(() =>
            {
                var result = ToJavaScriptValue(global);
                result.AddRef();
                return result;
            });
        }
    }

    public override JToken Run(string scriptPath, string methodName, JToken arg)
    {
        return RunInContext(() =>
        {
            var exports = Run(new PathString(scriptPath));
            var method = exports.GetProperty(JavaScriptPropertyId.FromString(methodName));
            var input = ToJavaScriptValue(arg);

            if (_global.IsValid)
            {
                input.SetProperty(JavaScriptPropertyId.FromString("__global"), _global, useStrictRules: true);
            }

            var output = method.CallFunction(stackalloc JavaScriptValue[] { JavaScriptValue.Undefined, input });

            return ToJToken(output);
        });
    }

    public override void Dispose()
    {
        _runtime.Dispose();
    }

    private T RunInContext<T>(Func<T> action)
    {
        Native.ThrowIfError(Native.JsSetCurrentContext(_context));

        try
        {
            return action();
        }
        catch (JavaScriptScriptException ex) when (ex.Error.IsValid)
        {
            throw new JavaScriptEngineException($"{ex.ErrorCode}:\n{ToJToken(ex.Error)}");
        }
        finally
        {
            Native.ThrowIfError(Native.JsSetCurrentContext(JavaScriptContext.Invalid));
        }
    }

    private JavaScriptValue Run(PathString scriptPath)
    {
        if (_modules.TryGetValue(scriptPath, out var result))
        {
            return result;
        }

        var sourceCode = _package.ReadString(scriptPath);
        var exports = JavaScriptValue.CreateObject();
        var module = JavaScriptValue.CreateObject();
        var exportsProperty = JavaScriptPropertyId.FromString("exports");
        module.SetProperty(exportsProperty, exports, useStrictRules: true);

        // add `process` to input to get the correct file path while running script inside docs-ui
        var script = $@"(function (module, exports, __dirname, require, process) {{{sourceCode}
}})";
        var dirname = Path.GetDirectoryName(scriptPath) ?? "";

        _dirnames.Push(dirname);

        try
        {
            var sourceContext = JavaScriptSourceContext.FromIntPtr((IntPtr)Interlocked.Increment(ref s_currentSourceContext));

            JavaScriptContext.RunScript(script, sourceContext, scriptPath).CallFunction(stackalloc JavaScriptValue[]
            {
                JavaScriptValue.Undefined, // this pointer
                module,
                exports,
                JavaScriptValue.FromString(dirname),
                JavaScriptValue.CreateFunction(_requireFunction),
                JavaScriptValue.CreateObject(),
            });

            var moduleExports = module.GetProperty(exportsProperty);

            // Avoid exports been garbage collected by javascript garbage collector.
            moduleExports.AddRef();
            return _modules[scriptPath] = moduleExports;
        }
        finally
        {
            _dirnames.Pop();
        }
    }

    private JavaScriptValue Require(
        JavaScriptValue callee,
        [MarshalAs(UnmanagedType.U1)] bool isConstructCall,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] JavaScriptValue[] arguments,
        ushort argumentCount,
        IntPtr callbackData)
    {
        // First argument is this pointer
        if (argumentCount < 2)
        {
            return JavaScriptValue.CreateObject();
        }

        try
        {
            return Run(new PathString(Path.Combine(_dirnames.Peek(), arguments[1].ToString())));
        }
        catch (Exception ex)
        {
            Log.Write(ex);
            Native.JsSetException(JavaScriptValue.CreateError(JavaScriptValue.FromString(ex.Message)));
            return JavaScriptValue.Invalid;
        }
    }

    private static JavaScriptValue ToJavaScriptValue(JToken token)
    {
        switch (token)
        {
            case JArray arr:
                var resultArray = JavaScriptValue.CreateArray((uint)arr.Count);
                for (var i = 0; i < arr.Count; i++)
                {
                    resultArray.SetIndexedProperty(
                        JavaScriptValue.FromInt32(i), ToJavaScriptValue(arr[i]));
                }
                return resultArray;

            case JObject obj:
                var resultObj = JavaScriptValue.CreateObject();
                foreach (var (name, value) in obj)
                {
                    if (value != null)
                    {
                        resultObj.SetProperty(
                            JavaScriptPropertyId.FromString(name), ToJavaScriptValue(value), useStrictRules: true);
                    }
                }
                return resultObj;

            case JValue scalar:
                switch (scalar.Value)
                {
                    case null:
                        return JavaScriptValue.Null;

                    case bool aBool:
                        return JavaScriptValue.FromBoolean(aBool);

                    case string aString:
                        return JavaScriptValue.FromString(aString);

                    case DateTime aDate:
                        var constructor = JavaScriptValue.GlobalObject.GetProperty(JavaScriptPropertyId.FromString("Date"));
                        var args = new[] { JavaScriptValue.Undefined, JavaScriptValue.FromString(aDate.ToString("o", CultureInfo.InvariantCulture)) };
                        Native.ThrowIfError(Native.JsConstructObject(constructor, args, 2, out var date));
                        return date;

                    case long aInt:
                        return JavaScriptValue.FromInt32((int)aInt);

                    case double aDouble:
                        return JavaScriptValue.FromDouble(aDouble);
                }
                break;
        }

        throw new NotSupportedException($"Cannot marshal JToken type '{token.Type}'");
    }

    private static JToken ToJToken(JavaScriptValue value)
    {
        switch (value.ValueType)
        {
            case JavaScriptValueType.Boolean:
                return value.ToBoolean();

            case JavaScriptValueType.Null:
                return JValue.CreateNull();

            case JavaScriptValueType.Undefined:
                return JValue.CreateUndefined();

            case JavaScriptValueType.String:
                return value.ToString();

            case JavaScriptValueType.Number:
                var intNumber = value.ToInt32();
                var doubleNumber = value.ToDouble();
                return intNumber == doubleNumber ? intNumber : (JValue)doubleNumber;

            case JavaScriptValueType.Array:
                var arr = new JArray();
                var arrLength = value.GetProperty(JavaScriptPropertyId.FromString("length")).ToInt32();
                for (var i = 0; i < arrLength; i++)
                {
                    arr.Add(ToJToken(value.GetIndexedProperty(JavaScriptValue.FromInt32(i))));
                }
                return arr;

            case JavaScriptValueType.Object:
            case JavaScriptValueType.Error:
                var obj = new JObject();
                var names = value.GetOwnPropertyNames();
                var namesLength = names.GetProperty(JavaScriptPropertyId.FromString("length")).ToInt32();
                for (var i = 0; i < namesLength; i++)
                {
                    var name = names.GetIndexedProperty(JavaScriptValue.FromInt32(i)).ToString();
                    if (name != "__global")
                    {
                        obj[name] = ToJToken(value.GetProperty(JavaScriptPropertyId.FromString(name)));
                    }
                }
                return obj;

            default:
                throw new NotSupportedException($"Cannot marshal javascript type '{value.ValueType}'");
        }
    }
}
