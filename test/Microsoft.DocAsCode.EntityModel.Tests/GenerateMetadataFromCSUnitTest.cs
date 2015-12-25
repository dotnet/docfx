// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Tests
{
    using DocAsCode.EntityModel;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Emit;
    using Microsoft.CodeAnalysis.MSBuild;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Xunit;
    using static DocAsCode.EntityModel.ExtractMetadataWorker;

    [Trait("Owner", "vwxyzh")]
    [Trait("Language", "CSharp")]
    [Trait("EntityType", "Model")]
    public class GenerateMetadataFromCSUnitTest
    {
        private static readonly MSBuildWorkspace Workspace = MSBuildWorkspace.Create();

        [Fact]
        public void TestGenereateMetadataAsyncWithFuncVoidReturn()
        {
            string code = @"
namespace Test1
{
    public class Class1
    {
        public void Func1(int i)
        {
            return;
        }
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            var function = output.Items[0].Items[0].Items[0];
            Assert.NotNull(function);
            Assert.Equal("Func1(Int32)", function.DisplayNames.First().Value);
            Assert.Equal("Test1.Class1.Func1(System.Int32)", function.DisplayQualifiedNames.First().Value);
            Assert.Equal("Test1.Class1.Func1(System.Int32)", function.Name);
            Assert.Equal(1, output.Items.Count);
            var parameter = function.Syntax.Parameters[0];
            Assert.Equal("i", parameter.Name);
            Assert.Equal("System.Int32", parameter.Type);
            var returnValue = function.Syntax.Return;
            Assert.Null(returnValue);
        }

        [Fact]
        public void TestGenereateMetadataAsyncWithNamespace()
        {
            string code = @"
namespace Test1.Test2
{
    public class Class1
    {
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Equal(1, output.Items.Count);
            var ns = output.Items[0];
            Assert.NotNull(ns);
            Assert.Equal("Test1.Test2", ns.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.Equal("Test1.Test2", ns.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
        }

        [Trait("Related", "Generic")]
        [Trait("Related", "Reference")]
        [Trait("Related", "TripleSlashComments")]
        [Fact]
        public void TestGenereateMetadataWithGenericClass()
        {
            string code = @"
using System.Collections.Generic
namespace Test1
{
    /// <summary>
    /// class1 <see cref=""Dictionary{TKey,TValue}""/>
    /// </summary>
    /// <typeparam name=""T"">The type</typeparam>
    public sealed class Class1<T> where T : struct, IEnumerable<T>
    {
        public TResult? Func1<TResult>(T? x, IEnumerable<T> y) where TResult : struct
        {
            return null;
        }
        public IEnumerable<T> Items { get; set; }
        public event EventHandler Event1;
        public static bool operator ==(Class1<T> x, Class1<T> y) { return false; }
        public IEnumerable<T> Items2 { get; private set; }
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            MetadataItem output_preserveRaw = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code), true);
            Assert.Equal(1, output.Items.Count);
            {
                var type = output.Items[0].Items[0];
                Assert.NotNull(type);
                Assert.Equal("Class1<T>", type.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Class1<T>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Class1`1", type.Name);
                Assert.Equal(@"public sealed class Class1<T>
    where T : struct, IEnumerable<T>", type.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.NotNull(type.Syntax.TypeParameters);
                Assert.Equal(1, type.Syntax.TypeParameters.Count);
                Assert.Equal("T", type.Syntax.TypeParameters[0].Name);
                Assert.Null(type.Syntax.TypeParameters[0].Type);
                Assert.Equal("The type", type.Syntax.TypeParameters[0].Description);
            }
            {
                var function = output.Items[0].Items[0].Items[0];
                Assert.NotNull(function);
                Assert.Equal("Func1<TResult>(Nullable<T>, IEnumerable<T>)", function.DisplayNames.First().Value);
                Assert.Equal("Test1.Class1<T>.Func1<TResult>(System.Nullable<T>, System.Collections.Generic.IEnumerable<T>)", function.DisplayQualifiedNames.First().Value);
                Assert.Equal("Test1.Class1`1.Func1``1(System.Nullable{`0},System.Collections.Generic.IEnumerable{`0})", function.Name);

                var parameterX = function.Syntax.Parameters[0];
                Assert.Equal("x", parameterX.Name);
                Assert.Equal("System.Nullable{{T}}", parameterX.Type);

                var parameterY = function.Syntax.Parameters[1];
                Assert.Equal("y", parameterY.Name);
                Assert.Equal("System.Collections.Generic.IEnumerable{{T}}", parameterY.Type);

                var returnValue = function.Syntax.Return;
                Assert.NotNull(returnValue);
                Assert.NotNull(returnValue.Type);
                Assert.Equal("System.Nullable{{TResult}}", returnValue.Type);
                Assert.Equal(@"public TResult? Func1<TResult>(T? x, IEnumerable<T> y)where TResult : struct", function.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var proptery = output.Items[0].Items[0].Items[1];
                Assert.NotNull(proptery);
                Assert.Equal("Items", proptery.DisplayNames.First().Value);
                Assert.Equal("Test1.Class1<T>.Items", proptery.DisplayQualifiedNames.First().Value);
                Assert.Equal("Test1.Class1`1.Items", proptery.Name);
                Assert.Equal(0, proptery.Syntax.Parameters.Count);
                var returnValue = proptery.Syntax.Return;
                Assert.NotNull(returnValue.Type);
                Assert.Equal("System.Collections.Generic.IEnumerable{{T}}", returnValue.Type);
                Assert.Equal(@"public IEnumerable<T> Items
{
    get;
    set;
}", proptery.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var event1 = output.Items[0].Items[0].Items[2];
                Assert.NotNull(event1);
                Assert.Equal("Event1", event1.DisplayNames.First().Value);
                Assert.Equal("Test1.Class1<T>.Event1", event1.DisplayQualifiedNames.First().Value);
                Assert.Equal("Test1.Class1`1.Event1", event1.Name);
                Assert.Null(event1.Syntax.Parameters);
                Assert.Null(event1.Syntax.Return);
                Assert.Equal("public event EventHandler Event1", event1.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var operator1 = output.Items[0].Items[0].Items[3];
                Assert.NotNull(operator1);
                Assert.Equal("Equality(Class1<T>, Class1<T>)", operator1.DisplayNames.First().Value);
                Assert.Equal("Test1.Class1<T>.Equality(Test1.Class1<T>, Test1.Class1<T>)", operator1.DisplayQualifiedNames.First().Value);
                Assert.Equal("Test1.Class1`1.op_Equality(Test1.Class1{`0},Test1.Class1{`0})", operator1.Name);
                Assert.NotNull(operator1.Syntax.Parameters);

                var parameterX = operator1.Syntax.Parameters[0];
                Assert.Equal("x", parameterX.Name);
                Assert.Equal("Test1.Class1`1", parameterX.Type);

                var parameterY = operator1.Syntax.Parameters[1];
                Assert.Equal("y", parameterY.Name);
                Assert.Equal("Test1.Class1`1", parameterY.Type);

                Assert.NotNull(operator1.Syntax.Return);
                Assert.Equal("System.Boolean", operator1.Syntax.Return.Type);

                Assert.Equal("public static bool operator ==(Class1<T> x, Class1<T> y)", operator1.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var proptery = output.Items[0].Items[0].Items[4];
                Assert.NotNull(proptery);
                Assert.Equal("Items2", proptery.DisplayNames.First().Value);
                Assert.Equal("Test1.Class1<T>.Items2", proptery.DisplayQualifiedNames.First().Value);
                Assert.Equal("Test1.Class1`1.Items2", proptery.Name);
                Assert.Equal(0, proptery.Syntax.Parameters.Count);
                var returnValue = proptery.Syntax.Return;
                Assert.NotNull(returnValue.Type);
                Assert.Equal("System.Collections.Generic.IEnumerable{{T}}", returnValue.Type);
                Assert.Equal(@"public IEnumerable<T> Items2
{
    get;
}", proptery.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            // check references
            {
                Assert.NotNull(output.References);
                Assert.True(output.References.Count > 0);

                Assert.True(output.References.ContainsKey("Test1.Class1`1"));
                var refenence = output.References["Test1.Class1`1"];
                Assert.Equal(true, refenence.IsDefinition);
                Assert.Equal("Test1", refenence.Parent);
                Assert.True(output.References.ContainsKey("Test1"));
                refenence = output.References["Test1"];
                Assert.Equal(true, refenence.IsDefinition);
                Assert.Null(refenence.Parent);

                Assert.True(output.References.ContainsKey("System.Collections.Generic.Dictionary`2"));
                Assert.NotNull(output.References["System.Collections.Generic.Dictionary`2"]);
                Assert.True(output.Items[0].Items[0].References.ContainsKey("System.Collections.Generic.Dictionary`2"));
                Assert.Null(output.Items[0].Items[0].References["System.Collections.Generic.Dictionary`2"]);
            }
        }

        [Fact]
        public void TestGenereateMetadataWithInterface()
        {
            string code = @"
namespace Test1
{
    public interface IFoo
    {
        string Bar(int x);
        int Count { get; }
        event EventHandler FooBar;
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Equal(1, output.Items.Count);
            {
                var method = output.Items[0].Items[0].Items[0];
                Assert.NotNull(method);
                Assert.Equal("Bar(Int32)", method.DisplayNames.First().Value);
                Assert.Equal("Test1.IFoo.Bar(System.Int32)", method.DisplayQualifiedNames.First().Value);
                Assert.Equal("Test1.IFoo.Bar(System.Int32)", method.Name);
                var parameter = method.Syntax.Parameters[0];
                Assert.Equal("x", parameter.Name);
                Assert.Equal("System.Int32", parameter.Type);
                var returnValue = method.Syntax.Return;
                Assert.NotNull(returnValue);
                Assert.NotNull(returnValue.Type);
                Assert.Equal("System.String", returnValue.Type);
            }
            {
                var property = output.Items[0].Items[0].Items[1];
                Assert.NotNull(property);
                Assert.Equal("Count", property.DisplayNames.First().Value);
                Assert.Equal("Test1.IFoo.Count", property.DisplayQualifiedNames.First().Value);
                Assert.Equal("Test1.IFoo.Count", property.Name);
                Assert.Equal(0, property.Syntax.Parameters.Count);
                var returnValue = property.Syntax.Return;
                Assert.NotNull(returnValue);
                Assert.NotNull(returnValue.Type);
                Assert.Equal("System.Int32", returnValue.Type);
            }
            {
                var @event = output.Items[0].Items[0].Items[2];
                Assert.NotNull(@event);
                Assert.Equal("FooBar", @event.DisplayNames.First().Value);
                Assert.Equal("Test1.IFoo.FooBar", @event.DisplayQualifiedNames.First().Value);
                Assert.Equal("Test1.IFoo.FooBar", @event.Name);
                Assert.Equal("event EventHandler FooBar", @event.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Null(@event.Syntax.Parameters);
                Assert.Null(@event.Syntax.Return);
            }
        }

        [Fact]
        public void TestGenereateMetadataWithInterfaceAndInherits()
        {
            string code = @"
namespace Test1
{
    public interface IFoo { }
    public interface IBar : IFoo { }
    public interface IFooBar : IBar { }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Equal(1, output.Items.Count);

            var ifoo = output.Items[0].Items[0];
            Assert.NotNull(ifoo);
            Assert.Equal("IFoo", ifoo.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.Equal("Test1.IFoo", ifoo.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.Equal("public interface IFoo", ifoo.Syntax.Content[SyntaxLanguage.CSharp]);

            var ibar = output.Items[0].Items[1];
            Assert.NotNull(ibar);
            Assert.Equal("IBar", ibar.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.Equal("Test1.IBar", ibar.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.Equal("public interface IBar : IFoo", ibar.Syntax.Content[SyntaxLanguage.CSharp]);

            var ifoobar = output.Items[0].Items[2];
            Assert.NotNull(ifoobar);
            Assert.Equal("IFooBar", ifoobar.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.Equal("Test1.IFooBar", ifoobar.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.Equal("public interface IFooBar : IBar, IFoo", ifoobar.Syntax.Content[SyntaxLanguage.CSharp]);
        }

        [Trait("Related", "Generic")]
        [Trait("Related", "Inheritance")]
        [Trait("Related", "Reference")]
        [Fact]
        public void TestGenereateMetadataWithClassAndInherits()
        {
            string code = @"
namespace Test1
{
    public class Foo<T> : IFoo { }
    public class Bar<T> : Foo<T[]>, IBar { }
    public class FooBar : Bar<string>, IFooBar { }
    public interface IFoo { }
    public interface IBar { }
    public interface IFooBar : IFoo, IBar { }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Equal(1, output.Items.Count);

            var foo = output.Items[0].Items[0];
            Assert.NotNull(foo);
            Assert.Equal("Foo<T>", foo.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.Equal("Test1.Foo<T>", foo.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.Equal("public class Foo<T> : IFoo", foo.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.NotNull(foo.Implements);
            Assert.Equal(1, foo.Implements.Count);
            Assert.Equal(new[] { "Test1.IFoo" }, foo.Implements);


            var bar = output.Items[0].Items[1];
            Assert.NotNull(bar);
            Assert.Equal("Bar<T>", bar.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.Equal("Test1.Bar<T>", bar.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.Equal("public class Bar<T> : Foo<T[]>, IFoo, IBar", bar.Syntax.Content[SyntaxLanguage.CSharp]);
            Console.WriteLine(string.Join(",", bar.Inheritance));
            Assert.Equal(new[] { "System.Object", "Test1.Foo{{T}[]}" }, bar.Inheritance);
            Assert.Equal(new[] { "Test1.IFoo", "Test1.IBar" }, bar.Implements);

            var foobar = output.Items[0].Items[2];
            Assert.NotNull(foobar);
            Assert.Equal("FooBar", foobar.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.Equal("Test1.FooBar", foobar.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.Equal("public class FooBar : Bar<string>, IFooBar, IFoo, IBar", foobar.Syntax.Content[SyntaxLanguage.CSharp]);
            Console.WriteLine(string.Join(",", foobar.Inheritance));
            Assert.Equal(new[] { "System.Object", "Test1.Foo{System.String[]}", "Test1.Bar{System.String}" }, foobar.Inheritance);
            Assert.Equal(new[] { "Test1.IFoo", "Test1.IBar", "Test1.IFooBar" }.OrderBy(s => s), foobar.Implements.OrderBy(s => s));

            Assert.NotNull(output.References);
            Assert.Equal(19, output.References.Count);
            {
                var item = output.References["System.Object"];
                Assert.Equal("System", item.Parent);
                Assert.NotNull(item);
                Assert.Equal(1, item.Parts[SyntaxLanguage.CSharp].Count);

                Assert.Equal("System.Object", item.Parts[SyntaxLanguage.CSharp][0].Name);
                Assert.Equal("Object", item.Parts[SyntaxLanguage.CSharp][0].DisplayName);
                Assert.Equal("System.Object", item.Parts[SyntaxLanguage.CSharp][0].DisplayQualifiedNames);
            }
            {
                var item = output.References["Test1.Bar{System.String}"];
                Assert.NotNull(item);
                Assert.Equal("Test1.Bar`1", item.Definition);
                Assert.Equal("Test1", item.Parent);
                Assert.Equal(4, item.Parts[SyntaxLanguage.CSharp].Count);

                Assert.Equal("Test1.Bar`1", item.Parts[SyntaxLanguage.CSharp][0].Name);
                Assert.Equal("Bar", item.Parts[SyntaxLanguage.CSharp][0].DisplayName);
                Assert.Equal("Test1.Bar", item.Parts[SyntaxLanguage.CSharp][0].DisplayQualifiedNames);

                Assert.Null(item.Parts[SyntaxLanguage.CSharp][1].Name);
                Assert.Equal("<", item.Parts[SyntaxLanguage.CSharp][1].DisplayName);
                Assert.Equal("<", item.Parts[SyntaxLanguage.CSharp][1].DisplayQualifiedNames);

                Assert.Equal("System.String", item.Parts[SyntaxLanguage.CSharp][2].Name);
                Assert.Equal("String", item.Parts[SyntaxLanguage.CSharp][2].DisplayName);
                Assert.Equal("System.String", item.Parts[SyntaxLanguage.CSharp][2].DisplayQualifiedNames);

                Assert.Null(item.Parts[SyntaxLanguage.CSharp][3].Name);
                Assert.Equal(">", item.Parts[SyntaxLanguage.CSharp][3].DisplayName);
                Assert.Equal(">", item.Parts[SyntaxLanguage.CSharp][3].DisplayQualifiedNames);
            }
            {
                var item = output.References["Test1.Foo{{T}[]}"];
                Assert.NotNull(item);
                Assert.Equal("Test1.Foo`1", item.Definition);
                Assert.Equal("Test1", item.Parent);
                Assert.Equal(5, item.Parts[SyntaxLanguage.CSharp].Count);

                Assert.Equal("Test1.Foo`1", item.Parts[SyntaxLanguage.CSharp][0].Name);
                Assert.Equal("Foo", item.Parts[SyntaxLanguage.CSharp][0].DisplayName);
                Assert.Equal("Test1.Foo", item.Parts[SyntaxLanguage.CSharp][0].DisplayQualifiedNames);

                Assert.Null(item.Parts[SyntaxLanguage.CSharp][1].Name);
                Assert.Equal("<", item.Parts[SyntaxLanguage.CSharp][1].DisplayName);
                Assert.Equal("<", item.Parts[SyntaxLanguage.CSharp][1].DisplayQualifiedNames);

                Assert.Null(item.Parts[SyntaxLanguage.CSharp][2].Name);
                Assert.Equal("T", item.Parts[SyntaxLanguage.CSharp][2].DisplayName);
                Assert.Equal("T", item.Parts[SyntaxLanguage.CSharp][2].DisplayQualifiedNames);

                Assert.Null(item.Parts[SyntaxLanguage.CSharp][3].Name);
                Assert.Equal("[]", item.Parts[SyntaxLanguage.CSharp][3].DisplayName);
                Assert.Equal("[]", item.Parts[SyntaxLanguage.CSharp][3].DisplayQualifiedNames);

                Assert.Null(item.Parts[SyntaxLanguage.CSharp][4].Name);
                Assert.Equal(">", item.Parts[SyntaxLanguage.CSharp][4].DisplayName);
                Assert.Equal(">", item.Parts[SyntaxLanguage.CSharp][4].DisplayQualifiedNames);
            }
            {
                var item = output.References["Test1.Foo{System.String[]}"];
                Assert.NotNull(item);
                Assert.Equal("Test1.Foo`1", item.Definition);
                Assert.Equal("Test1", item.Parent);
                Assert.Equal(5, item.Parts[SyntaxLanguage.CSharp].Count);

                Assert.Equal("Test1.Foo`1", item.Parts[SyntaxLanguage.CSharp][0].Name);
                Assert.Equal("Foo", item.Parts[SyntaxLanguage.CSharp][0].DisplayName);
                Assert.Equal("Test1.Foo", item.Parts[SyntaxLanguage.CSharp][0].DisplayQualifiedNames);

                Assert.Null(item.Parts[SyntaxLanguage.CSharp][1].Name);
                Assert.Equal("<", item.Parts[SyntaxLanguage.CSharp][1].DisplayName);
                Assert.Equal("<", item.Parts[SyntaxLanguage.CSharp][1].DisplayQualifiedNames);

                Assert.Equal("System.String", item.Parts[SyntaxLanguage.CSharp][2].Name);
                Assert.Equal("String", item.Parts[SyntaxLanguage.CSharp][2].DisplayName);
                Assert.Equal("System.String", item.Parts[SyntaxLanguage.CSharp][2].DisplayQualifiedNames);

                Assert.Null(item.Parts[SyntaxLanguage.CSharp][3].Name);
                Assert.Equal("[]", item.Parts[SyntaxLanguage.CSharp][3].DisplayName);
                Assert.Equal("[]", item.Parts[SyntaxLanguage.CSharp][3].DisplayQualifiedNames);

                Assert.Null(item.Parts[SyntaxLanguage.CSharp][4].Name);
                Assert.Equal(">", item.Parts[SyntaxLanguage.CSharp][4].DisplayName);
                Assert.Equal(">", item.Parts[SyntaxLanguage.CSharp][4].DisplayQualifiedNames);
            }
        }

        [Fact]
        public void TestGenereateMetadataWithEnum()
        {
            string code = @"
namespace Test1
{
    public enum ABC{A,B,C}
    public enum YN : byte {Y=1, N=0}
    public enum XYZ:int{X,Y,Z}
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Equal(1, output.Items.Count);
            {
                var type = output.Items[0].Items[0];
                Assert.NotNull(type);
                Assert.Equal("ABC", type.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.ABC", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.ABC", type.Name);
                Assert.Equal("public enum ABC", type.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var type = output.Items[0].Items[1];
                Assert.NotNull(type);
                Assert.Equal("YN", type.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.YN", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.YN", type.Name);
                Assert.Equal("public enum YN : byte", type.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var type = output.Items[0].Items[2];
                Assert.NotNull(type);
                Assert.Equal("XYZ", type.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.XYZ", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.XYZ", type.Name);
                Assert.Equal("public enum XYZ", type.Syntax.Content[SyntaxLanguage.CSharp]);
            }
        }

        [Trait("Related", "Inheritance")]
        [Fact]
        public void TestGenereateMetadataWithStruct()
        {
            string code = @"
using System.Collections
using System.Collections.Generic
namespace Test1
{
    public struct Foo{}
    public struct Bar<T> : IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator() => null;
        IEnumerator IEnumerable.GetEnumerator() => null;
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Equal(1, output.Items.Count);
            {
                var type = output.Items[0].Items[0];
                Assert.NotNull(type);
                Assert.Equal("Foo", type.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo", type.Name);
                Assert.Equal("public struct Foo", type.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Null(type.Implements);
            }
            {
                var type = output.Items[0].Items[1];
                Assert.NotNull(type);
                Assert.Equal("Bar<T>", type.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar<T>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar`1", type.Name);
                Assert.Equal("public struct Bar<T> : IEnumerable<T>, IEnumerable", type.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "System.Collections.Generic.IEnumerable{{T}}", "System.Collections.IEnumerable" }, type.Implements);
            }
            // inheritance of Foo
            {
                var inheritedMembers = output.Items[0].Items[0].InheritedMembers;
                Assert.NotNull(inheritedMembers);
                Assert.Equal(
                    new string[]
                    {
                        "System.ValueType.ToString",
                        "System.ValueType.Equals(System.Object)",
                        "System.ValueType.GetHashCode",
                        "System.Object.Equals(System.Object,System.Object)",
                        "System.Object.ReferenceEquals(System.Object,System.Object)",
                        "System.Object.GetType",
                    }.OrderBy(s => s),
                    inheritedMembers.OrderBy(s => s));
            }
        }

        [Trait("Related", "Generic")]
        [Fact]
        public void TestGenereateMetadataWithDelegate()
        {
            string code = @"
using System.Collections.Generic
namespace Test1
{
    public delegate void Foo();
    public delegate T Bar<T>(IEnumerable<T> x = null) where T : class;
    public delegate void FooBar(ref int x, out string y, params byte[] z);
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Equal(1, output.Items.Count);
            {
                var type = output.Items[0].Items[0];
                Assert.NotNull(type);
                Assert.Equal("Foo", type.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo", type.Name);
                Assert.Equal("public delegate void Foo();", type.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Null(type.Syntax.Parameters);
                Assert.Null(type.Syntax.Return);
            }
            {
                var type = output.Items[0].Items[1];
                Assert.NotNull(type);
                Assert.Equal("Bar<T>", type.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar<T>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar`1", type.Name);
                Assert.Equal(@"public delegate T Bar<T>(IEnumerable<T> x = null)where T : class;", type.Syntax.Content[SyntaxLanguage.CSharp]);

                Assert.NotNull(type.Syntax.Parameters);
                Assert.Equal(1, type.Syntax.Parameters.Count);
                Assert.Equal("x", type.Syntax.Parameters[0].Name);
                Assert.Equal("System.Collections.Generic.IEnumerable{{T}}", type.Syntax.Parameters[0].Type);
                Assert.NotNull(type.Syntax.Return);
                Assert.Equal("{T}", type.Syntax.Return.Type);
            }
            {
                var type = output.Items[0].Items[2];
                Assert.NotNull(type);
                Assert.Equal("FooBar", type.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.FooBar", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.FooBar", type.Name);
                Assert.Equal(@"public delegate void FooBar(ref int x, out string y, params byte[] z);", type.Syntax.Content[SyntaxLanguage.CSharp]);

                Assert.NotNull(type.Syntax.Parameters);
                Assert.Equal(3, type.Syntax.Parameters.Count);
                Assert.Equal("x", type.Syntax.Parameters[0].Name);
                Assert.Equal("System.Int32", type.Syntax.Parameters[0].Type);
                Assert.Equal("y", type.Syntax.Parameters[1].Name);
                Assert.Equal("System.String", type.Syntax.Parameters[1].Type);
                Assert.Equal("z", type.Syntax.Parameters[2].Name);
                Assert.Equal("System.Byte[]", type.Syntax.Parameters[2].Type);
                Assert.Null(type.Syntax.Return);
            }
        }

        [Trait("Related", "Generic")]
        [Fact]
        public void TestGenereateMetadataWithMethod()
        {
            string code = @"
using System.Threading.Tasks
namespace Test1
{
    public abstract class Foo<T>
    {
        public abstract void M1();
        protected virtual Foo<T> M2<TArg>(TArg arg) where TArg : Foo<T> => this;
        public static TResult M3<TResult>(string x) where TResult : class => null;
        public void M4(int x){}
    }
    public class Bar : Foo<string>, IFooBar
    {
        public override void M1(){}
        protected override sealed Foo<T> M2<TArg>(TArg arg) where TArg : Foo<string> => this;
        public int M5<TArg>(TArg arg) where TArg : struct, new() => 2;
    }
    public interface IFooBar
    {
        void M1();
        Foo<T> M2<TArg>(TArg arg) where TArg : Foo<string>;
        int M5<TArg>(TArg arg) where TArg : struct, new();
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Equal(1, output.Items.Count);
            // Foo<T>
            {
                var method = output.Items[0].Items[0].Items[0];
                Assert.NotNull(method);
                Assert.Equal("M1()", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.M1()", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.M1", method.Name);
                Assert.Equal("public abstract void M1()", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[1];
                Assert.NotNull(method);
                Assert.Equal("M2<TArg>(TArg)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.M2<TArg>(TArg)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.M2``1(``0)", method.Name);
                Assert.Equal("protected virtual Foo<T> M2<TArg>(TArg arg)where TArg : Foo<T>", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[2];
                Assert.NotNull(method);
                Assert.Equal("M3<TResult>(String)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.M3<TResult>(System.String)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.M3``1(System.String)", method.Name);
                Assert.Equal("public static TResult M3<TResult>(string x)where TResult : class", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[3];
                Assert.NotNull(method);
                Assert.Equal("M4(Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.M4(System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.M4(System.Int32)", method.Name);
                Assert.Equal("public void M4(int x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            // Bar
            {
                var method = output.Items[0].Items[1].Items[0];
                Assert.NotNull(method);
                Assert.Equal("M1()", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.M1()", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.M1", method.Name);
                Assert.Equal("public override void M1()", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo{System.String}.M1", method.Overridden);
            }
            {
                var method = output.Items[0].Items[1].Items[1];
                Assert.NotNull(method);
                Assert.Equal("M2<TArg>(TArg)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.M2<TArg>(TArg)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.M2``1(``0)", method.Name);
                Assert.Equal("protected override sealed Foo<T> M2<TArg>(TArg arg)where TArg : Foo<string>", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo{System.String}.M2``1({TArg})", method.Overridden);
            }
            {
                var method = output.Items[0].Items[1].Items[2];
                Assert.NotNull(method);
                Assert.Equal("M5<TArg>(TArg)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.M5<TArg>(TArg)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.M5``1(``0)", method.Name);
                Assert.Equal("public int M5<TArg>(TArg arg)where TArg : struct, new ()", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            // IFooBar
            {
                var method = output.Items[0].Items[2].Items[0];
                Assert.NotNull(method);
                Assert.Equal("M1()", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.M1()", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.M1", method.Name);
                Assert.Equal("void M1()", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[2].Items[1];
                Assert.NotNull(method);
                Assert.Equal("M2<TArg>(TArg)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.M2<TArg>(TArg)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.M2``1(``0)", method.Name);
                Assert.Equal("Foo<T> M2<TArg>(TArg arg)where TArg : Foo<string>", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[2].Items[2];
                Assert.NotNull(method);
                Assert.Equal("M5<TArg>(TArg)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.M5<TArg>(TArg)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.M5``1(``0)", method.Name);
                Assert.Equal("int M5<TArg>(TArg arg)where TArg : struct, new ()", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
        }

        [Trait("Related", "Generic")]
        [Trait("Related", "EII")]
        [Fact]
        public void TestGenereateMetadataWithEii()
        {
            string code = @"
using System.Collections.Generic
namespace Test1
{
    public class Foo<T> : IFoo, IFoo<string>, IFoo<T> where T : class
    {
        object IFoo.Bar(ref int x) => null;
        string IFoo<string>.Bar<TArg>(TArg[] x) => "";
        T IFoo<T>.Bar<TArg>(TArg[] x) => null;
        string IFoo<string>.P { get; set; }
        T IFoo<T>.P { get; set; }
        int IFoo<string>.this[string x] { get { return 1; } }
        int IFoo<T>.this[T x] { get { return 1; } }
        event EventHandler IFoo.E { add { } remove { } }
    }
    public interface IFoo
    {
        object Bar(ref int x);
        event EventHandler E;
    }
    public interface IFoo<out T>
    {
        T Bar<TArg>(TArg[] x)
        T P { get; set; }
        int this[T x] { get; }
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Equal(1, output.Items.Count);
            {
                var type = output.Items[0].Items[0];
                Assert.NotNull(type);
                Assert.Equal("Foo<T>", type.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1", type.Name);
                Assert.Equal(@"public class Foo<T> : IFoo, IFoo<string>, IFoo<T> where T : class", type.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[0];
                Assert.NotNull(method);
                Assert.Equal("IFoo.Bar(ref Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Test1.IFoo.Bar(ref System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Test1#IFoo#Bar(System.Int32@)", method.Name);
                Assert.Equal(@"object IFoo.Bar(ref int x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[1];
                Assert.NotNull(method);
                Assert.Equal("IFoo<String>.Bar<TArg>(TArg[])", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Test1.IFoo<System.String>.Bar<TArg>(TArg[])", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Test1#IFoo{System#String}#Bar``1(``0[])", method.Name);
                Assert.Equal(@"string IFoo<string>.Bar<TArg>(TArg[] x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[2];
                Assert.NotNull(method);
                Assert.Equal("IFoo<T>.Bar<TArg>(TArg[])", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Test1.IFoo<T>.Bar<TArg>(TArg[])", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Test1#IFoo{T}#Bar``1(``0[])", method.Name);
                Assert.Equal(@"T IFoo<T>.Bar<TArg>(TArg[] x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var p = output.Items[0].Items[0].Items[3];
                Assert.NotNull(p);
                Assert.Equal("IFoo<String>.P", p.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Test1.IFoo<System.String>.P", p.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Test1#IFoo{System#String}#P", p.Name);
                Assert.Equal(@"string IFoo<string>.P
{
    get;
    set;
}", p.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var p = output.Items[0].Items[0].Items[4];
                Assert.NotNull(p);
                Assert.Equal("IFoo<T>.P", p.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Test1.IFoo<T>.P", p.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Test1#IFoo{T}#P", p.Name);
                Assert.Equal(@"T IFoo<T>.P
{
    get;
    set;
}", p.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var p = output.Items[0].Items[0].Items[5];
                Assert.NotNull(p);
                Assert.Equal("IFoo<String>.Item[String]", p.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Test1.IFoo<System.String>.Item[System.String]", p.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Test1#IFoo{System#String}#Item(System.String)", p.Name);
                Assert.Equal(@"int IFoo<string>.this[string x]
{
    get;
}", p.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var p = output.Items[0].Items[0].Items[6];
                Assert.NotNull(p);
                Assert.Equal("IFoo<T>.Item[T]", p.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Test1.IFoo<T>.Item[T]", p.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Test1#IFoo{T}#Item(`0)", p.Name);
                Assert.Equal(@"int IFoo<T>.this[T x]
{
    get;
}", p.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var e = output.Items[0].Items[0].Items[7];
                Assert.NotNull(e);
                Assert.Equal("IFoo.E", e.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Test1.IFoo.E", e.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Test1#IFoo#E", e.Name);
                Assert.Equal(@"event EventHandler IFoo.E", e.Syntax.Content[SyntaxLanguage.CSharp]);
            }
        }

        [Fact]
        public void TestGenereateMetadataWithOperator()
        {
            string code = @"
using System.Collections.Generic
namespace Test1
{
    public class Foo
    {
        // unary
        public static Foo operator +(Foo x) => x;
        public static Foo operator -(Foo x) => x;
        public static Foo operator !(Foo x) => x;
        public static Foo operator ~(Foo x) => x;
        public static Foo operator ++(Foo x) => x;
        public static Foo operator --(Foo x) => x;
        public static Foo operator true(Foo x) => x;
        public static Foo operator false(Foo x) => x;
        // binary
        public static Foo operator +(Foo x, int y) => x;
        public static Foo operator -(Foo x, int y) => x;
        public static Foo operator *(Foo x, int y) => x;
        public static Foo operator /(Foo x, int y) => x;
        public static Foo operator %(Foo x, int y) => x;
        public static Foo operator &(Foo x, int y) => x;
        public static Foo operator |(Foo x, int y) => x;
        public static Foo operator ^(Foo x, int y) => x;
        public static Foo operator >>(Foo x, int y) => x;
        public static Foo operator <<(Foo x, int y) => x;
        // comparison
        public static bool operator ==(Foo x, int y) => false;
        public static bool operator !=(Foo x, int y) => false;
        public static bool operator >(Foo x, int y) => false;
        public static bool operator <(Foo x, int y) => false;
        public static bool operator >=(Foo x, int y) => false;
        public static bool operator <=(Foo x, int y) => false;
        // convertion
        public static implicit operator Foo (int x) => null;
        public static explicit operator int (Foo x) => 0;
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Equal(1, output.Items.Count);
            // unary
            {
                var method = output.Items[0].Items[0].Items[0];
                Assert.NotNull(method);
                Assert.Equal("UnaryPlus(Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.UnaryPlus(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_UnaryPlus(Test1.Foo)", method.Name);
                Assert.Equal(@"public static Foo operator +(Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[1];
                Assert.NotNull(method);
                Assert.Equal("UnaryNegation(Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.UnaryNegation(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_UnaryNegation(Test1.Foo)", method.Name);
                Assert.Equal(@"public static Foo operator -(Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[2];
                Assert.NotNull(method);
                Assert.Equal("LogicalNot(Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.LogicalNot(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_LogicalNot(Test1.Foo)", method.Name);
                Assert.Equal(@"public static Foo operator !(Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[3];
                Assert.NotNull(method);
                Assert.Equal("OnesComplement(Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.OnesComplement(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_OnesComplement(Test1.Foo)", method.Name);
                Assert.Equal(@"public static Foo operator ~(Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[4];
                Assert.NotNull(method);
                Assert.Equal("Increment(Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.Increment(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_Increment(Test1.Foo)", method.Name);
                Assert.Equal(@"public static Foo operator ++(Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[5];
                Assert.NotNull(method);
                Assert.Equal("Decrement(Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.Decrement(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_Decrement(Test1.Foo)", method.Name);
                Assert.Equal(@"public static Foo operator --(Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[6];
                Assert.NotNull(method);
                Assert.Equal("True(Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.True(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_True(Test1.Foo)", method.Name);
                Assert.Equal(@"public static Foo operator true (Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[7];
                Assert.NotNull(method);
                Assert.Equal("False(Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.False(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_False(Test1.Foo)", method.Name);
                Assert.Equal(@"public static Foo operator false (Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            // binary
            {
                var method = output.Items[0].Items[0].Items[8];
                Assert.NotNull(method);
                Assert.Equal("Addition(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.Addition(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_Addition(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static Foo operator +(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[9];
                Assert.NotNull(method);
                Assert.Equal("Subtraction(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.Subtraction(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_Subtraction(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static Foo operator -(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[10];
                Assert.NotNull(method);
                Assert.Equal("Multiply(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.Multiply(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_Multiply(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static Foo operator *(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[11];
                Assert.NotNull(method);
                Assert.Equal("Division(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.Division(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_Division(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static Foo operator /(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[12];
                Assert.NotNull(method);
                Assert.Equal("Modulus(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.Modulus(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_Modulus(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static Foo operator %(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[13];
                Assert.NotNull(method);
                Assert.Equal("BitwiseAnd(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.BitwiseAnd(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_BitwiseAnd(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static Foo operator &(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[14];
                Assert.NotNull(method);
                Assert.Equal("BitwiseOr(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.BitwiseOr(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_BitwiseOr(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static Foo operator |(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[15];
                Assert.NotNull(method);
                Assert.Equal("ExclusiveOr(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.ExclusiveOr(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_ExclusiveOr(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static Foo operator ^(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[16];
                Assert.NotNull(method);
                Assert.Equal("RightShift(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.RightShift(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_RightShift(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static Foo operator >>(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[17];
                Assert.NotNull(method);
                Assert.Equal("LeftShift(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.LeftShift(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_LeftShift(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static Foo operator <<(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            // comparison
            {
                var method = output.Items[0].Items[0].Items[18];
                Assert.NotNull(method);
                Assert.Equal("Equality(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.Equality(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_Equality(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static bool operator ==(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[19];
                Assert.NotNull(method);
                Assert.Equal("Inequality(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.Inequality(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_Inequality(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static bool operator !=(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[20];
                Assert.NotNull(method);
                Assert.Equal("GreaterThan(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.GreaterThan(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_GreaterThan(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static bool operator>(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[21];
                Assert.NotNull(method);
                Assert.Equal("LessThan(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.LessThan(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_LessThan(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static bool operator <(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[22];
                Assert.NotNull(method);
                Assert.Equal("GreaterThanOrEqual(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.GreaterThanOrEqual(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_GreaterThanOrEqual(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static bool operator >=(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[23];
                Assert.NotNull(method);
                Assert.Equal("LessThanOrEqual(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.LessThanOrEqual(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_LessThanOrEqual(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static bool operator <=(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            // conversion
            {
                var method = output.Items[0].Items[0].Items[24];
                Assert.NotNull(method);
                Assert.Equal("Implicit(Int32 to Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.Implicit(System.Int32 to Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_Implicit(System.Int32)~Test1.Foo", method.Name);
                Assert.Equal(@"public static implicit operator Foo(int x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[25];
                Assert.NotNull(method);
                Assert.Equal("Explicit(Foo to Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.Explicit(Test1.Foo to System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_Explicit(Test1.Foo)~System.Int32", method.Name);
                Assert.Equal(@"public static explicit operator int (Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
        }

        [Trait("Related", "Generic")]
        [Fact]
        public void TestGenereateMetadataWithConstructor()
        {
            string code = @"
namespace Test1
{
    public class Foo<T>
    {
        static Foo(){}
        public Foo(){}
        public Foo(int x) : base(x){}
        protected internal Foo(string x) : base(0){}
    }
    public class Bar
    {
        public Bar(){}
        protected Bar(int x){}
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Equal(1, output.Items.Count);
            {
                var constructor = output.Items[0].Items[0].Items[0];
                Assert.NotNull(constructor);
                Assert.Equal("Foo()", constructor.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Foo()", constructor.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.#ctor", constructor.Name);
                Assert.Equal("public Foo()", constructor.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var constructor = output.Items[0].Items[0].Items[1];
                Assert.NotNull(constructor);
                Assert.Equal("Foo(Int32)", constructor.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Foo(System.Int32)", constructor.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.#ctor(System.Int32)", constructor.Name);
                Assert.Equal("public Foo(int x)", constructor.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var constructor = output.Items[0].Items[0].Items[2];
                Assert.NotNull(constructor);
                Assert.Equal("Foo(String)", constructor.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Foo(System.String)", constructor.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.#ctor(System.String)", constructor.Name);
                Assert.Equal("protected Foo(string x)", constructor.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var constructor = output.Items[0].Items[1].Items[0];
                Assert.NotNull(constructor);
                Assert.Equal("Bar()", constructor.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Bar()", constructor.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.#ctor", constructor.Name);
                Assert.Equal("public Bar()", constructor.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var constructor = output.Items[0].Items[1].Items[1];
                Assert.NotNull(constructor);
                Assert.Equal("Bar(Int32)", constructor.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Bar(System.Int32)", constructor.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.#ctor(System.Int32)", constructor.Name);
                Assert.Equal("protected Bar(int x)", constructor.Syntax.Content[SyntaxLanguage.CSharp]);
            }
        }

        [Trait("Related", "Generic")]
        [Fact]
        public void TestGenereateMetadataWithField()
        {
            string code = @"
namespace Test1
{
    public class Foo<T>
    {
        public volatile int X;
        protected static readonly Foo<T> Y = null;
        protected internal const string Z = "";
    }
    public enum Bar
    {
        Black,
        Red,
        Blue = 2,
        Green = 4,
        White = Red | Blue | Green,
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Equal(1, output.Items.Count);
            {
                var field = output.Items[0].Items[0].Items[0];
                Assert.NotNull(field);
                Assert.Equal("X", field.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.X", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.X", field.Name);
                Assert.Equal("public volatile int X", field.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var field = output.Items[0].Items[0].Items[1];
                Assert.NotNull(field);
                Assert.Equal("Y", field.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Y", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Y", field.Name);
                Assert.Equal("protected static readonly Foo<T> Y", field.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var field = output.Items[0].Items[0].Items[2];
                Assert.NotNull(field);
                Assert.Equal("Z", field.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Z", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Z", field.Name);
                Assert.Equal("protected const string Z", field.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var field = output.Items[0].Items[1].Items[0];
                Assert.NotNull(field);
                Assert.Equal("Black", field.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Black", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Black", field.Name);
                Assert.Equal("Black = 0", field.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var field = output.Items[0].Items[1].Items[1];
                Assert.NotNull(field);
                Assert.Equal("Red", field.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Red", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Red", field.Name);
                Assert.Equal("Red = 1", field.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var field = output.Items[0].Items[1].Items[2];
                Assert.NotNull(field);
                Assert.Equal("Blue", field.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Blue", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Blue", field.Name);
                Assert.Equal(@"Blue = 2", field.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var field = output.Items[0].Items[1].Items[3];
                Assert.NotNull(field);
                Assert.Equal("Green", field.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Green", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Green", field.Name);
                Assert.Equal("Green = 4", field.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var field = output.Items[0].Items[1].Items[4];
                Assert.NotNull(field);
                Assert.Equal("White", field.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.White", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.White", field.Name);
                Assert.Equal(@"White = 7", field.Syntax.Content[SyntaxLanguage.CSharp]);
            }
        }

        [Trait("Related", "Generic")]
        [Fact]
        public void TestGenereateMetadataWithCSharpCodeAndEvent()
        {
            string code = @"
using System;
namespace Test1
{
    public abstract class Foo<T> where T : EventArgs
    {
        public event EventHandler A;
        protected static event EventHandler B { add {} remove {}}
        protected internal abstract event EventHandler<T> C;
        public virtual event EventHandler<T> D { add {} remove {}}
    }
    public class Bar<T> : Foo<T> where T : EventArgs
    {
        public new event EventHandler A;
        protected internal override sealed event EventHandler<T> C;
        public override event EventHandler<T> D;
    }
    public interface IFooBar<T> where T : EventArgs
    {
        event EventHandler A;
        event EventHandler<T> D;
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Equal(1, output.Items.Count);
            {
                var a = output.Items[0].Items[0].Items[0];
                Assert.NotNull(a);
                Assert.Equal("A", a.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.A", a.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.A", a.Name);
                Assert.Equal("public event EventHandler A", a.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var b = output.Items[0].Items[0].Items[1];
                Assert.NotNull(b);
                Assert.Equal("B", b.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.B", b.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.B", b.Name);
                Assert.Equal("protected static event EventHandler B", b.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var c = output.Items[0].Items[0].Items[2];
                Assert.NotNull(c);
                Assert.Equal("C", c.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.C", c.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.C", c.Name);
                Assert.Equal("protected abstract event EventHandler<T> C", c.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var d = output.Items[0].Items[0].Items[3];
                Assert.NotNull(d);
                Assert.Equal("D", d.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.D", d.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.D", d.Name);
                Assert.Equal("public virtual event EventHandler<T> D", d.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var a = output.Items[0].Items[1].Items[0];
                Assert.NotNull(a);
                Assert.Equal("A", a.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar<T>.A", a.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar`1.A", a.Name);
                Assert.Equal("public event EventHandler A", a.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var c = output.Items[0].Items[1].Items[1];
                Assert.NotNull(c);
                Assert.Equal("C", c.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar<T>.C", c.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar`1.C", c.Name);
                Assert.Equal("protected override sealed event EventHandler<T> C", c.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo{{T}}.C", c.Overridden);
            }
            {
                var d = output.Items[0].Items[1].Items[2];
                Assert.NotNull(d);
                Assert.Equal("D", d.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar<T>.D", d.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar`1.D", d.Name);
                Assert.Equal("public override event EventHandler<T> D", d.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo{{T}}.D", d.Overridden);
            }
            {
                var a = output.Items[0].Items[2].Items[0];
                Assert.NotNull(a);
                Assert.Equal("A", a.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar<T>.A", a.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar`1.A", a.Name);
                Assert.Equal("event EventHandler A", a.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var d = output.Items[0].Items[2].Items[1];
                Assert.NotNull(d);
                Assert.Equal("D", d.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar<T>.D", d.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar`1.D", d.Name);
                Assert.Equal("event EventHandler<T> D", d.Syntax.Content[SyntaxLanguage.CSharp]);
            }
        }

        [Trait("Related", "Generic")]
        [Fact]
        public void TestGenereateMetadataWithProperty()
        {
            string code = @"
namespace Test1
{
    public abstract class Foo<T> where T : class
    {
        public int A { get; set; }
        public virtual int B { get { return 1; } }
        public abstract int C { set; }
        protected int D { get; private set; }
        public T E { get; protected set; }
        protected internal static int F { get; protected set; }
    }
    public class Bar : Foo<string>, IFooBar
    {
        public new virtual int A { get; set; }
        public override int B { get { return 2; } }
        public override sealed int C { set; }
    }
    public interface IFooBar
    {
        int A { get; set; }
        int B { get; }
        int C { set; }
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Equal(1, output.Items.Count);
            {
                var a = output.Items[0].Items[0].Items[0];
                Assert.NotNull(a);
                Assert.Equal("A", a.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.A", a.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.A", a.Name);
                Assert.Equal(@"public int A
{
    get;
    set;
}", a.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var b = output.Items[0].Items[0].Items[1];
                Assert.NotNull(b);
                Assert.Equal("B", b.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.B", b.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.B", b.Name);
                Assert.Equal(@"public virtual int B
{
    get;
}", b.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var c = output.Items[0].Items[0].Items[2];
                Assert.NotNull(c);
                Assert.Equal("C", c.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.C", c.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.C", c.Name);
                Assert.Equal(@"public abstract int C
{
    set;
}", c.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var d = output.Items[0].Items[0].Items[3];
                Assert.NotNull(d);
                Assert.Equal("D", d.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.D", d.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.D", d.Name);
                Assert.Equal(@"protected int D
{
    get;
}", d.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var e = output.Items[0].Items[0].Items[4];
                Assert.NotNull(e);
                Assert.Equal("E", e.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.E", e.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.E", e.Name);
                Assert.Equal(@"public T E
{
    get;
    protected set;
}", e.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var f = output.Items[0].Items[0].Items[5];
                Assert.NotNull(f);
                Assert.Equal("F", f.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.F", f.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.F", f.Name);
                Assert.Equal(@"protected static int F
{
    get;
    set;
}", f.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var a = output.Items[0].Items[1].Items[0];
                Assert.NotNull(a);
                Assert.Equal("A", a.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.A", a.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.A", a.Name);
                Assert.Equal(@"public virtual int A
{
    get;
    set;
}", a.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var b = output.Items[0].Items[1].Items[1];
                Assert.NotNull(b);
                Assert.Equal("B", b.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.B", b.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.B", b.Name);
                Assert.Equal(@"public override int B
{
    get;
}", b.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo{System.String}.B", b.Overridden);
            }
            {
                var c = output.Items[0].Items[1].Items[2];
                Assert.NotNull(c);
                Assert.Equal("C", c.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.C", c.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.C", c.Name);
                Assert.Equal(@"public override sealed int C
{
    set;
}", c.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo{System.String}.C", c.Overridden);
            }
            {
                var a = output.Items[0].Items[2].Items[0];
                Assert.NotNull(a);
                Assert.Equal("A", a.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.A", a.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.A", a.Name);
                Assert.Equal(@"int A
{
    get;
    set;
}", a.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var b = output.Items[0].Items[2].Items[1];
                Assert.NotNull(b);
                Assert.Equal("B", b.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.B", b.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.B", b.Name);
                Assert.Equal(@"int B
{
    get;
}", b.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var c = output.Items[0].Items[2].Items[2];
                Assert.NotNull(c);
                Assert.Equal("C", c.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.C", c.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.C", c.Name);
                Assert.Equal(@"int C
{
    set;
}", c.Syntax.Content[SyntaxLanguage.CSharp]);
            }
        }

        [Trait("Related", "Generic")]
        [Fact]
        public void TestGenereateMetadataWithIndexer()
        {
            string code = @"
using System;
namespace Test1
{
    public abstract class Foo<T> where T : class
    {
        public int this[int x] { get { return 0; } set { } }
        public virtual int this[string x] { get { return 1; } }
        public abstract int this[object x] { set; }
        protected int this[DateTime x] { get { return 0; } private set { } }
        public int this[T t] { get { return 0; } protected set { } }
        protected internal int this[int x, T t] { get; protected set; }
    }
    public class Bar : Foo<string>, IFooBar
    {
        public new virtual int this[int x] { get { return 0; } set { } }
        public override int this[string x] { get { return 2; } }
        public override sealed int this[object x] { set; }
    }
    public interface IFooBar
    {
        int this[int x] { get; set; }
        int this[string x] { get; }
        int this[object x] { set; }
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Equal(1, output.Items.Count);
            // Foo<T>
            {
                var indexer = output.Items[0].Items[0].Items[0];
                Assert.NotNull(indexer);
                Assert.Equal("Item[Int32]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Item[System.Int32]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Item(System.Int32)", indexer.Name);
                Assert.Equal(@"public int this[int x]
{
    get;
    set;
}", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var indexer = output.Items[0].Items[0].Items[1];
                Assert.NotNull(indexer);
                Assert.Equal("Item[String]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Item[System.String]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Item(System.String)", indexer.Name);
                Assert.Equal(@"public virtual int this[string x]
{
    get;
}", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var indexer = output.Items[0].Items[0].Items[2];
                Assert.NotNull(indexer);
                Assert.Equal("Item[Object]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Item[System.Object]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Item(System.Object)", indexer.Name);
                Assert.Equal(@"public abstract int this[object x]
{
    set;
}", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var indexer = output.Items[0].Items[0].Items[3];
                Assert.NotNull(indexer);
                Assert.Equal("Item[DateTime]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Item[System.DateTime]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Item(System.DateTime)", indexer.Name);
                Assert.Equal(@"protected int this[DateTime x]
{
    get;
}", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var indexer = output.Items[0].Items[0].Items[4];
                Assert.NotNull(indexer);
                Assert.Equal("Item[T]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Item[T]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Item(`0)", indexer.Name);
                Assert.Equal(@"public int this[T t]
{
    get;
    protected set;
}", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var indexer = output.Items[0].Items[0].Items[5];
                Assert.NotNull(indexer);
                Assert.Equal("Item[Int32, T]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Item[System.Int32, T]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Item(System.Int32,`0)", indexer.Name);
                Assert.Equal(@"protected int this[int x, T t]
{
    get;
    set;
}", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            // Bar
            {
                var indexer = output.Items[0].Items[1].Items[0];
                Assert.NotNull(indexer);
                Assert.Equal("Item[Int32]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Item[System.Int32]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Item(System.Int32)", indexer.Name);
                Assert.Equal(@"public virtual int this[int x]
{
    get;
    set;
}", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var indexer = output.Items[0].Items[1].Items[1];
                Assert.NotNull(indexer);
                Assert.Equal("Item[String]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Item[System.String]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Item(System.String)", indexer.Name);
                Assert.Equal(@"public override int this[string x]
{
    get;
}", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo{System.String}.Item(System.String)", indexer.Overridden);
            }
            {
                var indexer = output.Items[0].Items[1].Items[2];
                Assert.NotNull(indexer);
                Assert.Equal("Item[Object]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Item[System.Object]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Item(System.Object)", indexer.Name);
                Assert.Equal(@"public override sealed int this[object x]
{
    set;
}", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo{System.String}.Item(System.Object)", indexer.Overridden);
            }
            // IFooBar
            {
                var indexer = output.Items[0].Items[2].Items[0];
                Assert.NotNull(indexer);
                Assert.Equal("Item[Int32]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.Item[System.Int32]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.Item(System.Int32)", indexer.Name);
                Assert.Equal(@"int this[int x]
{
    get;
    set;
}", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var indexer = output.Items[0].Items[2].Items[1];
                Assert.NotNull(indexer);
                Assert.Equal("Item[String]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.Item[System.String]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.Item(System.String)", indexer.Name);
                Assert.Equal(@"int this[string x]
{
    get;
}", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var indexer = output.Items[0].Items[2].Items[2];
                Assert.NotNull(indexer);
                Assert.Equal("Item[Object]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.Item[System.Object]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.Item(System.Object)", indexer.Name);
                Assert.Equal(@"int this[object x]
{
    set;
}", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
            }
        }

        [Fact]
        public void TestGenereateMetadataWithMethodUsingDefaultValue()
        {
            string code = @"
namespace Test1
{
    public class Foo
    {
        public void Test(
            int a = 1, uint b = 1,
            short c = 1, ushort d = 1,
            long e = 1, ulong f= 1,
            byte g = 1, sbyte h = 1,
            char i = '1', string j = ""1"",
            bool k = true, object l = null)
        {
        }
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Equal(1, output.Items.Count);
            {
                var method = output.Items[0].Items[0].Items[0];
                Assert.NotNull(method);
                Assert.Equal(@"public void Test(int a = 1, uint b = 1U, short c = 1, ushort d = 1, long e = 1L, ulong f = 1UL, byte g = 1, sbyte h = 1, char i = '1', string j = ""1"", bool k = true, object l = null)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
        }

        [Fact]
        public void TestGenereateMetadataAsyncWithAssemblyInfoAndCrossReference()
        {
            string referenceCode = @"
namespace Test1
{
    public class Class1
    {
        public void Func1(int i)
        {
            return;
        }
    }
}
";

            string code = @"
namespace Test2
{
    public class Class2 : Test1.Class1
    {
        public void Func1(Test1.Class1 i)
        {
            return;
        }
    }
}
namespace Test1
{
    public class Class2 : Test1.Class1
    {
        public void Func1(Test1.Class1 i)
        {
            return;
        }
    }
}
";
            var referencedAssembly = CreateAssemblyFromCSharpCode(referenceCode, "reference.dll");
            var compilation = CreateCompilationFromCSharpCode(code, MetadataReference.CreateFromFile(referencedAssembly.Location));
            Assert.Equal("test.dll", compilation.AssemblyName);
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Null(output.AssemblyNameList);
            Assert.Null(output.NamespaceName);
            Assert.Equal("test.dll", output.Items[0].AssemblyNameList.First());
            Assert.Null(output.Items[0].NamespaceName);
            Assert.Equal("test.dll", output.Items[0].Items[0].AssemblyNameList.First());
            Assert.Equal("Test2", output.Items[0].Items[0].NamespaceName);
        }

        [Fact]
        [Trait("Related", "Multilanguage")]
        [Trait("Related", "Generic")]
        public void TestGenereateMetadataAsyncWithMultilanguage()
        {
            string code = @"
namespace Test1
{
    public class Foo<T>
    {
        public void Bar<K>(int i)
        {
        }
        public int this[int index]
        {
            get { return index; }
        }
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            var type = output.Items[0].Items[0];
            Assert.NotNull(type);
            Assert.Equal("Foo<T>", type.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.Equal("Foo(Of T)", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.Equal("Test1.Foo<T>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.Equal("Test1.Foo(Of T)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.Equal("Test1.Foo`1", type.Name);

            {
                var method = output.Items[0].Items[0].Items[0];
                Assert.NotNull(method);
                Assert.Equal("Bar<K>(Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Bar(Of K)(Int32)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo<T>.Bar<K>(System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo(Of T).Bar(Of K)(System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.Bar``1(System.Int32)", method.Name);
                Assert.Equal(1, method.Syntax.Parameters.Count);
                var parameter = method.Syntax.Parameters[0];
                Assert.Equal("i", parameter.Name);
                Assert.Equal("System.Int32", parameter.Type);
                var returnValue = method.Syntax.Return;
                Assert.Null(returnValue);
            }

            {
                var indexer = output.Items[0].Items[0].Items[1];
                Assert.NotNull(indexer);
                Assert.Equal("Item[Int32]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Item(Int32)", indexer.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo<T>.Item[System.Int32]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo(Of T).Item(System.Int32)", indexer.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.Item(System.Int32)", indexer.Name);
                Assert.Equal(1, indexer.Syntax.Parameters.Count);
                var parameter = indexer.Syntax.Parameters[0];
                Assert.Equal("index", parameter.Name);
                Assert.Equal("System.Int32", parameter.Type);
                var returnValue = indexer.Syntax.Return;
                Assert.NotNull(returnValue);
                Assert.Equal("System.Int32", returnValue.Type);
            }
        }

        [Fact]
        [Trait("Related", "Generic")]
        [Trait("Related", "Inheritance")]
        public void TestGenereateMetadataAsyncWithGenericInheritance()
        {
            string code = @"
using System.Collections.Generic;
namespace Test1
{
    public class Foo<T>
        : Dictionary<string, T>
    {
    }
    public class Foo<T1, T2, T3>
        : List<T3>
    {
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            {
                var type = output.Items[0].Items[0];
                Assert.NotNull(type);
                Assert.Equal("Foo<T>", type.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo(Of T)", type.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo<T>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo(Of T)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1", type.Name);
                Assert.Equal(2, type.Inheritance.Count);
                Assert.Equal("System.Collections.Generic.Dictionary{System.String,{T}}", type.Inheritance[1]);
            }
            {
                var type = output.Items[0].Items[1];
                Assert.NotNull(type);
                Assert.Equal("Foo<T1, T2, T3>", type.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo(Of T1, T2, T3)", type.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo<T1, T2, T3>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo(Of T1, T2, T3)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`3", type.Name);
                Assert.Equal(2, type.Inheritance.Count);
                Assert.Equal("System.Collections.Generic.List{{T3}}", type.Inheritance[1]);
            }
        }

        [Trait("Related", "Dynamic")]
        [Trait("Related", "Multilanguage")]
        [Fact]
        public void TestGenereateMetadataWithDynamic()
        {
            string code = @"
namespace Test1
{
    public abstract class Foo
    {
        public dynamic F = 1;
        public dynamic M(dynamic arg) => null;
        public dynamic P { get; protected set; } = "";
        public dynamic this[dynamic index] { get; } => 1;
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Equal(1, output.Items.Count);
            {
                var field = output.Items[0].Items[0].Items[0];
                Assert.NotNull(field);
                Assert.Equal("F", field.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("F", field.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.F", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.F", field.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.F", field.Name);
                Assert.Equal("public dynamic F", field.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal("Public F As Object", field.Syntax.Content[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[1];
                Assert.NotNull(method);
                Assert.Equal("M(Object)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("M(Object)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.M(System.Object)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.M(System.Object)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.M(System.Object)", method.Name);
                Assert.Equal("public dynamic M(dynamic arg)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal("Public Function M(arg As Object) As Object", method.Syntax.Content[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[2];
                Assert.NotNull(method);
                Assert.Equal("P", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("P", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.P", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.P", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.P", method.Name);
                Assert.Equal(@"public dynamic P
{
    get;
    protected set;
}", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(@"Public Property P As Object", method.Syntax.Content[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[3];
                Assert.NotNull(method);
                Assert.Equal("Item[Object]", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Item(Object)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.Item[System.Object]", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.Item(System.Object)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.Item(System.Object)", method.Name);
                Assert.Equal(@"public dynamic this[dynamic index]
{
    get;
}", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(@"Public ReadOnly Property Item(index As Object) As Object", method.Syntax.Content[SyntaxLanguage.VB]);
            }
        }

        [Fact]
        [Trait("Related", "Generic")]
        public void TestGenereateMetadataAsyncWithNestedGeneric()
        {
            string code = @"
using System.Collections.Generic;
namespace Test1
{
    public class Foo<T1, T2>
    {
        public class Bar<T3> { }
        public class FooBar { }
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            {
                var type = output.Items[0].Items[0];
                Assert.NotNull(type);
                Assert.Equal("Foo<T1, T2>", type.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo(Of T1, T2)", type.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo<T1, T2>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo(Of T1, T2)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`2", type.Name);
                Assert.Equal(1, type.Inheritance.Count);
                Assert.Equal("System.Object", type.Inheritance[0]);
            }
            {
                var type = output.Items[0].Items[1];
                Assert.NotNull(type);
                Assert.Equal("Foo<T1, T2>.Bar<T3>", type.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo(Of T1, T2).Bar(Of T3)", type.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo<T1, T2>.Bar<T3>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo(Of T1, T2).Bar(Of T3)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`2.Bar`1", type.Name);
                Assert.Equal(1, type.Inheritance.Count);
                Assert.Equal("System.Object", type.Inheritance[0]);
            }
            {
                var type = output.Items[0].Items[2];
                Assert.NotNull(type);
                Assert.Equal("Foo<T1, T2>.FooBar", type.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo(Of T1, T2).FooBar", type.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo<T1, T2>.FooBar", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo(Of T1, T2).FooBar", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`2.FooBar", type.Name);
                Assert.Equal(1, type.Inheritance.Count);
                Assert.Equal("System.Object", type.Inheritance[0]);
            }
        }

        private static Compilation CreateCompilationFromCSharpCode(string code, params MetadataReference[] references)
        {
            return CreateCompilationFromCSharpCode(code, "test.dll", references);
        }

        private static Compilation CreateCompilationFromCSharpCode(string code, string assemblyName, params MetadataReference[] references)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(code);
            var defaultReferences = new List<MetadataReference> { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };
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

        private static Assembly CreateAssemblyFromCSharpCode(string code, string assemblyName)
        {
            // MemoryStream fails when MetadataReference.CreateFromAssembly with error: Empty path name is not legal
            var compilation = CreateCompilationFromCSharpCode(code);
            EmitResult result;
            using (FileStream stream = new FileStream(assemblyName, FileMode.Create))
            {
                result = compilation.Emit(stream);
            }

            Assert.True(result.Success, string.Join(",", result.Diagnostics.Select(s => s.GetMessage())));
            return Assembly.LoadFile(Path.GetFullPath(assemblyName));
        }

    }
}
