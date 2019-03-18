// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Microsoft.Docs.Build
{
    internal static class ReflectionUtility
    {
        public static Func<T, TField> CreateInstanceFieldGetter<T, TField>(string fieldName)
        {
            var field = typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            var getter = new DynamicMethod(Guid.NewGuid().ToString(), typeof(TField), new[] { typeof(T) }, true);
            var il = getter.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);
            return (Func<T, TField>)getter.CreateDelegate(typeof(Func<T, TField>));
        }

        public static Action<T, TField> CreateInstanceFieldSetter<T, TField>(string fieldName)
        {
            var field = typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            var setter = new DynamicMethod(Guid.NewGuid().ToString(), null, new[] { typeof(T), typeof(TField) }, true);
            var il = setter.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);
            return (Action<T, TField>)setter.CreateDelegate(typeof(Action<T, TField>));
        }

        public static TDelegate CreateInstanceMethod<T, TDelegate>(string methodName, Type[] types = null) where TDelegate : Delegate
        {
            var methodInfo = typeof(T).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance, null, types, null);

            return (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), null, methodInfo, throwOnBindFailure: true);
        }
    }
}
