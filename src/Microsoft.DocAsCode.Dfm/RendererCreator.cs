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

    public static class RendererCreator
    {
        #region Fields
        private static readonly ModuleBuilder _module = CreateModule();
        private static readonly MethodInfo MatchMethod = typeof(IDfmRendererPart).GetMethod(nameof(IDfmRendererPart.Match));
        private static readonly MethodInfo RenderMethod = typeof(IDfmRendererPart).GetMethod(nameof(IDfmRendererPart.Render));
        private static readonly ConstructorInfo BaseConstructor = typeof(PlugableRendererBase).GetConstructor(new[] { typeof(object) });
        private static readonly MethodInfo BaseRenderMethod = typeof(PlugableRendererBase).GetMethod(nameof(PlugableRendererBase.BaseRender));
        private static int _typeCounter;
        #endregion

        #region Public Methods

        public static object CreateRenderer(object baseRenderer, IEnumerable<IDfmRendererPartProvider> partProviders, IReadOnlyDictionary<string, object> parameters)
        {
            if (baseRenderer == null)
            {
                throw new ArgumentNullException(nameof(baseRenderer));
            }
            if (partProviders == null)
            {
                return baseRenderer;
            }
            return CreateRendererNoCheck(baseRenderer, partProviders, parameters ?? ImmutableDictionary<string, object>.Empty);
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

        private static object CreateRendererNoCheck(object baseRenderer, IEnumerable<IDfmRendererPartProvider> partProviders, IReadOnlyDictionary<string, object> parameters)
        {
            var partGroups = from provider in partProviders
                             from part in provider.CreateParts(parameters)
                             select new
                             {
                                 part,
                                 types = Tuple.Create(part.MarkdownRendererType, part.MarkdownTokenType, part.MarkdownContextType)
                             } into info
                             group info.part by info.types;
            if (!partGroups.Any())
            {
                return baseRenderer;
            }
            foreach (var item in from g in partGroups
                                 where !ValidateKey(g.Key)
                                 from item in g
                                 select item)
            {
                Logger.LogWarning($"Ignore invalid renderer part: {item.Name}.");
            }
            var type = _module.DefineType("renderer-t" + Interlocked.Increment(ref _typeCounter).ToString(), TypeAttributes.Public | TypeAttributes.Class, typeof(PlugableRendererBase));
            var f = type.DefineField("f", typeof(IDfmRendererPart[]), FieldAttributes.Private | FieldAttributes.InitOnly);
            DefineConstructor(type, f);
            List<IDfmRendererPart> partList = new List<IDfmRendererPart>();
            foreach (var g in from g in partGroups
                              where ValidateKey(g.Key)
                              select g)
            {
                DefineMethod(type, g.Key, g, f, partList);
            }
            return Activator.CreateInstance(type.CreateType(), baseRenderer, partList.ToArray());
        }

        private static bool ValidateKey(Tuple<Type, Type, Type> types)
        {
            return types.Item1 != null && types.Item2 != null && types.Item3 != null &&
                types.Item1 != typeof(void) && types.Item2 != typeof(void) && types.Item3 != typeof(void);
        }

        private static void DefineConstructor(TypeBuilder type, FieldBuilder f)
        {
            // public .ctor(object baseRenderer, IDfmRendererPart[] parts)
            var ctor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new[] { typeof(object), typeof(IDfmRendererPart[]) });
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

        private static void DefineMethod(TypeBuilder hostType, Tuple<Type, Type, Type> types, IEnumerable<IDfmRendererPart> parts, FieldBuilder f, List<IDfmRendererPart> partList)
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

        #endregion

        #region PlugableRendererBase class

        public abstract class PlugableRendererBase
        {
            private readonly object _baseRenderer;

            public PlugableRendererBase(object baseRenderer)
            {
                _baseRenderer = baseRenderer;
            }

            public StringBuffer Render(IMarkdownRenderer renderer, IMarkdownToken token, IMarkdownContext context)
            {
                return BaseRender(renderer, token, context);
            }

            public StringBuffer BaseRender(IMarkdownRenderer renderer, IMarkdownToken token, IMarkdownContext context)
            {
                // double dispatch.
                return ((dynamic)_baseRenderer).Render((dynamic)renderer, (dynamic)token, (dynamic)context);
            }
        }

        #endregion
    }
}
