// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal static class ReflectionUtility
    {
        public static Func<T, TField> CreateInstanceFieldGetter<T, TField>(string fieldName)
        {
            var field = typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new InvalidOperationException();
            var getter = new DynamicMethod(Guid.NewGuid().ToString(), typeof(TField), new[] { typeof(T) }, true);
            var il = getter.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);
            return (Func<T, TField>)getter.CreateDelegate(typeof(Func<T, TField>));
        }
    }
}
