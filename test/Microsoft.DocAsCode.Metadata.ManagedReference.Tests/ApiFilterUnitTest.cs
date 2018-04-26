// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference.Tests
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Xunit;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Emit;
    using Microsoft.CodeAnalysis.MSBuild;

    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    using static Microsoft.DocAsCode.Metadata.ManagedReference.RoslynIntermediateMetadataExtractor;

    [Trait("Owner", "vwxyzh")]
    [Trait("Language", "CSharp")]
    [Trait("Related", "Filter")]
    [Collection("docfx STA")]
    public class ApiFilterUnitTest
    {
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code), options: new ExtractMetadataOptions { FilterConfigFile = configFile });
            Assert.Equal(2, output.Items.Count);
            var @namespace = output.Items[0];
            Assert.NotNull(@namespace);
            Assert.Equal("Test1", @namespace.Name);
            Assert.Equal(5, @namespace.Items.Count);
            {
                var class1 = @namespace.Items[0];
                Assert.Equal("Test1.Class1", class1.Name);
                Assert.Equal(1, class1.Items.Count);
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
                Assert.Equal(0, class4.Items.Count);
            }
            {
                var class6 = @namespace.Items[3];
                Assert.Equal("Test1.Class6", class6.Name);
                Assert.Equal(2, class6.Items.Count);
                Assert.Equal("Test1.Class6.D", class6.Items[0].Name);
                Assert.Equal("Test1.Class6.Test(System.String)", class6.Items[1].Name);
            }

            var nestedNamespace = output.Items[1];
            Assert.NotNull(nestedNamespace);
            Assert.Equal("Test1.Test2.Test3", nestedNamespace.Name);
            Assert.Equal(1, nestedNamespace.Items.Count);
            {
                var class5 = nestedNamespace.Items[0];
                Assert.Equal("Test1.Test2.Test3.Class5", class5.Name);
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code), options: new ExtractMetadataOptions { FilterConfigFile = configFile });
            var @namespace = output.Items[0];
            var class1 = @namespace.Items[0];
            Assert.Equal(1, class1.Attributes.Count);
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            var @namespace = output.Items[0];
            Assert.Equal(1, @namespace.Items.Count);
            var class1 = @namespace.Items[0];
            Assert.Equal(1, class1.Attributes.Count);
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            var @namespace = output.Items[0];
            Assert.NotNull(@namespace);
            Assert.Equal(3, @namespace.Items.Count);
        }

        private static Compilation CreateCompilationFromCSharpCode(string code, params MetadataReference[] references)
        {
            return CreateCompilationFromCSharpCode(code, "test.dll", references);
        }

        private static Compilation CreateCompilationFromCSharpCode(string code, string assemblyName, params MetadataReference[] references)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(code);
            var defaultReferences = new List<MetadataReference> { MetadataReference.CreateFromFile(typeof(object).Assembly.Location), MetadataReference.CreateFromFile(typeof(EditorBrowsableAttribute).Assembly.Location) };
            if (references != null)
            {
                defaultReferences.AddRange(references);
            }

            var compilation = CSharpCompilation.Create(
                assemblyName,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                syntaxTrees: new[] { tree },
                references: defaultReferences);
            return compilation;
        }
    }
}
