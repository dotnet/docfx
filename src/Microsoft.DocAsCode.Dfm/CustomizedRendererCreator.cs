// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Threading;

    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Common;

    public static class CustomizedRendererCreator
    {
        #region Fields
        private static readonly ModuleBuilder _module = CreateModule();
        private static readonly MethodInfo MatchMethod = typeof(IDfmCustomizedRendererPart).GetMethod(nameof(IDfmCustomizedRendererPart.Match));
        private static readonly MethodInfo RenderMethod = typeof(IDfmCustomizedRendererPart).GetMethod(nameof(IDfmCustomizedRendererPart.Render));
        private static readonly ConstructorInfo BaseConstructor = typeof(PlugableRendererBase).GetConstructor(new[] { typeof(object) });
        private static readonly MethodInfo BaseRenderMethod = typeof(PlugableRendererBase).GetMethod(nameof(PlugableRendererBase.BaseRender));
        private static readonly MethodInfo DisposeMethod = typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose));
        private static readonly MethodInfo BaseDisposeMethod = typeof(PlugableRendererBase).GetMethod(nameof(PlugableRendererBase.Dispose));
        private static int _typeCounter;
        #endregion

        #region Public Methods

        public static object CreateRenderer(
            object innerRenderer,
            IEnumerable<IDfmCustomizedRendererPartProvider> partProviders,
            IReadOnlyDictionary<string, object> parameters)
        {
            if (innerRenderer == null)
            {
                throw new ArgumentNullException(nameof(innerRenderer));
            }
            if (partProviders == null)
            {
                return innerRenderer;
            }
            return CreateRendererNoCheck(innerRenderer, partProviders, parameters ?? ImmutableDictionary<string, object>.Empty);
        }

        #endregion

        #region Private Methods

        private static ModuleBuilder CreateModule()
        {
            const string name = "dfm-renderer";
            var dynamicAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName(name),
                AssemblyBuilderAccess.Run);
            return dynamicAssembly.DefineDynamicModule(name);
        }

        private static object CreateRendererNoCheck(
            object innerRenderer,
            IEnumerable<IDfmCustomizedRendererPartProvider> partProviders,
            IReadOnlyDictionary<string, object> parameters)
        {
            var partGroups = (from provider in partProviders
                              from part in provider.CreateParts(parameters)
                              select new
                              {
                                  part,
                                  types = Tuple.Create(part.MarkdownRendererType, part.MarkdownTokenType, part.MarkdownContextType)
                              } into info
                              group info.part by info.types into g
                              select new { g.Key, Items = g, IsValid = ValidateKey(g.Key) }).ToList();
            if (partGroups.Any(g => !g.IsValid))
            {
                Logger.LogWarning($@"Ignore invalid renderer parts: {
                    string.Join(
                        ", ",
                        from g in partGroups
                        where !g.IsValid
                        from item in g.Items
                        select item.Name)}.");
            }
            if (!partGroups.Any(g => g.IsValid))
            {
                return innerRenderer;
            }
            var type = _module.DefineType("renderer-t" + Interlocked.Increment(ref _typeCounter).ToString(), TypeAttributes.Public | TypeAttributes.Class, typeof(PlugableRendererBase));
            var f = type.DefineField("f", typeof(IDfmCustomizedRendererPart[]), FieldAttributes.Private | FieldAttributes.InitOnly);
            DefineConstructor(type, f);
            List<IDfmCustomizedRendererPart> partList = new List<IDfmCustomizedRendererPart>();
            foreach (var g in from g in partGroups
                              where g.IsValid
                              select g)
            {
                DefineMethod(type, g.Key, g.Items, f, partList);
            }
            OverrideDispose(type, f, partList.Count);
            return Activator.CreateInstance(type.CreateType(), innerRenderer, partList.ToArray());
        }

        private static bool ValidateKey(Tuple<Type, Type, Type> types)
        {
            return ValidateType(types.Item1, typeof(IMarkdownRenderer)) &&
                ValidateType(types.Item2, typeof(IMarkdownToken)) &&
                ValidateType(types.Item3, typeof(IMarkdownContext));
        }

        private static bool ValidateType(Type t, Type expected)
        {
            if (t == null || t == typeof(void) || t.IsValueType)
            {
                return false;
            }
            return t.IsVisible && expected.IsAssignableFrom(t);
        }

        private static void DefineConstructor(TypeBuilder type, FieldBuilder f)
        {
            // public .ctor(object baseRenderer, IDfmRendererPart[] parts)
            var ctor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new[] { typeof(object), typeof(IDfmCustomizedRendererPart[]) });
            var il = ctor.GetILGenerator();
            //  : base(baseRenderer)
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, BaseConstructor);
            // { f = parts; }
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Stfld, f);
            il.Emit(OpCodes.Ret);
        }

        private static void DefineMethod(
            TypeBuilder hostType,
            Tuple<Type, Type, Type> types,
            IEnumerable<IDfmCustomizedRendererPart> parts,
            FieldBuilder f,
            List<IDfmCustomizedRendererPart> partList)
        {
            const string MethodName = "Render";
            const MethodAttributes MethodAttr = MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.HideBySig;
            var method = hostType.DefineMethod(MethodName, MethodAttr, typeof(StringBuffer), new[] { types.Item1, types.Item2, types.Item3 });
            var il = method.GetILGenerator();
            foreach (var part in parts)
            {
                // if (!f[i].Match(r, t, c)) continue;
                var next = il.DefineLabel();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, f);
                il.Emit(OpCodes.Ldc_I4, partList.Count);
                il.Emit(OpCodes.Ldelem_Ref);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Callvirt, MatchMethod);
                il.Emit(OpCodes.Brfalse, next);
                // return f[i].Render(r, t, c);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, f);
                il.Emit(OpCodes.Ldc_I4, partList.Count);
                il.Emit(OpCodes.Ldelem_Ref);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Callvirt, RenderMethod);
                il.Emit(OpCodes.Ret);
                // :next
                il.MarkLabel(next);

                partList.Add(part);
            }
            // return this.BaseRender(r, t, c);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_3);
            il.Emit(OpCodes.Callvirt, BaseRenderMethod);
            il.Emit(OpCodes.Ret);
        }

        private static void OverrideDispose(TypeBuilder hostType, FieldBuilder f, int partCount)
        {
            const string MethodName = nameof(IDisposable.Dispose);
            const MethodAttributes MethodAttr = MethodAttributes.ReuseSlot | MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.HideBySig;
            var method = hostType.DefineMethod(MethodName, MethodAttr, typeof(void), Type.EmptyTypes);
            var il = method.GetILGenerator();
            il.DeclareLocal(typeof(IDisposable));
            for (int i = 0; i < partCount; i++)
            {
                var next = il.DefineLabel();
                // var temp = f[i] as IDisposable;
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, f);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldelem_Ref);
                il.Emit(OpCodes.Isinst, typeof(IDisposable));
                il.Emit(OpCodes.Stloc_0);
                // if (temp != null) temp.Dispose();
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Brfalse, next);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Callvirt, DisposeMethod);
                il.MarkLabel(next);
            }
            // base.Dispose();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, BaseDisposeMethod);
            il.Emit(OpCodes.Ret);
        }
        #endregion
    }
}
