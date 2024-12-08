// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.CodeAnalysis;

#nullable enable

namespace Docfx.Dotnet;

internal static class ISymbolExtensions
{
    public static ImmutableArray<IParameterSymbol> GetParameters(this ISymbol? symbol)
    {
        return symbol switch
        {
            IMethodSymbol m => m.Parameters,
            IPropertySymbol nt => nt.Parameters,
            _ => [],
        };
    }

    public static ImmutableArray<ITypeParameterSymbol> GetTypeParameters(this ISymbol? symbol)
    {
        return symbol switch
        {
            IMethodSymbol m => m.TypeParameters,
            INamedTypeSymbol nt => nt.TypeParameters,
            _ => [],
        };
    }

    public static DocumentationComment GetDocumentationComment(this ISymbol symbol, Compilation compilation, CultureInfo? preferredCulture = null, bool expandIncludes = false, bool expandInheritdoc = false, CancellationToken cancellationToken = default)
    {
        // Gets FullXmlFragment by calling `symbol.DocumentationComment(...).FullXmlFragment`
        string fullXmlFragment = Helpers.GetFullXmlFragment(symbol, compilation, preferredCulture, expandIncludes, expandInheritdoc, cancellationToken);

        return new DocumentationComment
        {
            FullXmlFragment = fullXmlFragment,
        };
    }

    internal class DocumentationComment
    {
        public required string FullXmlFragment { get; init; }
    }

    private static class Helpers
    {
        /// <summary>
        /// Gets result of `symbol.GetDocumentationComment(args).FullXmlFragment`
        /// </summary>
        public static string GetFullXmlFragment(ISymbol symbol, Compilation compilation, CultureInfo? preferredCulture = null, bool expandIncludes = false, bool expandInheritdoc = false, CancellationToken cancellationToken = default)
          => CachedDelegate(symbol, compilation, preferredCulture, expandIncludes, expandInheritdoc, cancellationToken);

        static Helpers()
        {
            CachedDelegate = GetDelegate();
        }

        private delegate string GetFullXmlFragmentDelegate(ISymbol symbol, Compilation compilation, CultureInfo? preferredCulture, bool expandIncludes, bool expandInheritdoc, CancellationToken cancellationToken);
        private static readonly GetFullXmlFragmentDelegate CachedDelegate;

        private static GetFullXmlFragmentDelegate GetDelegate()
        {
            // Gets Microsoft.CodeAnalysis.Workspaces assembly
            var workspaceAssembly = typeof(Workspace).Assembly;

            // Gets MethodInfo for GetDocumentationComment
            var type = workspaceAssembly.GetType("Microsoft.CodeAnalysis.Shared.Extensions.ISymbolExtensions", throwOnError: true)!;
            var methodInfo = type.GetMethod("GetDocumentationComment", BindingFlags.Public | BindingFlags.Static);

            // Gets PropertyInfo for DocumentationComment.
            var docCommentType = workspaceAssembly.GetType("Microsoft.CodeAnalysis.Shared.Utilities.DocumentationComment", throwOnError: true)!;
            var propertyInfo = docCommentType.GetProperty("FullXmlFragment", BindingFlags.Instance | BindingFlags.Public)!;

            // Reflection may fail when updating the Microsoft.CodeAnalysis.Workspaces.Common package..
            if (methodInfo == null || propertyInfo == null)
                throw new InvalidOperationException("Failed to get required MethodInfo/PropertyInfo via reflection.");

            var dm = new DynamicMethod(string.Empty, returnType: typeof(string), parameterTypes: [
                typeof(ISymbol),
                typeof(Compilation),
                typeof(CultureInfo), // preferredCulture
                typeof(bool),        // expandIncludes
                typeof(bool),        // expandInheritdoc
                typeof(CancellationToken),
            ]);

            ILGenerator il = dm.GetILGenerator();

            // call Microsoft.CodeAnalysis.Shared.Extensions.ISymbolExtensions::GetDocumentationComment(args)
            il.Emit(OpCodes.Ldarg_0);    // symbol
            il.Emit(OpCodes.Ldarg_1);    // compilation
            il.Emit(OpCodes.Ldarg_2);    // preferredCulture
            il.Emit(OpCodes.Ldarg_3);    // expandIncludes
            il.Emit(OpCodes.Ldarg_S, 4); // expandInheritdoc
            il.Emit(OpCodes.Ldarg_S, 5); // cancellationToken
            il.EmitCall(OpCodes.Call, methodInfo, null);

            // callvirt DocumentationComment::get_FullXmlFragment()
            il.EmitCall(OpCodes.Callvirt, propertyInfo.GetMethod!, null);

            // return FullXmlFragment
            il.Emit(OpCodes.Ret);

            return dm.CreateDelegate<GetFullXmlFragmentDelegate>();
        }
    }
}
