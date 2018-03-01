// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference.Tests
{
    using System.Linq;

    using CodeAnalysis;
    using CodeAnalysis.CSharp;
    using Xunit;

    [Trait("Owner", "xuzho")]
    [Trait("Related", "CodeAnalysisSymbolExtension")]
    public class CodeAnalysisSymbolExtensionUnitTest
    {
        [Fact]
        public void TestFindSymbol()
        {
            string code1 = @"
namespace outer.inner
{
    public class Monkey<T>
    { }
    public class Monkey<U,V>
    { }
    public interface IEii<T>
    {
        void Rain(T t);
    }
}";
            string code2 = @"
namespace ext
{
    using outer.inner;
    public class Eii<T> : IEii<T>
    {
        void IEii<T>.Rain(T t)
        {
        }

        public void Rain(T t)
        {
        }
    }
    public static class Extension
    {
        public static void Eat<T>(this Monkey<T> monkey)
        { }
        public static void Eat<U,V>(this Monkey<U,V> monkey)
        { }
    }
}";
            var c1 = CSharpCompilation.Create("C1",
                syntaxTrees: new[] { SyntaxFactory.ParseSyntaxTree(code1) },
                references: null,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var c2 = CSharpCompilation.Create("C2",
                syntaxTrees: new[] { SyntaxFactory.ParseSyntaxTree(code2) },
                references: new[] { c1.ToMetadataReference() },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var @space1 = c1.Assembly.GlobalNamespace.GetNamespaceMembers().Single(n => n.Name == "outer").GetNamespaceMembers().Single(n => n.Name == "inner");
            var @space2 = c2.Assembly.GlobalNamespace.GetNamespaceMembers().Single(n => n.Name == "ext");
            var @type1 = @space1.GetTypeMembers().ToList();
            var @type2 = @space2.GetTypeMembers().ToList();
            {
                var method = @type2[0].GetMembers("outer.inner.IEii<T>.Rain").OfType<IMethodSymbol>().Single();
                var found = c2.FindSymbol<IMethodSymbol>(method);
                Assert.True(found != null);
                Assert.Equal("outer.inner.IEii<T>.Rain", found.Name);
                Assert.Equal(found, method);
            }
            {
                var methods = @type2[1].GetMembers("Eat").OfType<IMethodSymbol>().ToList();
                Assert.Equal(2, methods.Count());
                var found = c2.FindSymbol<INamedTypeSymbol>(@type1[1]);
                Assert.Equal("Monkey", found.Name);
                Assert.Equal(found, methods[1].Parameters[0].Type.OriginalDefinition);
            }
        }
    }
}
