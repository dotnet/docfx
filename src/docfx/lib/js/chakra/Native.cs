namespace ChakraHost.Hosting
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;

    /// <summary>
    ///     Native interfaces.
    /// </summary>
    public static class Native
    {
        /// <summary>
        /// Throws if a native method returns an error code.
        /// </summary>
        /// <param name="error">The error.</param>
        internal static void ThrowIfError(JavaScriptErrorCode error)
        {
            if (error != JavaScriptErrorCode.NoError)
            {
                switch (error)
                {
                    case JavaScriptErrorCode.InvalidArgument:
                        throw new JavaScriptUsageException(error, "Invalid argument.");

                    case JavaScriptErrorCode.NullArgument:
                        throw new JavaScriptUsageException(error, "Null argument.");

                    case JavaScriptErrorCode.NoCurrentContext:
                        throw new JavaScriptUsageException(error, "No current context.");

                    case JavaScriptErrorCode.InExceptionState:
                        throw new JavaScriptUsageException(error, "Runtime is in exception state.");

                    case JavaScriptErrorCode.NotImplemented:
                        throw new JavaScriptUsageException(error, "Method is not implemented.");

                    case JavaScriptErrorCode.WrongThread:
                        throw new JavaScriptUsageException(error, "Runtime is active on another thread.");

                    case JavaScriptErrorCode.RuntimeInUse:
                        throw new JavaScriptUsageException(error, "Runtime is in use.");

                    case JavaScriptErrorCode.BadSerializedScript:
                        throw new JavaScriptUsageException(error, "Bad serialized script.");

                    case JavaScriptErrorCode.InDisabledState:
                        throw new JavaScriptUsageException(error, "Runtime is disabled.");

                    case JavaScriptErrorCode.CannotDisableExecution:
                        throw new JavaScriptUsageException(error, "Cannot disable execution.");

                    case JavaScriptErrorCode.AlreadyDebuggingContext:
                        throw new JavaScriptUsageException(error, "Context is already in debug mode.");

                    case JavaScriptErrorCode.HeapEnumInProgress:
                        throw new JavaScriptUsageException(error, "Heap enumeration is in progress.");

                    case JavaScriptErrorCode.ArgumentNotObject:
                        throw new JavaScriptUsageException(error, "Argument is not an object.");

                    case JavaScriptErrorCode.InProfileCallback:
                        throw new JavaScriptUsageException(error, "In a profile callback.");

                    case JavaScriptErrorCode.InThreadServiceCallback:
                        throw new JavaScriptUsageException(error, "In a thread service callback.");

                    case JavaScriptErrorCode.CannotSerializeDebugScript:
                        throw new JavaScriptUsageException(error, "Cannot serialize a debug script.");

                    case JavaScriptErrorCode.AlreadyProfilingContext:
                        throw new JavaScriptUsageException(error, "Already profiling this context.");

                    case JavaScriptErrorCode.IdleNotEnabled:
                        throw new JavaScriptUsageException(error, "Idle is not enabled.");

                    case JavaScriptErrorCode.OutOfMemory:
                        throw new JavaScriptEngineException(error, "Out of memory.");

                    case JavaScriptErrorCode.ScriptException:
                        {
                            JavaScriptValue errorObject;
                            JavaScriptErrorCode innerError = JsGetAndClearException(out errorObject);

                            if (innerError != JavaScriptErrorCode.NoError)
                            {
                                throw new JavaScriptFatalException(innerError);
                            }

                            throw new JavaScriptScriptException(error, errorObject, "Script threw an exception.");
                        }

                    case JavaScriptErrorCode.ScriptCompile:
                        {
                            JavaScriptValue errorObject;
                            JavaScriptErrorCode innerError = JsGetAndClearException(out errorObject);

                            if (innerError != JavaScriptErrorCode.NoError)
                            {
                                throw new JavaScriptFatalException(innerError);
                            }

                            throw new JavaScriptScriptException(error, errorObject, "Compile error.");
                        }

                    case JavaScriptErrorCode.ScriptTerminated:
                        throw new JavaScriptScriptException(error, JavaScriptValue.Invalid, "Script was terminated.");

                    case JavaScriptErrorCode.ScriptEvalDisabled:
                        throw new JavaScriptScriptException(error, JavaScriptValue.Invalid, "Eval of strings is disabled in this runtime.");

                    case JavaScriptErrorCode.Fatal:
                        throw new JavaScriptFatalException(error);

                    default:
                        throw new JavaScriptFatalException(error);
                }
            }
        }

        const string DllName = "ChakraCore.dll";

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsCreateRuntime(JavaScriptRuntimeAttributes attributes, JavaScriptThreadServiceCallback threadService, out JavaScriptRuntime runtime);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsCollectGarbage(JavaScriptRuntime handle);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsDisposeRuntime(JavaScriptRuntime handle);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetRuntimeMemoryUsage(JavaScriptRuntime runtime, out UIntPtr memoryUsage);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetRuntimeMemoryLimit(JavaScriptRuntime runtime, out UIntPtr memoryLimit);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsSetRuntimeMemoryLimit(JavaScriptRuntime runtime, UIntPtr memoryLimit);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsSetRuntimeMemoryAllocationCallback(JavaScriptRuntime runtime, IntPtr callbackState, JavaScriptMemoryAllocationCallback allocationCallback);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsSetRuntimeBeforeCollectCallback(JavaScriptRuntime runtime, IntPtr callbackState, JavaScriptBeforeCollectCallback beforeCollectCallback);

        [DllImport(DllName, EntryPoint = "JsAddRef")]
        internal static extern JavaScriptErrorCode JsContextAddRef(JavaScriptContext reference, out uint count);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsAddRef(JavaScriptValue reference, out uint count);

        [DllImport(DllName, EntryPoint = "JsRelease")]
        internal static extern JavaScriptErrorCode JsContextRelease(JavaScriptContext reference, out uint count);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsRelease(JavaScriptValue reference, out uint count);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsCreateContext(JavaScriptRuntime runtime, out JavaScriptContext newContext);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetCurrentContext(out JavaScriptContext currentContext);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsSetCurrentContext(JavaScriptContext context);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetRuntime(JavaScriptContext context, out JavaScriptRuntime runtime);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsIdle(out uint nextIdleTick);

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        internal static extern JavaScriptErrorCode JsParseScript(string script, JavaScriptSourceContext sourceContext, string sourceUrl, out JavaScriptValue result);

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        internal static extern JavaScriptErrorCode JsRunScript(string script, JavaScriptSourceContext sourceContext, string sourceUrl, out JavaScriptValue result);

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        internal static extern JavaScriptErrorCode JsSerializeScript(string script, byte[] buffer, ref ulong bufferSize);

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        internal static extern JavaScriptErrorCode JsParseSerializedScript(string script, byte[] buffer, JavaScriptSourceContext sourceContext, string sourceUrl, out JavaScriptValue result);

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        internal static extern JavaScriptErrorCode JsRunSerializedScript(string script, byte[] buffer, JavaScriptSourceContext sourceContext, string sourceUrl, out JavaScriptValue result);

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        internal static extern JavaScriptErrorCode JsGetPropertyIdFromName(string name, out JavaScriptPropertyId propertyId);

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        internal static extern JavaScriptErrorCode JsGetPropertyNameFromId(JavaScriptPropertyId propertyId, out string name);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetUndefinedValue(out JavaScriptValue undefinedValue);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetNullValue(out JavaScriptValue nullValue);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetTrueValue(out JavaScriptValue trueValue);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetFalseValue(out JavaScriptValue falseValue);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsBoolToBoolean(bool value, out JavaScriptValue booleanValue);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsBooleanToBool(JavaScriptValue booleanValue, out bool boolValue);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsConvertValueToBoolean(JavaScriptValue value, out JavaScriptValue booleanValue);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetValueType(JavaScriptValue value, out JavaScriptValueType type);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsDoubleToNumber(double doubleValue, out JavaScriptValue value);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsIntToNumber(int intValue, out JavaScriptValue value);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsNumberToDouble(JavaScriptValue value, out double doubleValue);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsConvertValueToNumber(JavaScriptValue value, out JavaScriptValue numberValue);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetStringLength(JavaScriptValue sringValue, out int length);

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        internal static extern JavaScriptErrorCode JsPointerToString(string value, UIntPtr stringLength, out JavaScriptValue stringValue);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsStringToPointer(JavaScriptValue value, out IntPtr stringValue, out UIntPtr stringLength);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsConvertValueToString(JavaScriptValue value, out JavaScriptValue stringValue);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetGlobalObject(out JavaScriptValue globalObject);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsCreateObject(out JavaScriptValue obj);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsCreateExternalObject(IntPtr data, JavaScriptObjectFinalizeCallback finalizeCallback, out JavaScriptValue obj);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsConvertValueToObject(JavaScriptValue value, out JavaScriptValue obj);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetPrototype(JavaScriptValue obj, out JavaScriptValue prototypeObject);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsSetPrototype(JavaScriptValue obj, JavaScriptValue prototypeObject);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetExtensionAllowed(JavaScriptValue obj, out bool value);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsPreventExtension(JavaScriptValue obj);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetProperty(JavaScriptValue obj, JavaScriptPropertyId propertyId, out JavaScriptValue value);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetOwnPropertyDescriptor(JavaScriptValue obj, JavaScriptPropertyId propertyId, out JavaScriptValue propertyDescriptor);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetOwnPropertyNames(JavaScriptValue obj, out JavaScriptValue propertyNames);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsSetProperty(JavaScriptValue obj, JavaScriptPropertyId propertyId, JavaScriptValue value, bool useStrictRules);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsHasProperty(JavaScriptValue obj, JavaScriptPropertyId propertyId, out bool hasProperty);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsDeleteProperty(JavaScriptValue obj, JavaScriptPropertyId propertyId, bool useStrictRules, out JavaScriptValue result);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsDefineProperty(JavaScriptValue obj, JavaScriptPropertyId propertyId, JavaScriptValue propertyDescriptor, out bool result);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsHasIndexedProperty(JavaScriptValue obj, JavaScriptValue index, out bool result);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetIndexedProperty(JavaScriptValue obj, JavaScriptValue index, out JavaScriptValue result);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsSetIndexedProperty(JavaScriptValue obj, JavaScriptValue index, JavaScriptValue value);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsDeleteIndexedProperty(JavaScriptValue obj, JavaScriptValue index);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsEquals(JavaScriptValue obj1, JavaScriptValue obj2, out bool result);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsStrictEquals(JavaScriptValue obj1, JavaScriptValue obj2, out bool result);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsHasExternalData(JavaScriptValue obj, out bool value);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetExternalData(JavaScriptValue obj, out IntPtr externalData);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsSetExternalData(JavaScriptValue obj, IntPtr externalData);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsCreateArray(uint length, out JavaScriptValue result);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsCallFunction(JavaScriptValue function, JavaScriptValue[] arguments, ushort argumentCount, out JavaScriptValue result);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsConstructObject(JavaScriptValue function, JavaScriptValue[] arguments, ushort argumentCount, out JavaScriptValue result);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsCreateFunction(JavaScriptNativeFunction nativeFunction, IntPtr externalData, out JavaScriptValue function);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsCreateError(JavaScriptValue message, out JavaScriptValue error);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsCreateRangeError(JavaScriptValue message, out JavaScriptValue error);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsCreateReferenceError(JavaScriptValue message, out JavaScriptValue error);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsCreateSyntaxError(JavaScriptValue message, out JavaScriptValue error);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsCreateTypeError(JavaScriptValue message, out JavaScriptValue error);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsCreateURIError(JavaScriptValue message, out JavaScriptValue error);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsHasException(out bool hasException);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetAndClearException(out JavaScriptValue exception);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsSetException(JavaScriptValue exception);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsDisableRuntimeExecution(JavaScriptRuntime runtime);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsEnableRuntimeExecution(JavaScriptRuntime runtime);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsIsRuntimeExecutionDisabled(JavaScriptRuntime runtime, out bool isDisabled);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsSetObjectBeforeCollectCallback(JavaScriptValue reference, IntPtr callbackState, JavaScriptObjectBeforeCollectCallback beforeCollectCallback);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsCreateNamedFunction(JavaScriptValue name, JavaScriptNativeFunction nativeFunction, IntPtr callbackState, out JavaScriptValue function);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsSetPromiseContinuationCallback(JavaScriptPromiseContinuationCallback promiseContinuationCallback, IntPtr callbackState);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsCreateArrayBuffer(uint byteLength, out JavaScriptValue result);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsCreateTypedArray(JavaScriptTypedArrayType arrayType, JavaScriptValue arrayBuffer, uint byteOffset,
            uint elementLength, out JavaScriptValue result);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsCreateDataView(JavaScriptValue arrayBuffer, uint byteOffset, uint byteOffsetLength, out JavaScriptValue result);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetArrayBufferStorage(JavaScriptValue arrayBuffer, out IntPtr buffer, out uint bufferLength);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetTypedArrayStorage(JavaScriptValue typedArray, out IntPtr buffer, out uint bufferLength, out JavaScriptTypedArrayType arrayType, out int elementSize);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetDataViewStorage(JavaScriptValue dataView, out IntPtr buffer, out uint bufferLength);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetPropertyIdType(JavaScriptPropertyId propertyId, out JavaScriptPropertyIdType propertyIdType);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsCreateSymbol(JavaScriptValue description, out JavaScriptValue symbol);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetSymbolFromPropertyId(JavaScriptPropertyId propertyId, out JavaScriptValue symbol);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetPropertyIdFromSymbol(JavaScriptValue symbol, out JavaScriptPropertyId propertyId);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetOwnPropertySymbols(JavaScriptValue obj, out JavaScriptValue propertySymbols);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsNumberToInt(JavaScriptValue value, out int intValue);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsSetIndexedPropertiesToExternalData(JavaScriptValue obj, IntPtr data, JavaScriptTypedArrayType arrayType, uint elementLength);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetIndexedPropertiesExternalData(JavaScriptValue obj, IntPtr data, out JavaScriptTypedArrayType arrayType, out uint elementLength);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsHasIndexedPropertiesExternalData(JavaScriptValue obj, out bool value);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsInstanceOf(JavaScriptValue obj, JavaScriptValue constructor, out bool result);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsCreateExternalArrayBuffer(IntPtr data, uint byteLength, JavaScriptObjectFinalizeCallback finalizeCallback, IntPtr callbackState, out JavaScriptValue result);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetTypedArrayInfo(JavaScriptValue typedArray, out JavaScriptTypedArrayType arrayType, out JavaScriptValue arrayBuffer, out uint byteOffset, out uint byteLength);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetContextOfObject(JavaScriptValue obj, out JavaScriptContext context);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsGetContextData(JavaScriptContext context, out IntPtr data);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsSetContextData(JavaScriptContext context, IntPtr data);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsParseSerializedScriptWithCallback(JavaScriptSerializedScriptLoadSourceCallback scriptLoadCallback,
            JavaScriptSerializedScriptUnloadCallback scriptUnloadCallback, byte[] buffer, JavaScriptSourceContext sourceContext, string sourceUrl, out JavaScriptValue result);

        [DllImport(DllName)]
        internal static extern JavaScriptErrorCode JsRunSerializedScriptWithCallback(JavaScriptSerializedScriptLoadSourceCallback scriptLoadCallback,
            JavaScriptSerializedScriptUnloadCallback scriptUnloadCallback, byte[] buffer, JavaScriptSourceContext sourceContext, string sourceUrl, out JavaScriptValue result);
    }
}
