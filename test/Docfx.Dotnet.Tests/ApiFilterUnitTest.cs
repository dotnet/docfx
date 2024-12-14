// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Xunit;

namespace Docfx.Dotnet.Tests;

[Trait("Related", "Filter")]
[Collection("docfx STA")]
public class ApiFilterUnitTest
{
    private static readonly Dictionary<string, string> EmptyMSBuildProperties = [];

    [Fact]
    public void TestApiFilter()
    {
        string code = @"
using System;
using System.ComponentModel;

namespace Test1
{
    /// <summary>
    /// This is a test
    /// </summary>
    /// <seealso cref=""Func1(int)""/>
    [Serializable]
    public class Class1
    {
        /// <summary>
        /// This is a function
        /// </summary>
        /// <param name=""i"">This is a param as <see cref=""int""/></param>
        /// <seealso cref=""int""/>
        public void Func1(int i)
        {
            return;
        }
    }

    namespace Test2
    {
        public class Class2
        {
        }
    }
    
    public class Class3
    {
        public int A { get; set; }
        internal int B { get; set; }
        public void Func2()
        {
            return;
        }
        public void Func2(int i)
        {
            return;
        }
        public class Class4
        {
            public int Func2()
            {
                return 0;
            }
        }
    }
    
    namespace Test2.Test3
    {
        public class Class5
        {
        }
    }

    public class Class6 : IFoo
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int C { get; set; }
        [EditorBrowsable(EditorBrowsableState.Always)]
        public int D { get; set; }

        void IFoo.Bar() {}

        [Obsolete(""Some text."")]
        public void ObsoleteTest()
        {
        }


        [EnumDisplay(Test = null)]
        public void Test(string a = null)
        {
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IFoo
    {
        void Bar();
    }

    public class EnumDisplayAttribute : Attribute
    {
        public string Test { get; set; } = null;
        public string Description { get; private set; }

        public EnumDisplayAttribute(string description = null)
        {
            Description = description;
        }
    }
}
";
        string configFile = "TestData/filterconfig.yml";
        MetadataItem output = Verify(code, new() { FilterConfigFile = configFile });
        Assert.Single(output.Items);
        var @namespace = output.Items[0];
        Assert.NotNull(@namespace);
        Assert.Equal("Test1", @namespace.Name);
        Assert.Equal(5, @namespace.Items.Count);
        {
            var class1 = @namespace.Items[0];
            Assert.Equal("Test1.Class1", class1.Name);
            Assert.Single(class1.Items);
            var method = class1.Items[0];
            Assert.Equal("Test1.Class1.Func1(System.Int32)", method.Name);
        }
        {
            var class3 = @namespace.Items[1];
            Assert.Equal("Test1.Class3", class3.Name);
            Assert.Equal(2, class3.Items.Count);
            Assert.Equal("Test1.Class3.Func2", class3.Items[0].Name);
            Assert.Equal("Test1.Class3.Func2(System.Int32)", class3.Items[1].Name);
        }
        {
            var class4 = @namespace.Items[2];
            Assert.Equal("Test1.Class3.Class4", class4.Name);
            Assert.Empty(class4.Items);
        }
        {
            var class6 = @namespace.Items[3];
            Assert.Equal("Test1.Class6", class6.Name);
            Assert.Equal(2, class6.Items.Count);
            Assert.Equal("Test1.Class6.D", class6.Items[0].Name);
            Assert.Equal("Test1.Class6.Test(System.String)", class6.Items[1].Name);
        }
    }

    [Fact]
    public void TestAttributeFilter()
    {
        string code = @"
using System;
using System.Runtime.InteropServices;

namespace Test1
{
    [Serializable]
    [ComVisibleAttribute(true)]
    public class Class1
    {
        public void Func1(int i)
        {
            return;
        }
    }
}";
        string configFile = "TestData/filterconfig_attribute.yml";
        MetadataItem output = Verify(code, new() { FilterConfigFile = configFile });
        var @namespace = output.Items[0];
        var class1 = @namespace.Items[0];
        Assert.Single(class1.Attributes);
        Assert.Equal("System.SerializableAttribute", class1.Attributes[0].Type);
    }

    [Fact]
    public void TestDefaultFilter()
    {
        string code = @"
using System;
using System.ComponentModel;
using System.CodeDom.Compiler;

namespace Test1
{
    [Serializable]
    [GeneratedCode(""xsd"", ""1.0.0.0"")]
    public class Class1
    {
        public void Func1(int i)
        {
            return;
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface Interface1
    {
        void Bar();
    }
}";
        MetadataItem output = Verify(code);
        var @namespace = output.Items[0];
        Assert.Single(@namespace.Items);
        var class1 = @namespace.Items[0];
        Assert.Single(class1.Attributes);
        Assert.Equal("System.SerializableAttribute", class1.Attributes[0].Type);
    }

    [Fact]
    public void TestFilterBugIssue2547()
    {
        string code = @"using System;

namespace Test1
{
    [Flags]
    public enum ExecutionMode
    {
        None = 0,
        Runtime = 1,
        Editor = 2,
        Thumbnail = 4,
        Preview = 8,
        EffectCompile = 16,
        All = Runtime | Editor | Thumbnail | Preview | EffectCompile,
    }

    public class Test1Attribute : Attribute
    {
        public ExecutionMode ExecutionMode { get; set; } = ExecutionMode.All;
    }

    [Test1(ExecutionMode = ExecutionMode.Runtime | ExecutionMode.Thumbnail | ExecutionMode.Preview)]
    public class Test2
    {
    }
}";
        MetadataItem output = Verify(code);
        var @namespace = output.Items[0];
        Assert.NotNull(@namespace);
        Assert.Equal(3, @namespace.Items.Count);
    }

    [Fact]
    public void TestSymbolFilterOptions()
    {
        var code = @"
using System;
using System.Runtime.InteropServices;

namespace Test1
{
    [Serializable]
    [ComVisible(true)]
    [A1, A2, C.A3]
    public class Class1 : IClass1
    {
        public void M1() { }
        public void M2() { }
    }

    public class A1 : Attribute { public A1() {} }
    public class A2 : Attribute { public A2() {} }
    public class C
    {
        public class A3 : Attribute { public A3() {} }
    }

    interface IClass1 { }
}";
        var output = Verify(code, new(), new() { IncludeApi = IncludeApi, IncludeAttribute = IncludeAttribute });
        var class1 = output.Items[0].Items[0];
        Assert.Equal(
            new[]
            {
                "System.SerializableAttribute",
                "System.Runtime.InteropServices.ComVisibleAttribute",
                "Test1.A2",
            },
            class1.Attributes.Select(a => a.Type));
        Assert.Equal(new[] { "Test1.Class1.M2" }, class1.Items.Select(m => m.Name));
        Assert.Equal(new[] { "System.Object" }, class1.Inheritance);

        SymbolIncludeState IncludeAttribute(ISymbol symbol)
        {
            return symbol.Name switch
            {
                "A1" or "C" => SymbolIncludeState.Exclude,
                "ComVisibleAttribute" => SymbolIncludeState.Include,
                _ => default,
            };
        }

        SymbolIncludeState IncludeApi(ISymbol symbol)
        {
            return symbol.Name switch
            {
                "M1" or "IClass1" => SymbolIncludeState.Exclude,
                _ => default,
            };
        }
    }

    [Fact]
    public void TestExcludeInterface_ExcludesExplicitInterfaceImplementations()
    {
        var code = @"
namespace Test1
{
    public class Class1 : IClass1
    {
        void IClass1.M() { }
    }

    public interface IClass1
    {
        void M();
    }
}";
        var output = Verify(
            code,
            new() { IncludePrivateMembers = true },
            new() { IncludeApi = symbol => symbol.Name is "IClass1" ? SymbolIncludeState.Exclude : default });

        var class1 = output.Items[0].Items[0];
        Assert.Empty(class1.Items);
    }

    [Fact]
    public void TestDocsSampleFilter()
    {
        var code = @"
namespace Microsoft.DevDiv
{
    public class Class1
    {
    }
}
namespace Microsoft.DevDiv.SpecialCase
{
    public class NestedClass : Class1
    {
    }
}
";
        string configFile = "TestData/filterconfig_docs_sample.yml";
        MetadataItem output = Verify(code, new() { FilterConfigFile = configFile });

        var namespaces = output.Items;
        Assert.Single(namespaces);

        var @namespace = namespaces[0];
        Assert.NotNull(@namespace);
        Assert.Equal("Microsoft.DevDiv.SpecialCase", @namespace.Name);
        Assert.Single(@namespace.Items);

        var nestedClass = @namespace.Items[0];
        Assert.Equal("Microsoft.DevDiv.SpecialCase.NestedClass", nestedClass.Name);
    }

    [Fact]
    public void TestExtendedSymbolKindFlags()
    {
        Assert.True((ExtendedSymbolKind.Type | ExtendedSymbolKind.Member).Contains(new SymbolFilterData { Kind = ExtendedSymbolKind.Interface }));
    }

    private static MetadataItem Verify(string code, ExtractMetadataConfig config = null, DotnetApiOptions options = null, IDictionary<string, string> msbuildProperties = null)
    {
        var compilation = CompilationHelper.CreateCompilationFromCSharpCode(code, msbuildProperties ?? EmptyMSBuildProperties, "test.dll");
        Assert.Empty(compilation.GetDeclarationDiagnostics());
        return compilation.Assembly.GenerateMetadataItem(compilation, config, options);
    }
}
