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
    [Trait("EntityType", "Model")]
    [Collection("docfx STA")]
    public class GenerateMetadataFromCSUnitTest
    {
        private static readonly MSBuildWorkspace Workspace = MSBuildWorkspace.Create();

        [Fact]
        [Trait("Related", "Attribute")]
        public void TestGenerateMetadataAsyncWithFuncVoidReturn()
        {
            string code = @"
using System;

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
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            var @class = output.Items[0].Items[0];
            Assert.NotNull(@class);
            Assert.Equal("Class1", @class.DisplayNames.First().Value);
            Assert.Equal("Class1", @class.DisplayNamesWithType.First().Value);
            Assert.Equal("Test1.Class1", @class.DisplayQualifiedNames.First().Value);
            Assert.Equal(@"
This is a test
".Replace("\r\n", "\n"), @class.Summary);
            Assert.Equal("Test1.Class1.Func1(System.Int32)", @class.SeeAlsos[0].LinkId);
            Assert.Equal(@"[Serializable]
public class Class1", @class.Syntax.Content[SyntaxLanguage.CSharp]);

            var function = output.Items[0].Items[0].Items[0];
            Assert.NotNull(function);
            Assert.Equal("Func1(Int32)", function.DisplayNames.First().Value);
            Assert.Equal("Class1.Func1(Int32)", function.DisplayNamesWithType.First().Value);
            Assert.Equal("Test1.Class1.Func1(System.Int32)", function.DisplayQualifiedNames.First().Value);
            Assert.Equal("Test1.Class1.Func1(System.Int32)", function.Name);
            Assert.Equal(@"
This is a function
".Replace("\r\n", "\n"), function.Summary);
            Assert.Equal("System.Int32", function.SeeAlsos[0].LinkId);
            Assert.Equal("This is a param as <xref href=\"System.Int32\" data-throw-if-not-resolved=\"false\"></xref>", function.Syntax.Parameters[0].Description);
            Assert.Equal(1, output.Items.Count);
            var parameter = function.Syntax.Parameters[0];
            Assert.Equal("i", parameter.Name);
            Assert.Equal("System.Int32", parameter.Type);
            var returnValue = function.Syntax.Return;
            Assert.Null(returnValue);
        }

        [Fact]
        public void TestGenerateMetadataAsyncWithNamespace()
        {
            string code = @"
namespace Test1.Test2
{
    /// <seealso cref=""Class1""/>
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
            Assert.Equal("Test1.Test2", ns.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.Equal("Test1.Test2", ns.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.Equal(0, ns.Modifiers.Count);
        }

        [Trait("Related", "Generic")]
        [Trait("Related", "Reference")]
        [Trait("Related", "TripleSlashComments")]
        [Fact]
        public void TestGenerateMetadataWithGenericClass()
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
            MetadataItem output_preserveRaw = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code), null, options: new ExtractMetadataOptions { PreserveRawInlineComments = true });
            Assert.Equal(1, output.Items.Count);
            {
                var type = output.Items[0].Items[0];
                Assert.NotNull(type);
                Assert.Equal("Class1<T>", type.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Class1<T>", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Class1<T>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Class1`1", type.Name);
                Assert.Equal(@"public sealed class Class1<T>
    where T : struct, IEnumerable<T>", type.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.NotNull(type.Syntax.TypeParameters);
                Assert.Equal(1, type.Syntax.TypeParameters.Count);
                Assert.Equal("T", type.Syntax.TypeParameters[0].Name);
                Assert.Null(type.Syntax.TypeParameters[0].Type);
                Assert.Equal("The type", type.Syntax.TypeParameters[0].Description);
                Assert.Equal(new[] { "public", "sealed", "class" }, type.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var function = output.Items[0].Items[0].Items[0];
                Assert.NotNull(function);
                Assert.Equal("Func1<TResult>(Nullable<T>, IEnumerable<T>)", function.DisplayNames.First().Value);
                Assert.Equal("Class1<T>.Func1<TResult>(Nullable<T>, IEnumerable<T>)", function.DisplayNamesWithType.First().Value);
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
                Assert.Equal("public TResult? Func1<TResult>(T? x, IEnumerable<T> y)\r\n    where TResult : struct", function.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public" }, function.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var proptery = output.Items[0].Items[0].Items[1];
                Assert.NotNull(proptery);
                Assert.Equal("Items", proptery.DisplayNames.First().Value);
                Assert.Equal("Class1<T>.Items", proptery.DisplayNamesWithType.First().Value);
                Assert.Equal("Test1.Class1<T>.Items", proptery.DisplayQualifiedNames.First().Value);
                Assert.Equal("Test1.Class1`1.Items", proptery.Name);
                Assert.Equal(0, proptery.Syntax.Parameters.Count);
                var returnValue = proptery.Syntax.Return;
                Assert.NotNull(returnValue.Type);
                Assert.Equal("System.Collections.Generic.IEnumerable{{T}}", returnValue.Type);
                Assert.Equal(@"public IEnumerable<T> Items { get; set; }", proptery.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "get", "set" }, proptery.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var event1 = output.Items[0].Items[0].Items[2];
                Assert.NotNull(event1);
                Assert.Equal("Event1", event1.DisplayNames.First().Value);
                Assert.Equal("Class1<T>.Event1", event1.DisplayNamesWithType.First().Value);
                Assert.Equal("Test1.Class1<T>.Event1", event1.DisplayQualifiedNames.First().Value);
                Assert.Equal("Test1.Class1`1.Event1", event1.Name);
                Assert.Null(event1.Syntax.Parameters);
                Assert.Equal("EventHandler", event1.Syntax.Return.Type);
                Assert.Equal("public event EventHandler Event1", event1.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public" }, event1.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var operator1 = output.Items[0].Items[0].Items[3];
                Assert.NotNull(operator1);
                Assert.Equal("Equality(Class1<T>, Class1<T>)", operator1.DisplayNames.First().Value);
                Assert.Equal("Class1<T>.Equality(Class1<T>, Class1<T>)", operator1.DisplayNamesWithType.First().Value);
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
                Assert.Equal(new[] { "public", "static" }, operator1.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var proptery = output.Items[0].Items[0].Items[4];
                Assert.NotNull(proptery);
                Assert.Equal("Items2", proptery.DisplayNames.First().Value);
                Assert.Equal("Class1<T>.Items2", proptery.DisplayNamesWithType.First().Value);
                Assert.Equal("Test1.Class1<T>.Items2", proptery.DisplayQualifiedNames.First().Value);
                Assert.Equal("Test1.Class1`1.Items2", proptery.Name);
                Assert.Equal(0, proptery.Syntax.Parameters.Count);
                var returnValue = proptery.Syntax.Return;
                Assert.NotNull(returnValue.Type);
                Assert.Equal("System.Collections.Generic.IEnumerable{{T}}", returnValue.Type);
                Assert.Equal(@"public IEnumerable<T> Items2 { get; }", proptery.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "get" }, proptery.Modifiers[SyntaxLanguage.CSharp]);
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
        public void TestGenerateMetadataWithInterface()
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
                Assert.Equal("IFoo.Bar(Int32)", method.DisplayNamesWithType.First().Value);
                Assert.Equal("Test1.IFoo.Bar(System.Int32)", method.DisplayQualifiedNames.First().Value);
                Assert.Equal("Test1.IFoo.Bar(System.Int32)", method.Name);
                var parameter = method.Syntax.Parameters[0];
                Assert.Equal("x", parameter.Name);
                Assert.Equal("System.Int32", parameter.Type);
                var returnValue = method.Syntax.Return;
                Assert.NotNull(returnValue);
                Assert.NotNull(returnValue.Type);
                Assert.Equal("System.String", returnValue.Type);
                Assert.Equal(new string[0], method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var property = output.Items[0].Items[0].Items[1];
                Assert.NotNull(property);
                Assert.Equal("Count", property.DisplayNames.First().Value);
                Assert.Equal("IFoo.Count", property.DisplayNamesWithType.First().Value);
                Assert.Equal("Test1.IFoo.Count", property.DisplayQualifiedNames.First().Value);
                Assert.Equal("Test1.IFoo.Count", property.Name);
                Assert.Equal(0, property.Syntax.Parameters.Count);
                var returnValue = property.Syntax.Return;
                Assert.NotNull(returnValue);
                Assert.NotNull(returnValue.Type);
                Assert.Equal("System.Int32", returnValue.Type);
                Assert.Equal(new[] { "get" }, property.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var @event = output.Items[0].Items[0].Items[2];
                Assert.NotNull(@event);
                Assert.Equal("FooBar", @event.DisplayNames.First().Value);
                Assert.Equal("IFoo.FooBar", @event.DisplayNamesWithType.First().Value);
                Assert.Equal("Test1.IFoo.FooBar", @event.DisplayQualifiedNames.First().Value);
                Assert.Equal("Test1.IFoo.FooBar", @event.Name);
                Assert.Equal("event EventHandler FooBar", @event.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Null(@event.Syntax.Parameters);
                Assert.Equal("EventHandler", @event.Syntax.Return.Type);
                Assert.Equal(new string[0], @event.Modifiers[SyntaxLanguage.CSharp]);
            }
        }

        [Fact]
        public void TestGenerateMetadataWithInterfaceAndInherits()
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
            Assert.Equal("IFoo", ifoo.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.Equal("Test1.IFoo", ifoo.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.Equal("public interface IFoo", ifoo.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.Equal(new[] { "public", "interface" }, ifoo.Modifiers[SyntaxLanguage.CSharp]);

            var ibar = output.Items[0].Items[1];
            Assert.NotNull(ibar);
            Assert.Equal("IBar", ibar.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.Equal("IBar", ibar.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.Equal("Test1.IBar", ibar.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.Equal("public interface IBar : IFoo", ibar.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.Equal(new[] { "public", "interface" }, ibar.Modifiers[SyntaxLanguage.CSharp]);

            var ifoobar = output.Items[0].Items[2];
            Assert.NotNull(ifoobar);
            Assert.Equal("IFooBar", ifoobar.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.Equal("IFooBar", ifoobar.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.Equal("Test1.IFooBar", ifoobar.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.Equal("public interface IFooBar : IBar, IFoo", ifoobar.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.Equal(new[] { "public", "interface" }, ifoobar.Modifiers[SyntaxLanguage.CSharp]);
        }

        [Trait("Related", "Generic")]
        [Trait("Related", "Inheritance")]
        [Trait("Related", "Reference")]
        [Fact]
        public void TestGenerateMetadataWithClassAndInherits()
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
            Assert.Equal("Foo<T>", foo.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.Equal("Test1.Foo<T>", foo.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.Equal("public class Foo<T> : IFoo", foo.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.NotNull(foo.Implements);
            Assert.Equal(1, foo.Implements.Count);
            Assert.Equal(new[] { "Test1.IFoo" }, foo.Implements);
            Assert.Equal(new[] { "public", "class" }, foo.Modifiers[SyntaxLanguage.CSharp]);


            var bar = output.Items[0].Items[1];
            Assert.NotNull(bar);
            Assert.Equal("Bar<T>", bar.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.Equal("Bar<T>", bar.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.Equal("Test1.Bar<T>", bar.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.Equal("public class Bar<T> : Foo<T[]>, IFoo, IBar", bar.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.Equal(new[] { "System.Object", "Test1.Foo{{T}[]}" }, bar.Inheritance);
            Assert.Equal(new[] { "Test1.IFoo", "Test1.IBar" }, bar.Implements);
            Assert.Equal(new[] { "public", "class" }, bar.Modifiers[SyntaxLanguage.CSharp]);

            var foobar = output.Items[0].Items[2];
            Assert.NotNull(foobar);
            Assert.Equal("FooBar", foobar.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.Equal("FooBar", foobar.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.Equal("Test1.FooBar", foobar.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.Equal("public class FooBar : Bar<string>, IFooBar, IFoo, IBar", foobar.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.Equal(new[] { "System.Object", "Test1.Foo{System.String[]}", "Test1.Bar{System.String}" }, foobar.Inheritance);
            Assert.Equal(new[] { "Test1.IFoo", "Test1.IBar", "Test1.IFooBar" }.OrderBy(s => s), foobar.Implements.OrderBy(s => s));
            Assert.Equal(new[] { "public", "class" }, foobar.Modifiers[SyntaxLanguage.CSharp]);

            Assert.NotNull(output.References);
            Assert.Equal(19, output.References.Count);
            {
                var item = output.References["System.Object"];
                Assert.Equal("System", item.Parent);
                Assert.NotNull(item);
                Assert.Equal(1, item.Parts[SyntaxLanguage.CSharp].Count);

                Assert.Equal("System.Object", item.Parts[SyntaxLanguage.CSharp][0].Name);
                Assert.Equal("Object", item.Parts[SyntaxLanguage.CSharp][0].DisplayName);
                Assert.Equal("Object", item.Parts[SyntaxLanguage.CSharp][0].DisplayNamesWithType);
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
                Assert.Equal("Bar", item.Parts[SyntaxLanguage.CSharp][0].DisplayNamesWithType);
                Assert.Equal("Test1.Bar", item.Parts[SyntaxLanguage.CSharp][0].DisplayQualifiedNames);

                Assert.Null(item.Parts[SyntaxLanguage.CSharp][1].Name);
                Assert.Equal("<", item.Parts[SyntaxLanguage.CSharp][1].DisplayName);
                Assert.Equal("<", item.Parts[SyntaxLanguage.CSharp][1].DisplayNamesWithType);
                Assert.Equal("<", item.Parts[SyntaxLanguage.CSharp][1].DisplayQualifiedNames);

                Assert.Equal("System.String", item.Parts[SyntaxLanguage.CSharp][2].Name);
                Assert.Equal("String", item.Parts[SyntaxLanguage.CSharp][2].DisplayNamesWithType);
                Assert.Equal("String", item.Parts[SyntaxLanguage.CSharp][2].DisplayName);
                Assert.Equal("System.String", item.Parts[SyntaxLanguage.CSharp][2].DisplayQualifiedNames);

                Assert.Null(item.Parts[SyntaxLanguage.CSharp][3].Name);
                Assert.Equal(">", item.Parts[SyntaxLanguage.CSharp][3].DisplayName);
                Assert.Equal(">", item.Parts[SyntaxLanguage.CSharp][3].DisplayNamesWithType);
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
                Assert.Equal("Foo", item.Parts[SyntaxLanguage.CSharp][0].DisplayNamesWithType);
                Assert.Equal("Test1.Foo", item.Parts[SyntaxLanguage.CSharp][0].DisplayQualifiedNames);

                Assert.Null(item.Parts[SyntaxLanguage.CSharp][1].Name);
                Assert.Equal("<", item.Parts[SyntaxLanguage.CSharp][1].DisplayName);
                Assert.Equal("<", item.Parts[SyntaxLanguage.CSharp][1].DisplayNamesWithType);
                Assert.Equal("<", item.Parts[SyntaxLanguage.CSharp][1].DisplayQualifiedNames);

                Assert.Null(item.Parts[SyntaxLanguage.CSharp][2].Name);
                Assert.Equal("T", item.Parts[SyntaxLanguage.CSharp][2].DisplayName);
                Assert.Equal("T", item.Parts[SyntaxLanguage.CSharp][2].DisplayNamesWithType);
                Assert.Equal("T", item.Parts[SyntaxLanguage.CSharp][2].DisplayQualifiedNames);

                Assert.Null(item.Parts[SyntaxLanguage.CSharp][3].Name);
                Assert.Equal("[]", item.Parts[SyntaxLanguage.CSharp][3].DisplayName);
                Assert.Equal("[]", item.Parts[SyntaxLanguage.CSharp][3].DisplayNamesWithType);
                Assert.Equal("[]", item.Parts[SyntaxLanguage.CSharp][3].DisplayQualifiedNames);

                Assert.Null(item.Parts[SyntaxLanguage.CSharp][4].Name);
                Assert.Equal(">", item.Parts[SyntaxLanguage.CSharp][4].DisplayName);
                Assert.Equal(">", item.Parts[SyntaxLanguage.CSharp][4].DisplayNamesWithType);
                Assert.Equal(">", item.Parts[SyntaxLanguage.CSharp][4].DisplayQualifiedNames);
            }
            {
                var item = output.References["Test1.Foo{System.String[]}"];
                Assert.NotNull(item);
                Assert.Equal("Test1.Foo`1", item.Definition);
                Assert.Equal("Test1", item.Parent);
                Assert.Equal(5, item.Parts[SyntaxLanguage.CSharp].Count);

                Assert.Equal("Test1.Foo`1", item.Parts[SyntaxLanguage.CSharp][0].Name);
                Assert.Equal("Foo", item.Parts[SyntaxLanguage.CSharp][0].DisplayNamesWithType);
                Assert.Equal("Foo", item.Parts[SyntaxLanguage.CSharp][0].DisplayName);
                Assert.Equal("Test1.Foo", item.Parts[SyntaxLanguage.CSharp][0].DisplayQualifiedNames);

                Assert.Null(item.Parts[SyntaxLanguage.CSharp][1].Name);
                Assert.Equal("<", item.Parts[SyntaxLanguage.CSharp][1].DisplayName);
                Assert.Equal("<", item.Parts[SyntaxLanguage.CSharp][1].DisplayNamesWithType);
                Assert.Equal("<", item.Parts[SyntaxLanguage.CSharp][1].DisplayQualifiedNames);

                Assert.Equal("System.String", item.Parts[SyntaxLanguage.CSharp][2].Name);
                Assert.Equal("String", item.Parts[SyntaxLanguage.CSharp][2].DisplayName);
                Assert.Equal("String", item.Parts[SyntaxLanguage.CSharp][2].DisplayNamesWithType);
                Assert.Equal("System.String", item.Parts[SyntaxLanguage.CSharp][2].DisplayQualifiedNames);

                Assert.Null(item.Parts[SyntaxLanguage.CSharp][3].Name);
                Assert.Equal("[]", item.Parts[SyntaxLanguage.CSharp][3].DisplayName);
                Assert.Equal("[]", item.Parts[SyntaxLanguage.CSharp][3].DisplayNamesWithType);
                Assert.Equal("[]", item.Parts[SyntaxLanguage.CSharp][3].DisplayQualifiedNames);

                Assert.Null(item.Parts[SyntaxLanguage.CSharp][4].Name);
                Assert.Equal(">", item.Parts[SyntaxLanguage.CSharp][4].DisplayName);
                Assert.Equal(">", item.Parts[SyntaxLanguage.CSharp][4].DisplayNamesWithType);
                Assert.Equal(">", item.Parts[SyntaxLanguage.CSharp][4].DisplayQualifiedNames);
            }
        }

        [Fact]
        public void TestGenerateMetadataWithEnum()
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
                Assert.Equal("ABC", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.ABC", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.ABC", type.Name);
                Assert.Equal("public enum ABC", type.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "enum" }, type.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var type = output.Items[0].Items[1];
                Assert.NotNull(type);
                Assert.Equal("YN", type.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("YN", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.YN", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.YN", type.Name);
                Assert.Equal("public enum YN : byte", type.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "enum" }, type.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var type = output.Items[0].Items[2];
                Assert.NotNull(type);
                Assert.Equal("XYZ", type.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("XYZ", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.XYZ", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.XYZ", type.Name);
                Assert.Equal("public enum XYZ", type.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "enum" }, type.Modifiers[SyntaxLanguage.CSharp]);
            }
        }

        [Trait("Related", "Inheritance")]
        [Fact]
        public void TestGenerateMetadataWithStruct()
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
                Assert.Equal("Foo", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo", type.Name);
                Assert.Equal("public struct Foo", type.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Null(type.Implements);
                Assert.Equal(new[] { "public", "struct" }, type.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var type = output.Items[0].Items[1];
                Assert.NotNull(type);
                Assert.Equal("Bar<T>", type.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Bar<T>", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar<T>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar`1", type.Name);
                Assert.Equal("public struct Bar<T> : IEnumerable<T>, IEnumerable", type.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "System.Collections.Generic.IEnumerable{{T}}", "System.Collections.IEnumerable" }, type.Implements);
                Assert.Equal(new[] { "public", "struct" }, type.Modifiers[SyntaxLanguage.CSharp]);
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
        public void TestGenerateMetadataWithDelegate()
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
                Assert.Equal("Foo", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo", type.Name);
                Assert.Equal("public delegate void Foo();", type.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Null(type.Syntax.Parameters);
                Assert.Null(type.Syntax.Return);
                Assert.Equal(new[] { "public", "delegate" }, type.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var type = output.Items[0].Items[1];
                Assert.NotNull(type);
                Assert.Equal("Bar<T>", type.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Bar<T>", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar<T>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar`1", type.Name);
                Assert.Equal("public delegate T Bar<T>(IEnumerable<T> x = null)\r\n    where T : class;", type.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "delegate" }, type.Modifiers[SyntaxLanguage.CSharp]);

                Assert.NotNull(type.Syntax.Parameters);
                Assert.Equal(1, type.Syntax.Parameters.Count);
                Assert.Equal("x", type.Syntax.Parameters[0].Name);
                Assert.Equal("System.Collections.Generic.IEnumerable{{T}}", type.Syntax.Parameters[0].Type);
                Assert.NotNull(type.Syntax.Return);
                Assert.Equal("{T}", type.Syntax.Return.Type);
                Assert.Equal(new[] { "public", "delegate" }, type.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var type = output.Items[0].Items[2];
                Assert.NotNull(type);
                Assert.Equal("FooBar", type.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("FooBar", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
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
                Assert.Equal(new[] { "public", "delegate" }, type.Modifiers[SyntaxLanguage.CSharp]);
            }
        }

        [Trait("Related", "Generic")]
        [Fact]
        public void TestGenerateMetadataWithMethod()
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
                Assert.Equal("Foo<T>.M1()", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.M1()", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.M1", method.Name);
                Assert.Equal("public abstract void M1()", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "abstract" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[1];
                Assert.NotNull(method);
                Assert.Equal("M2<TArg>(TArg)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.M2<TArg>(TArg)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.M2<TArg>(TArg)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.M2``1(``0)", method.Name);
                Assert.Equal("protected virtual Foo<T> M2<TArg>(TArg arg)\r\n    where TArg : Foo<T>", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "protected", "virtual" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[2];
                Assert.NotNull(method);
                Assert.Equal("M3<TResult>(String)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.M3<TResult>(String)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.M3<TResult>(System.String)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.M3``1(System.String)", method.Name);
                Assert.Equal("public static TResult M3<TResult>(string x)\r\n    where TResult : class", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[3];
                Assert.NotNull(method);
                Assert.Equal("M4(Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.M4(Int32)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.M4(System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.M4(System.Int32)", method.Name);
                Assert.Equal("public void M4(int x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            // Bar
            {
                var method = output.Items[0].Items[1].Items[0];
                Assert.NotNull(method);
                Assert.Equal("M1()", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Bar.M1()", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.M1()", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.M1", method.Name);
                Assert.Equal("public override void M1()", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo{System.String}.M1", method.Overridden);
                Assert.Equal(new[] { "public", "override" }, method.Modifiers[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.M1", method.Implements[0]);
            }
            {
                var method = output.Items[0].Items[1].Items[1];
                Assert.NotNull(method);
                Assert.Equal("M2<TArg>(TArg)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Bar.M2<TArg>(TArg)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.M2<TArg>(TArg)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.M2``1(``0)", method.Name);
                Assert.Equal("protected override sealed Foo<T> M2<TArg>(TArg arg)\r\n    where TArg : Foo<string>", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo{System.String}.M2``1({TArg})", method.Overridden);
            }
            {
                var method = output.Items[0].Items[1].Items[2];
                Assert.NotNull(method);
                Assert.Equal("M5<TArg>(TArg)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Bar.M5<TArg>(TArg)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.M5<TArg>(TArg)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.M5``1(``0)", method.Name);
                Assert.Equal("public int M5<TArg>(TArg arg)\r\n    where TArg : struct, new()", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public" }, method.Modifiers[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.M5``1({TArg})", method.Implements[0]);
            }
            // IFooBar
            {
                var method = output.Items[0].Items[2].Items[0];
                Assert.NotNull(method);
                Assert.Equal("M1()", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("IFooBar.M1()", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.M1()", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.M1", method.Name);
                Assert.Equal("void M1()", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new string[0], method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[2].Items[1];
                Assert.NotNull(method);
                Assert.Equal("M2<TArg>(TArg)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("IFooBar.M2<TArg>(TArg)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.M2<TArg>(TArg)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.M2``1(``0)", method.Name);
                Assert.Equal("Foo<T> M2<TArg>(TArg arg)\r\n    where TArg : Foo<string>", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new string[0], method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[2].Items[2];
                Assert.NotNull(method);
                Assert.Equal("M5<TArg>(TArg)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("IFooBar.M5<TArg>(TArg)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.M5<TArg>(TArg)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.M5``1(``0)", method.Name);
                Assert.Equal("int M5<TArg>(TArg arg)\r\n    where TArg : struct, new()", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new string[0], method.Modifiers[SyntaxLanguage.CSharp]);
            }
        }

        [Trait("Related", "Generic")]
        [Trait("Related", "EII")]
        [Fact]
        public void TestGenerateMetadataWithEii()
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
        public bool IFoo.Global { get; set; }
    }
    public interface IFoo
    {
        object Bar(ref int x);
        event EventHandler E;
        bool Global { get; set;}
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
                Assert.Equal("Foo<T>", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1", type.Name);
                Assert.Equal(@"public class Foo<T> : IFoo, IFoo<string>, IFoo<T> where T : class", type.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "class" }, type.Modifiers[SyntaxLanguage.CSharp]);
                Assert.Contains("Test1.IFoo", type.Implements);
                Assert.Contains("Test1.IFoo{System.String}", type.Implements);
                Assert.Contains("Test1.IFoo{{T}}", type.Implements);
            }
            {
                var method = output.Items[0].Items[0].Items[0];
                Assert.NotNull(method);
                Assert.True(method.IsExplicitInterfaceImplementation);
                Assert.Equal("IFoo.Bar(ref Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.IFoo.Bar(ref Int32)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Test1.IFoo.Bar(ref System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Test1#IFoo#Bar(System.Int32@)", method.Name);
                Assert.Equal(@"object IFoo.Bar(ref int x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new string[0], method.Modifiers[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFoo.Bar(System.Int32@)", method.Implements[0]);
            }
            {
                var method = output.Items[0].Items[0].Items[1];
                Assert.NotNull(method);
                Assert.True(method.IsExplicitInterfaceImplementation);
                Assert.Equal("IFoo<String>.Bar<TArg>(TArg[])", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.IFoo<String>.Bar<TArg>(TArg[])", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Test1.IFoo<System.String>.Bar<TArg>(TArg[])", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Test1#IFoo{System#String}#Bar``1(``0[])", method.Name);
                Assert.Equal(@"string IFoo<string>.Bar<TArg>(TArg[] x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new string[0], method.Modifiers[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFoo{System.String}.Bar``1({TArg}[])", method.Implements[0]);
            }
            {
                var method = output.Items[0].Items[0].Items[2];
                Assert.NotNull(method);
                Assert.True(method.IsExplicitInterfaceImplementation);
                Assert.Equal("IFoo<T>.Bar<TArg>(TArg[])", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.IFoo<T>.Bar<TArg>(TArg[])", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Test1.IFoo<T>.Bar<TArg>(TArg[])", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Test1#IFoo{T}#Bar``1(``0[])", method.Name);
                Assert.Equal(@"T IFoo<T>.Bar<TArg>(TArg[] x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new string[0], method.Modifiers[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFoo{{T}}.Bar``1({TArg}[])", method.Implements[0]);
            }
            {
                var p = output.Items[0].Items[0].Items[3];
                Assert.NotNull(p);
                Assert.True(p.IsExplicitInterfaceImplementation);
                Assert.Equal("IFoo<String>.P", p.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.IFoo<String>.P", p.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Test1.IFoo<System.String>.P", p.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Test1#IFoo{System#String}#P", p.Name);
                Assert.Equal(@"string IFoo<string>.P { get; set; }", p.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "get", "set" }, p.Modifiers[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFoo{System.String}.P", p.Implements[0]);
            }
            {
                var p = output.Items[0].Items[0].Items[4];
                Assert.NotNull(p);
                Assert.True(p.IsExplicitInterfaceImplementation);
                Assert.Equal("IFoo<T>.P", p.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.IFoo<T>.P", p.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Test1.IFoo<T>.P", p.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Test1#IFoo{T}#P", p.Name);
                Assert.Equal(@"T IFoo<T>.P { get; set; }", p.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "get", "set" }, p.Modifiers[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFoo{{T}}.P", p.Implements[0]);
            }
            {
                var p = output.Items[0].Items[0].Items[5];
                Assert.NotNull(p);
                Assert.True(p.IsExplicitInterfaceImplementation);
                Assert.Equal("IFoo<String>.Item[String]", p.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.IFoo<String>.Item[String]", p.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Test1.IFoo<System.String>.Item[System.String]", p.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Test1#IFoo{System#String}#Item(System.String)", p.Name);
                Assert.Equal(@"int IFoo<string>.this[string x] { get; }", p.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "get", }, p.Modifiers[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFoo{System.String}.Item(System.String)", p.Implements[0]);
            }
            {
                var p = output.Items[0].Items[0].Items[6];
                Assert.NotNull(p);
                Assert.True(p.IsExplicitInterfaceImplementation);
                Assert.Equal("IFoo<T>.Item[T]", p.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.IFoo<T>.Item[T]", p.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Test1.IFoo<T>.Item[T]", p.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Test1#IFoo{T}#Item(`0)", p.Name);
                Assert.Equal(@"int IFoo<T>.this[T x] { get; }", p.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "get", }, p.Modifiers[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFoo{{T}}.Item({T})", p.Implements[0]);
            }
            {
                var e = output.Items[0].Items[0].Items[7];
                Assert.NotNull(e);
                Assert.True(e.IsExplicitInterfaceImplementation);
                Assert.Equal("IFoo.E", e.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.IFoo.E", e.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Test1.IFoo.E", e.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Test1#IFoo#E", e.Name);
                Assert.Equal(@"event EventHandler IFoo.E", e.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new string[0], e.Modifiers[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFoo.E", e.Implements[0]);
            }
        }

        [Trait("Related", "Generic")]
        [Trait("Related", "EII")]
        [Fact]
        public void TestGenerateMetadataWithEditorBrowsableNeverEii()
        {
            string code = @"
namespace Test
{
    using System.ComponentModel;
    public interface IInterface
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        bool Method();
        [EditorBrowsable(EditorBrowsableState.Never)]
        bool Property { get; }
        [EditorBrowsable(EditorBrowsableState.Never)]
        event EventHandler Event;
    }

    public class Class : IInterface
    {
        bool IInterface.Method() { return false; }
        bool IInterface.Property { get { return false; } }
        event EventHandler IInterface.Event { add {} remove {} }
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Equal(1, output.Items.Count);
            var ns = output.Items[0];
            Assert.Equal(2, ns.Items.Count);
            {
                var type = ns.Items[0];
                Assert.NotNull(type);
                Assert.Equal("IInterface", type.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test.IInterface", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test.IInterface", type.Name);
                Assert.Equal("public interface IInterface", type.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "interface" }, type.Modifiers[SyntaxLanguage.CSharp]);
                Assert.Null(type.Implements);

                // Verify member with EditorBrowsable.Never should be filtered out
                Assert.Equal(0, type.Items.Count);
            }
            {
                var type = ns.Items[1];
                Assert.NotNull(type);
                Assert.Equal("Class", type.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test.Class", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test.Class", type.Name);
                Assert.Equal("public class Class : IInterface", type.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "class" }, type.Modifiers[SyntaxLanguage.CSharp]);
                Assert.Equal("Test.IInterface", type.Implements[0]);

                // Verify EII member with EditorBrowsable.Never should be filtered out
                Assert.Equal(0, type.Items.Count);
            }
        }

        [Trait("Related", "Generic")]
        [Trait("Related", "Extension Method")]
        [Fact]
        public void TestGenerateMetadataWithExtensionMethod()
        {
            string code = @"
namespace Test1
{
    public abstract class Foo<T>
    {
    }
    public class FooImple<T> : Foo<T[]>
    {
        public void M1<U>(T a, U b) { }
    }
    public class FooImple2<T> : Foo<dynamic>
    {
    }
    public class FooImple3<T> : Foo<Foo<T[]>>
    {
    }
    public class Doll
    {
    }

    public static class Extension
    {
        public static void Eat<Tool>(this FooImple<Tool> impl)
        { }
        public static void Play<Tool, Way>(this Foo<Tool> foo, Tool t, Way w)
        { }
        public static void Rain(this Doll d)
        { }
        public static void Rain(this Doll d, Doll another)
        { }
    }
}
";
            var compilation = CreateCompilationFromCSharpCode(code);
            MetadataItem output = GenerateYamlMetadata(compilation, options: new ExtractMetadataOptions { RoslynExtensionMethods = GetAllExtensionMethodsFromCompilation(new[] { compilation }) });
            Assert.Equal(1, output.Items.Count);
            // FooImple<T>
            {
                var method = output.Items[0].Items[1].Items[0];
                Assert.NotNull(method);
                Assert.Equal("M1<U>(T, U)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("FooImple<T>.M1<U>(T, U)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.FooImple<T>.M1<U>(T, U)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.FooImple`1.M1``1(`0,``0)", method.Name);
                Assert.Equal("public void M1<U>(T a, U b)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            var extensionMethods = output.Items[0].Items[1].ExtensionMethods;
            Assert.Equal(2, extensionMethods.Count);
            {
                Assert.Equal("Test1.FooImple`1.Test1.Extension.Eat``1", extensionMethods[0]);
                var reference = output.References[extensionMethods[0]];
                Assert.Equal(false, reference.IsDefinition);
                Assert.Equal("Test1.Extension.Eat``1(Test1.FooImple{``0})", reference.Definition);
                Assert.Equal("Eat<T>()", string.Concat(reference.Parts[SyntaxLanguage.CSharp].Select(n => n.DisplayName)));
                Assert.Equal("Extension.Eat<T>()", string.Concat(reference.Parts[SyntaxLanguage.CSharp].Select(n => n.DisplayNamesWithType)));
            }
            {
                Assert.Equal("Test1.Foo{`0[]}.Test1.Extension.Play``2({T}[],{Way})", extensionMethods[1]);
                var reference = output.References[extensionMethods[1]];
                Assert.Equal(false, reference.IsDefinition);
                Assert.Equal("Test1.Extension.Play``2(Test1.Foo{``0},``0,``1)", reference.Definition);
                Assert.Equal("Play<T[], Way>(T[], Way)", string.Concat(reference.Parts[SyntaxLanguage.CSharp].Select(n => n.DisplayName)));
                Assert.Equal("Extension.Play<T[], Way>(T[], Way)", string.Concat(reference.Parts[SyntaxLanguage.CSharp].Select(n => n.DisplayNamesWithType)));
            }
            // FooImple2<T>
            extensionMethods = output.Items[0].Items[2].ExtensionMethods;
            Assert.Equal(1, extensionMethods.Count);
            {
                Assert.Equal("Test1.Foo{System.Object}.Test1.Extension.Play``2(System.Object,{Way})", extensionMethods[0]);
                var reference = output.References[extensionMethods[0]];
                Assert.Equal(false, reference.IsDefinition);
                Assert.Equal("Test1.Extension.Play``2(Test1.Foo{``0},``0,``1)", reference.Definition);
                Assert.Equal("Play<Object, Way>(Object, Way)", string.Concat(reference.Parts[SyntaxLanguage.CSharp].Select(n => n.DisplayName)));
                Assert.Equal("Extension.Play<Object, Way>(Object, Way)", string.Concat(reference.Parts[SyntaxLanguage.CSharp].Select(n => n.DisplayNamesWithType)));
            }
            // FooImple3<T>
            extensionMethods = output.Items[0].Items[3].ExtensionMethods;
            Assert.Equal(1, extensionMethods.Count);
            {
                Assert.Equal("Test1.Foo{Test1.Foo{`0[]}}.Test1.Extension.Play``2(Test1.Foo{{T}[]},{Way})", extensionMethods[0]);
                var reference = output.References[extensionMethods[0]];
                Assert.Equal(false, reference.IsDefinition);
                Assert.Equal("Test1.Extension.Play``2(Test1.Foo{``0},``0,``1)", reference.Definition);
                Assert.Equal("Play<Foo<T[]>, Way>(Foo<T[]>, Way)", string.Concat(reference.Parts[SyntaxLanguage.CSharp].Select(n => n.DisplayName)));
                Assert.Equal("Extension.Play<Foo<T[]>, Way>(Foo<T[]>, Way)", string.Concat(reference.Parts[SyntaxLanguage.CSharp].Select(n => n.DisplayNamesWithType)));
            }
            // Doll
            extensionMethods = output.Items[0].Items[4].ExtensionMethods;
            Assert.Equal(2, extensionMethods.Count);
            {
                Assert.Equal("Test1.Doll.Test1.Extension.Rain", extensionMethods[0]);
                var reference = output.References[extensionMethods[0]];
                Assert.Equal(false, reference.IsDefinition);
                Assert.Equal("Test1.Extension.Rain(Test1.Doll)", reference.Definition);
                Assert.Equal("Rain()", string.Concat(reference.Parts[SyntaxLanguage.CSharp].Select(n => n.DisplayName)));
                Assert.Equal("Extension.Rain()", string.Concat(reference.Parts[SyntaxLanguage.CSharp].Select(n => n.DisplayNamesWithType)));
            }
            {
                Assert.Equal("Test1.Doll.Test1.Extension.Rain(Test1.Doll)", extensionMethods[1]);
                var reference = output.References[extensionMethods[1]];
                Assert.Equal(false, reference.IsDefinition);
                Assert.Equal("Test1.Extension.Rain(Test1.Doll,Test1.Doll)", reference.Definition);
                Assert.Equal("Rain(Doll)", string.Concat(reference.Parts[SyntaxLanguage.CSharp].Select(n => n.DisplayName)));
                Assert.Equal("Extension.Rain(Doll)", string.Concat(reference.Parts[SyntaxLanguage.CSharp].Select(n => n.DisplayNamesWithType)));
            }
        }

        [Fact]
        public void TestGenerateMetadataWithOperator()
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
                Assert.Equal("Foo.UnaryPlus(Foo)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.UnaryPlus(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_UnaryPlus(Test1.Foo)", method.Name);
                Assert.Equal(@"public static Foo operator +(Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[1];
                Assert.NotNull(method);
                Assert.Equal("UnaryNegation(Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.UnaryNegation(Foo)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.UnaryNegation(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_UnaryNegation(Test1.Foo)", method.Name);
                Assert.Equal(@"public static Foo operator -(Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[2];
                Assert.NotNull(method);
                Assert.Equal("LogicalNot(Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.LogicalNot(Foo)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.LogicalNot(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_LogicalNot(Test1.Foo)", method.Name);
                Assert.Equal(@"public static Foo operator !(Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[3];
                Assert.NotNull(method);
                Assert.Equal("OnesComplement(Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.OnesComplement(Foo)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.OnesComplement(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_OnesComplement(Test1.Foo)", method.Name);
                Assert.Equal(@"public static Foo operator ~(Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[4];
                Assert.NotNull(method);
                Assert.Equal("Increment(Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.Increment(Foo)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.Increment(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_Increment(Test1.Foo)", method.Name);
                Assert.Equal(@"public static Foo operator ++(Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[5];
                Assert.NotNull(method);
                Assert.Equal("Decrement(Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.Decrement(Foo)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.Decrement(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_Decrement(Test1.Foo)", method.Name);
                Assert.Equal(@"public static Foo operator --(Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[6];
                Assert.NotNull(method);
                Assert.Equal("True(Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.True(Foo)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.True(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_True(Test1.Foo)", method.Name);
                Assert.Equal(@"public static Foo operator true (Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[7];
                Assert.NotNull(method);
                Assert.Equal("False(Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.False(Foo)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.False(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_False(Test1.Foo)", method.Name);
                Assert.Equal(@"public static Foo operator false (Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            // binary
            {
                var method = output.Items[0].Items[0].Items[8];
                Assert.NotNull(method);
                Assert.Equal("Addition(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.Addition(Foo, Int32)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.Addition(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_Addition(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static Foo operator +(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[9];
                Assert.NotNull(method);
                Assert.Equal("Subtraction(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.Subtraction(Foo, Int32)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.Subtraction(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_Subtraction(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static Foo operator -(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[10];
                Assert.NotNull(method);
                Assert.Equal("Multiply(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.Multiply(Foo, Int32)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.Multiply(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_Multiply(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static Foo operator *(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[11];
                Assert.NotNull(method);
                Assert.Equal("Division(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.Division(Foo, Int32)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.Division(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_Division(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static Foo operator /(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[12];
                Assert.NotNull(method);
                Assert.Equal("Modulus(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.Modulus(Foo, Int32)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.Modulus(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_Modulus(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static Foo operator %(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[13];
                Assert.NotNull(method);
                Assert.Equal("BitwiseAnd(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.BitwiseAnd(Foo, Int32)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.BitwiseAnd(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_BitwiseAnd(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static Foo operator &(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[14];
                Assert.NotNull(method);
                Assert.Equal("BitwiseOr(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.BitwiseOr(Foo, Int32)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.BitwiseOr(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_BitwiseOr(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static Foo operator |(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[15];
                Assert.NotNull(method);
                Assert.Equal("ExclusiveOr(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.ExclusiveOr(Foo, Int32)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.ExclusiveOr(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_ExclusiveOr(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static Foo operator ^(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[16];
                Assert.NotNull(method);
                Assert.Equal("RightShift(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.RightShift(Foo, Int32)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.RightShift(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_RightShift(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static Foo operator >>(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[17];
                Assert.NotNull(method);
                Assert.Equal("LeftShift(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.LeftShift(Foo, Int32)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.LeftShift(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_LeftShift(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static Foo operator <<(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            // comparison
            {
                var method = output.Items[0].Items[0].Items[18];
                Assert.NotNull(method);
                Assert.Equal("Equality(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.Equality(Foo, Int32)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.Equality(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_Equality(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static bool operator ==(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[19];
                Assert.NotNull(method);
                Assert.Equal("Inequality(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.Inequality(Foo, Int32)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.Inequality(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_Inequality(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static bool operator !=(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[20];
                Assert.NotNull(method);
                Assert.Equal("GreaterThan(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.GreaterThan(Foo, Int32)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.GreaterThan(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_GreaterThan(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static bool operator>(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[21];
                Assert.NotNull(method);
                Assert.Equal("LessThan(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.LessThan(Foo, Int32)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.LessThan(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_LessThan(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static bool operator <(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[22];
                Assert.NotNull(method);
                Assert.Equal("GreaterThanOrEqual(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.GreaterThanOrEqual(Foo, Int32)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.GreaterThanOrEqual(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_GreaterThanOrEqual(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static bool operator >=(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[23];
                Assert.NotNull(method);
                Assert.Equal("LessThanOrEqual(Foo, Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.LessThanOrEqual(Foo, Int32)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.LessThanOrEqual(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_LessThanOrEqual(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"public static bool operator <=(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            // conversion
            {
                var method = output.Items[0].Items[0].Items[24];
                Assert.NotNull(method);
                Assert.Equal("Implicit(Int32 to Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.Implicit(Int32 to Foo)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.Implicit(System.Int32 to Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_Implicit(System.Int32)~Test1.Foo", method.Name);
                Assert.Equal(@"public static implicit operator Foo(int x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var method = output.Items[0].Items[0].Items[25];
                Assert.NotNull(method);
                Assert.Equal("Explicit(Foo to Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.Explicit(Foo to Int32)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.Explicit(Test1.Foo to System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.op_Explicit(Test1.Foo)~System.Int32", method.Name);
                Assert.Equal(@"public static explicit operator int (Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "static" }, method.Modifiers[SyntaxLanguage.CSharp]);
            }
        }

        [Trait("Related", "Generic")]
        [Fact]
        public void TestGenerateMetadataWithConstructor()
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
                Assert.Equal("Foo<T>.Foo()", constructor.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Foo()", constructor.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.#ctor", constructor.Name);
                Assert.Equal("public Foo()", constructor.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public" }, constructor.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var constructor = output.Items[0].Items[0].Items[1];
                Assert.NotNull(constructor);
                Assert.Equal("Foo(Int32)", constructor.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.Foo(Int32)", constructor.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Foo(System.Int32)", constructor.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.#ctor(System.Int32)", constructor.Name);
                Assert.Equal("public Foo(int x)", constructor.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public" }, constructor.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var constructor = output.Items[0].Items[0].Items[2];
                Assert.NotNull(constructor);
                Assert.Equal("Foo(String)", constructor.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.Foo(String)", constructor.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Foo(System.String)", constructor.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.#ctor(System.String)", constructor.Name);
                Assert.Equal("protected Foo(string x)", constructor.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "protected" }, constructor.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var constructor = output.Items[0].Items[1].Items[0];
                Assert.NotNull(constructor);
                Assert.Equal("Bar()", constructor.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Bar.Bar()", constructor.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Bar()", constructor.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.#ctor", constructor.Name);
                Assert.Equal("public Bar()", constructor.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public" }, constructor.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var constructor = output.Items[0].Items[1].Items[1];
                Assert.NotNull(constructor);
                Assert.Equal("Bar(Int32)", constructor.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Bar.Bar(Int32)", constructor.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Bar(System.Int32)", constructor.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.#ctor(System.Int32)", constructor.Name);
                Assert.Equal("protected Bar(int x)", constructor.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "protected" }, constructor.Modifiers[SyntaxLanguage.CSharp]);
            }
        }

        [Trait("Related", "Generic")]
        [Fact]
        public void TestGenerateMetadataWithField()
        {
            string code = @"
namespace Test1
{
    public class Foo<T>
    {
        public volatile int X;
        protected static readonly Foo<T> Y = null;
        protected internal const string Z = """";
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
                Assert.Equal("Foo<T>.X", field.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.X", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.X", field.Name);
                Assert.Equal("public volatile int X", field.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "volatile" }, field.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var field = output.Items[0].Items[0].Items[1];
                Assert.NotNull(field);
                Assert.Equal("Y", field.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.Y", field.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Y", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Y", field.Name);
                Assert.Equal("protected static readonly Foo<T> Y", field.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "protected", "static", "readonly" }, field.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var field = output.Items[0].Items[0].Items[2];
                Assert.NotNull(field);
                Assert.Equal("Z", field.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.Z", field.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Z", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Z", field.Name);
                Assert.Equal("protected const string Z = \"\"", field.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "protected", "const" }, field.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var field = output.Items[0].Items[1].Items[0];
                Assert.NotNull(field);
                Assert.Equal("Black", field.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Bar.Black", field.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Black", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Black", field.Name);
                Assert.Equal("Black = 0", field.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "const" }, field.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var field = output.Items[0].Items[1].Items[1];
                Assert.NotNull(field);
                Assert.Equal("Red", field.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Bar.Red", field.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Red", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Red", field.Name);
                Assert.Equal("Red = 1", field.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "const" }, field.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var field = output.Items[0].Items[1].Items[2];
                Assert.NotNull(field);
                Assert.Equal("Blue", field.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Bar.Blue", field.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Blue", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Blue", field.Name);
                Assert.Equal(@"Blue = 2", field.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "const" }, field.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var field = output.Items[0].Items[1].Items[3];
                Assert.NotNull(field);
                Assert.Equal("Green", field.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Bar.Green", field.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Green", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Green", field.Name);
                Assert.Equal("Green = 4", field.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "const" }, field.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var field = output.Items[0].Items[1].Items[4];
                Assert.NotNull(field);
                Assert.Equal("White", field.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Bar.White", field.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.White", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.White", field.Name);
                Assert.Equal(@"White = 7", field.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "const" }, field.Modifiers[SyntaxLanguage.CSharp]);
            }
        }

        [Trait("Related", "Generic")]
        [Fact]
        public void TestGenerateMetadataWithCSharpCodeAndEvent()
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
                Assert.Equal("Foo<T>.A", a.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.A", a.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.A", a.Name);
                Assert.Equal("public event EventHandler A", a.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public" }, a.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var b = output.Items[0].Items[0].Items[1];
                Assert.NotNull(b);
                Assert.Equal("B", b.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.B", b.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.B", b.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.B", b.Name);
                Assert.Equal("protected static event EventHandler B", b.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "protected", "static" }, b.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var c = output.Items[0].Items[0].Items[2];
                Assert.NotNull(c);
                Assert.Equal("C", c.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.C", c.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.C", c.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.C", c.Name);
                Assert.Equal("protected abstract event EventHandler<T> C", c.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "protected", "abstract" }, c.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var d = output.Items[0].Items[0].Items[3];
                Assert.NotNull(d);
                Assert.Equal("D", d.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.D", d.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.D", d.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.D", d.Name);
                Assert.Equal("public virtual event EventHandler<T> D", d.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var a = output.Items[0].Items[1].Items[0];
                Assert.NotNull(a);
                Assert.Equal("A", a.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Bar<T>.A", a.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar<T>.A", a.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar`1.A", a.Name);
                Assert.Equal("public event EventHandler A", a.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public" }, a.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var c = output.Items[0].Items[1].Items[1];
                Assert.NotNull(c);
                Assert.Equal("C", c.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Bar<T>.C", c.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar<T>.C", c.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar`1.C", c.Name);
                Assert.Equal("protected override sealed event EventHandler<T> C", c.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo{{T}}.C", c.Overridden);
                Assert.Equal(new[] { "protected", "override", "sealed" }, c.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var d = output.Items[0].Items[1].Items[2];
                Assert.NotNull(d);
                Assert.Equal("D", d.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Bar<T>.D", d.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar<T>.D", d.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar`1.D", d.Name);
                Assert.Equal("public override event EventHandler<T> D", d.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo{{T}}.D", d.Overridden);
                Assert.Equal(new[] { "public", "override" }, d.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var a = output.Items[0].Items[2].Items[0];
                Assert.NotNull(a);
                Assert.Equal("A", a.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("IFooBar<T>.A", a.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar<T>.A", a.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar`1.A", a.Name);
                Assert.Equal("event EventHandler A", a.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new string[0], a.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var d = output.Items[0].Items[2].Items[1];
                Assert.NotNull(d);
                Assert.Equal("D", d.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("IFooBar<T>.D", d.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar<T>.D", d.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar`1.D", d.Name);
                Assert.Equal("event EventHandler<T> D", d.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new string[0], d.Modifiers[SyntaxLanguage.CSharp]);
            }
        }

        [Trait("Related", "Generic")]
        [Fact]
        public void TestGenerateMetadataWithProperty()
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
                Assert.Equal("Foo<T>.A", a.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.A", a.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.A", a.Name);
                Assert.Equal(@"public int A { get; set; }", a.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "get", "set" }, a.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var b = output.Items[0].Items[0].Items[1];
                Assert.NotNull(b);
                Assert.Equal("B", b.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.B", b.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.B", b.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.B", b.Name);
                Assert.Equal(@"public virtual int B { get; }", b.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "virtual", "get" }, b.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var c = output.Items[0].Items[0].Items[2];
                Assert.NotNull(c);
                Assert.Equal("C", c.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.C", c.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.C", c.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.C", c.Name);
                Assert.Equal(@"public abstract int C { set; }", c.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "abstract", "set" }, c.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var d = output.Items[0].Items[0].Items[3];
                Assert.NotNull(d);
                Assert.Equal("D", d.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.D", d.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.D", d.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.D", d.Name);
                Assert.Equal(@"protected int D { get; }", d.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "protected", "get" }, d.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var e = output.Items[0].Items[0].Items[4];
                Assert.NotNull(e);
                Assert.Equal("E", e.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.E", e.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.E", e.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.E", e.Name);
                Assert.Equal(@"public T E { get; protected set; }", e.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "get", "protected set" }, e.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var f = output.Items[0].Items[0].Items[5];
                Assert.NotNull(f);
                Assert.Equal("F", f.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.F", f.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.F", f.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.F", f.Name);
                Assert.Equal(@"protected static int F { get; set; }", f.Syntax.Content[SyntaxLanguage.CSharp]);
            }
            {
                var a = output.Items[0].Items[1].Items[0];
                Assert.NotNull(a);
                Assert.Equal("A", a.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Bar.A", a.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.A", a.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.A", a.Name);
                Assert.Equal(@"public virtual int A { get; set; }", a.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "virtual", "get", "set" }, a.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var b = output.Items[0].Items[1].Items[1];
                Assert.NotNull(b);
                Assert.Equal("B", b.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Bar.B", b.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.B", b.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.B", b.Name);
                Assert.Equal(@"public override int B { get; }", b.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo{System.String}.B", b.Overridden);
                Assert.Equal(new[] { "public", "override", "get" }, b.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var c = output.Items[0].Items[1].Items[2];
                Assert.NotNull(c);
                Assert.Equal("C", c.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Bar.C", c.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.C", c.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.C", c.Name);
                Assert.Equal(@"public override sealed int C { set; }", c.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo{System.String}.C", c.Overridden);
                Assert.Equal(new[] { "public", "override", "sealed", "set" }, c.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var a = output.Items[0].Items[2].Items[0];
                Assert.NotNull(a);
                Assert.Equal("A", a.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("IFooBar.A", a.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.A", a.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.A", a.Name);
                Assert.Equal(@"int A { get; set; }", a.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "get", "set" }, a.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var b = output.Items[0].Items[2].Items[1];
                Assert.NotNull(b);
                Assert.Equal("B", b.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("IFooBar.B", b.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.B", b.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.B", b.Name);
                Assert.Equal(@"int B { get; }", b.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "get" }, b.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var c = output.Items[0].Items[2].Items[2];
                Assert.NotNull(c);
                Assert.Equal("C", c.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("IFooBar.C", c.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.C", c.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.C", c.Name);
                Assert.Equal(@"int C { set; }", c.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "set" }, c.Modifiers[SyntaxLanguage.CSharp]);
            }
        }

        [Trait("Related", "Generic")]
        [Fact]
        public void TestGenerateMetadataWithIndexer()
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
                Assert.Equal("Foo<T>.Item[Int32]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Item[System.Int32]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Item(System.Int32)", indexer.Name);
                Assert.Equal(@"public int this[int x] { get; set; }", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "get", "set" }, indexer.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var indexer = output.Items[0].Items[0].Items[1];
                Assert.NotNull(indexer);
                Assert.Equal("Item[String]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.Item[String]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Item[System.String]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Item(System.String)", indexer.Name);
                Assert.Equal(@"public virtual int this[string x] { get; }", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "virtual", "get" }, indexer.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var indexer = output.Items[0].Items[0].Items[2];
                Assert.NotNull(indexer);
                Assert.Equal("Item[Object]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.Item[Object]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Item[System.Object]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Item(System.Object)", indexer.Name);
                Assert.Equal(@"public abstract int this[object x] { set; }", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "abstract", "set" }, indexer.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var indexer = output.Items[0].Items[0].Items[3];
                Assert.NotNull(indexer);
                Assert.Equal("Item[DateTime]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.Item[DateTime]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Item[System.DateTime]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Item(System.DateTime)", indexer.Name);
                Assert.Equal(@"protected int this[DateTime x] { get; }", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "protected", "get" }, indexer.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var indexer = output.Items[0].Items[0].Items[4];
                Assert.NotNull(indexer);
                Assert.Equal("Item[T]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.Item[T]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Item[T]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Item(`0)", indexer.Name);
                Assert.Equal(@"public int this[T t] { get; protected set; }", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "get", "protected set" }, indexer.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var indexer = output.Items[0].Items[0].Items[5];
                Assert.NotNull(indexer);
                Assert.Equal("Item[Int32, T]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo<T>.Item[Int32, T]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo<T>.Item[System.Int32, T]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo`1.Item(System.Int32,`0)", indexer.Name);
                Assert.Equal(@"protected int this[int x, T t] { get; set; }", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "protected", "get", "set" }, indexer.Modifiers[SyntaxLanguage.CSharp]);
            }
            // Bar
            {
                var indexer = output.Items[0].Items[1].Items[0];
                Assert.NotNull(indexer);
                Assert.Equal("Item[Int32]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Bar.Item[Int32]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Item[System.Int32]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Item(System.Int32)", indexer.Name);
                Assert.Equal(@"public virtual int this[int x] { get; set; }", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "public", "virtual", "get", "set" }, indexer.Modifiers[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.Item(System.Int32)", indexer.Implements[0]);
            }
            {
                var indexer = output.Items[0].Items[1].Items[1];
                Assert.NotNull(indexer);
                Assert.Equal("Item[String]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Bar.Item[String]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Item[System.String]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Item(System.String)", indexer.Name);
                Assert.Equal(@"public override int this[string x] { get; }", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo{System.String}.Item(System.String)", indexer.Overridden);
                Assert.Equal(new[] { "public", "override", "get" }, indexer.Modifiers[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.Item(System.String)", indexer.Implements[0]);
            }
            {
                var indexer = output.Items[0].Items[1].Items[2];
                Assert.NotNull(indexer);
                Assert.Equal("Item[Object]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Bar.Item[Object]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Item[System.Object]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Bar.Item(System.Object)", indexer.Name);
                Assert.Equal(@"public override sealed int this[object x] { set; }", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo{System.String}.Item(System.Object)", indexer.Overridden);
                Assert.Equal(new[] { "public", "override", "sealed", "set" }, indexer.Modifiers[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.Item(System.Object)", indexer.Implements[0]);
            }
            // IFooBar
            {
                var indexer = output.Items[0].Items[2].Items[0];
                Assert.NotNull(indexer);
                Assert.Equal("Item[Int32]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("IFooBar.Item[Int32]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.Item[System.Int32]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.Item(System.Int32)", indexer.Name);
                Assert.Equal(@"int this[int x] { get; set; }", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "get", "set" }, indexer.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var indexer = output.Items[0].Items[2].Items[1];
                Assert.NotNull(indexer);
                Assert.Equal("Item[String]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("IFooBar.Item[String]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.Item[System.String]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.Item(System.String)", indexer.Name);
                Assert.Equal(@"int this[string x] { get; }", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "get" }, indexer.Modifiers[SyntaxLanguage.CSharp]);
            }
            {
                var indexer = output.Items[0].Items[2].Items[2];
                Assert.NotNull(indexer);
                Assert.Equal("Item[Object]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("IFooBar.Item[Object]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.Item[System.Object]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.IFooBar.Item(System.Object)", indexer.Name);
                Assert.Equal(@"int this[object x] { set; }", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "set" }, indexer.Modifiers[SyntaxLanguage.CSharp]);
            }
        }

        [Fact]
        public void TestGenerateMetadataWithMethodUsingDefaultValue()
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
        public void TestGenerateMetadataAsyncWithAssemblyInfoAndCrossReference()
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
            var referencedAssembly = CreateAssemblyFromCSharpCode(referenceCode, "reference");
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
        public void TestGenerateMetadataAsyncWithMultilanguage()
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
            Assert.Equal("Foo<T>", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.Equal("Foo(Of T)", type.DisplayNamesWithType[SyntaxLanguage.VB]);
            Assert.Equal("Test1.Foo<T>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.Equal("Test1.Foo(Of T)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.Equal("Test1.Foo`1", type.Name);

            {
                var method = output.Items[0].Items[0].Items[0];
                Assert.NotNull(method);
                Assert.Equal("Bar<K>(Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Bar(Of K)(Int32)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Foo<T>.Bar<K>(Int32)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo(Of T).Bar(Of K)(Int32)", method.DisplayNamesWithType[SyntaxLanguage.VB]);
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
                Assert.Equal("Foo<T>.Item[Int32]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo(Of T).Item(Int32)", indexer.DisplayNamesWithType[SyntaxLanguage.VB]);
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
        public void TestGenerateMetadataAsyncWithGenericInheritance()
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
                Assert.Equal("Foo<T>", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo(Of T)", type.DisplayNamesWithType[SyntaxLanguage.VB]);
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
                Assert.Equal("Foo<T1, T2, T3>", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo(Of T1, T2, T3)", type.DisplayNamesWithType[SyntaxLanguage.VB]);
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
        public void TestGenerateMetadataWithDynamic()
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
                Assert.Equal("Foo.F", field.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.F", field.DisplayNamesWithType[SyntaxLanguage.VB]);
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
                Assert.Equal("Foo.M(Object)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.M(Object)", method.DisplayNamesWithType[SyntaxLanguage.VB]);
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
                Assert.Equal("Foo.P", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.P", method.DisplayNamesWithType[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.P", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.P", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.P", method.Name);
                Assert.Equal(@"public dynamic P { get; protected set; }", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(@"Public Property P As Object", method.Syntax.Content[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[3];
                Assert.NotNull(method);
                Assert.Equal("Item[Object]", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Item(Object)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Foo.Item[Object]", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo.Item(Object)", method.DisplayNamesWithType[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.Item[System.Object]", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo.Item(System.Object)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.Item(System.Object)", method.Name);
                Assert.Equal(@"public dynamic this[dynamic index] { get; }", method.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(@"Public ReadOnly Property Item(index As Object) As Object", method.Syntax.Content[SyntaxLanguage.VB]);
            }
        }

        [Fact]
        [Trait("Related", "Multilanguage")]
        public void TestGenerateMetadataWithStaticClass()
        {
            string code = @"
using System.Collections.Generic;
namespace Test1
{
    public static class Foo
    {
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            {
                var type = output.Items[0].Items[0];
                Assert.NotNull(type);
                Assert.Equal("Foo", type.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo", type.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo", type.Name);
                Assert.Equal(1, type.Inheritance.Count);
                Assert.Equal("System.Object", type.Inheritance[0]);

                Assert.Equal(@"public static class Foo", type.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(@"Public Module Foo", type.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "public", "static", "class" }, type.Modifiers[SyntaxLanguage.CSharp]);
                Assert.Equal(new[] { "Public", "Module" }, type.Modifiers[SyntaxLanguage.VB]);
            }
        }

        [Fact]
        [Trait("Related", "Generic")]
        public void TestGenerateMetadataAsyncWithNestedGeneric()
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
                Assert.Equal("Foo<T1, T2>", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo(Of T1, T2)", type.DisplayNamesWithType[SyntaxLanguage.VB]);
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
                Assert.Equal("Foo<T1, T2>.Bar<T3>", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo(Of T1, T2).Bar(Of T3)", type.DisplayNamesWithType[SyntaxLanguage.VB]);
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
                Assert.Equal("Foo<T1, T2>.FooBar", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("Foo(Of T1, T2).FooBar", type.DisplayNamesWithType[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo<T1, T2>.FooBar", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
                Assert.Equal("Test1.Foo(Of T1, T2).FooBar", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`2.FooBar", type.Name);
                Assert.Equal(1, type.Inheritance.Count);
                Assert.Equal("System.Object", type.Inheritance[0]);
            }
        }

        [Fact]
        [Trait("Related", "Attribute")]
        public void TestGenerateMetadataAsyncWithAttributes()
        {
            string code = @"
using System;
using System.ComponentModel;

namespace Test1
{
    [Serializable]
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = true)]
    [TypeConverter(typeof(TestAttribute))]
    [TypeConverter(typeof(TestAttribute[]))]
    [Test(""test"")]
    [Test(new int[]{1,2,3})]
    [Test(new object[]{null, ""abc"", 'd', 1.1f, 1.2, (sbyte)2, (byte)3, (short)4, (ushort)5, 6, 7u, 8l, 9ul, new int[]{ 10, 11, 12 }})]
    [Test(new Type[]{ typeof(Func<>), typeof(Func<,>), typeof(Func<string, string>) })]
    public class TestAttribute : Attribute
    {
        [Test(1)]
        [Test(2)]
        public TestAttribute([Test(3), Test(4)] object obj){}
        [Test(5)]
        public object Property { [Test(6)] get; [Test(7), Test(8)] set; }
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code, MetadataReference.CreateFromFile(typeof(System.ComponentModel.TypeConverterAttribute).Assembly.Location)));
            var @class = output.Items[0].Items[0];
            Assert.NotNull(@class);
            Assert.Equal("TestAttribute", @class.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.Equal("TestAttribute", @class.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.Equal("Test1.TestAttribute", @class.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.Equal(@"[Serializable]
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.Delegate | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter | AttributeTargets.All, Inherited = true, AllowMultiple = true)]
[TypeConverter(typeof(TestAttribute))]
[TypeConverter(typeof(TestAttribute[]))]
[Test(""test"")]
[Test(new int[]{1, 2, 3})]
[Test(new object[]{null, ""abc"", 'd', 1.1F, 1.2, (sbyte)2, (byte)3, (short)4, (ushort)5, 6, 7U, 8L, 9UL, new int[]{10, 11, 12}})]
[Test(new Type[]{typeof(Func<>), typeof(Func<, >), typeof(Func<string, string>)})]
public class TestAttribute : Attribute, _Attribute", @class.Syntax.Content[SyntaxLanguage.CSharp]);

            Assert.NotNull(@class.Attributes);
            Assert.Equal(5, @class.Attributes.Count);

            Assert.Equal("System.SerializableAttribute", @class.Attributes[0].Type);
            Assert.Equal("System.SerializableAttribute.#ctor", @class.Attributes[0].Constructor);
            Assert.NotNull(@class.Attributes[0].Arguments);
            Assert.Equal(0, @class.Attributes[0].Arguments.Count);
            Assert.Null(@class.Attributes[0].NamedArguments);

            Assert.Equal("System.AttributeUsageAttribute", @class.Attributes[1].Type);
            Assert.Equal("System.AttributeUsageAttribute.#ctor(System.AttributeTargets)", @class.Attributes[1].Constructor);
            Assert.NotNull(@class.Attributes[1].Arguments);
            Assert.Equal(1, @class.Attributes[1].Arguments.Count);
            Assert.Equal("System.AttributeTargets", @class.Attributes[1].Arguments[0].Type);
            Assert.Equal(32767, @class.Attributes[1].Arguments[0].Value);
            Assert.NotNull(@class.Attributes[1].NamedArguments);
            Assert.Equal(2, @class.Attributes[1].NamedArguments.Count);
            Assert.Equal("Inherited", @class.Attributes[1].NamedArguments[0].Name);
            Assert.Equal("System.Boolean", @class.Attributes[1].NamedArguments[0].Type);
            Assert.Equal(true, @class.Attributes[1].NamedArguments[0].Value);
            Assert.Equal("AllowMultiple", @class.Attributes[1].NamedArguments[1].Name);
            Assert.Equal("System.Boolean", @class.Attributes[1].NamedArguments[1].Type);
            Assert.Equal(true, @class.Attributes[1].NamedArguments[1].Value);

            Assert.Equal("System.ComponentModel.TypeConverterAttribute", @class.Attributes[2].Type);
            Assert.Equal("System.ComponentModel.TypeConverterAttribute.#ctor(System.Type)", @class.Attributes[2].Constructor);
            Assert.NotNull(@class.Attributes[2].Arguments);
            Assert.Equal(1, @class.Attributes[2].Arguments.Count);
            Assert.Equal("System.Type", @class.Attributes[2].Arguments[0].Type);
            Assert.Equal("Test1.TestAttribute", @class.Attributes[2].Arguments[0].Value);
            Assert.Null(@class.Attributes[2].NamedArguments);

            Assert.Equal("System.ComponentModel.TypeConverterAttribute", @class.Attributes[3].Type);
            Assert.Equal("System.ComponentModel.TypeConverterAttribute.#ctor(System.Type)", @class.Attributes[3].Constructor);
            Assert.NotNull(@class.Attributes[3].Arguments);
            Assert.Equal(1, @class.Attributes[3].Arguments.Count);
            Assert.Equal("System.Type", @class.Attributes[3].Arguments[0].Type);
            Assert.Equal("Test1.TestAttribute[]", @class.Attributes[3].Arguments[0].Value);
            Assert.Null(@class.Attributes[3].NamedArguments);

            Assert.Equal("Test1.TestAttribute", @class.Attributes[4].Type);
            Assert.Equal("Test1.TestAttribute.#ctor(System.Object)", @class.Attributes[4].Constructor);
            Assert.NotNull(@class.Attributes[4].Arguments);
            Assert.Equal(1, @class.Attributes[4].Arguments.Count);
            Assert.Equal("System.String", @class.Attributes[4].Arguments[0].Type);
            Assert.Equal("test", @class.Attributes[4].Arguments[0].Value);
            Assert.Null(@class.Attributes[4].NamedArguments);

            var ctor = @class.Items[0];
            Assert.NotNull(ctor);
            Assert.Equal(@"[Test(1)]
[Test(2)]
public TestAttribute([Test(3), Test(4)] object obj)", ctor.Syntax.Content[SyntaxLanguage.CSharp]);

            var property = @class.Items[1];
            Assert.NotNull(property);
            Assert.Equal(@"[Test(5)]
public object Property
{
    [Test(6)]
    get;
    [Test(7)]
    [Test(8)]
    set;
}", property.Syntax.Content[SyntaxLanguage.CSharp]);
        }

        [Fact]
        public void TestGenerateMetadataWithFieldHasDefaultValue()
        {
            string code = @"
namespace Test1
{
    public class Foo
    {
        public const ushort Test = 123;
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Equal(1, output.Items.Count);
            {
                var field = output.Items[0].Items[0].Items[0];
                Assert.NotNull(field);
                Assert.Equal(@"public const ushort Test = 123", field.Syntax.Content[SyntaxLanguage.CSharp]);
            }
        }

        [Fact]
        [Trait("Related", "Multilanguage")]
        public void TestGenerateMetadataWithFieldHasDefaultValue_SpecialCharacter()
        {
            string code = @"
namespace Test1
{
    public class Foo
    {
        public const char Test = '\uDBFF';
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Equal(1, output.Items.Count);
            {
                var field = output.Items[0].Items[0].Items[0];
                Assert.NotNull(field);
                Assert.Equal(@"public const char Test = '\uDBFF'", field.Syntax.Content[SyntaxLanguage.CSharp]);
                Assert.Equal(@"Public Const Test As Char = ""\uDBFF""c", field.Syntax.Content[SyntaxLanguage.VB]);
            }
        }

        [Fact]
        [Trait("Related", "ExtensionMethod")]
        [Trait("Related", "Multilanguage")]
        public void TestGenerateMetadataAsyncWithExtensionMethods()
        {
            string code = @"
namespace Test1
{
    public static class Class1
    {
        public static void Method1(this object obj) {}
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Equal(1, output.Items.Count);
            var ns = output.Items[0];
            Assert.NotNull(ns);
            var method = ns.Items[0].Items[0];
            Assert.NotNull(method);
            Assert.True(method.IsExtensionMethod);
            Assert.Equal(@"public static void Method1(this object obj)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.Equal(@"<ExtensionAttribute>
Public Shared Sub Method1(obj As Object)", method.Syntax.Content[SyntaxLanguage.VB]);
        }

        [Fact]
        [Trait("Related", "Generic")]
        public void TestGenerateMetadataAsyncWithInheritedFromGenericClass()
        {
            string code = @"
namespace Test1
{
    public interface I1<T>
    {
        public void M1(T obj) {}
    }
    public interface I2<T> : I1<string>, I1<T> {}
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Equal(1, output.Items.Count);
            var ns = output.Items[0];
            Assert.NotNull(ns);
            var i1 = ns.Items[0];
            Assert.NotNull(i1);
            Assert.Equal("Test1.I1`1", i1.Name);
            Assert.Equal(1, i1.Items.Count);
            Assert.Null(i1.InheritedMembers);
            var m1 = i1.Items[0];
            Assert.Equal("Test1.I1`1.M1(`0)", m1.Name);

            var i2 = ns.Items[1];
            Assert.NotNull(i2);
            Assert.Equal("Test1.I2`1", i2.Name);
            Assert.Equal(0, i2.Items.Count);
            Assert.Equal(2, i2.InheritedMembers.Count);
            Assert.Equal(new[] { "Test1.I1{System.String}.M1(System.String)", "Test1.I1{{T}}.M1({T})" }, i2.InheritedMembers);

            var r1 = output.References["Test1.I1{System.String}.M1(System.String)"];
            Assert.False(r1.IsDefinition);
            Assert.Equal("Test1.I1`1.M1(`0)", r1.Definition);

            var r2 = output.References["Test1.I1{{T}}.M1({T})"];
            Assert.False(r1.IsDefinition);
            Assert.Equal("Test1.I1`1.M1(`0)", r1.Definition);
        }

        [Fact]
        [Trait("Related", "Generic")]
        public void TestCSharpFeature_Default_7_1Class()
        {
            string code = @"
namespace Test1
{
    public class Foo
    {
        public int Bar(int x = default) => 1;
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Equal(1, output.Items.Count);
            var ns = output.Items[0];
            Assert.NotNull(ns);
            var foo = ns.Items[0];
            Assert.NotNull(foo);
            Assert.Equal("Test1.Foo", foo.Name);
            Assert.Equal(1, foo.Items.Count);
            var bar = foo.Items[0];
            Assert.Equal("Test1.Foo.Bar(System.Int32)", bar.Name);
            Assert.Equal("public int Bar(int x = 0)", bar.Syntax.Content[SyntaxLanguage.CSharp]);
        }

        [Fact]
        [Trait("Related", "ValueTuple")]
        public void TestGenerateMetadataAsyncWithTupleParameter()
        {
            string code = @"
namespace Test1
{
    public class Foo
    {
        public int Bar((string prefix, string uri) @namespace) => 1;
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Single(output.Items);
            var ns = output.Items[0];
            Assert.NotNull(ns);
            var foo = ns.Items[0];
            Assert.NotNull(foo);
            Assert.Equal("Test1.Foo", foo.Name);
            Assert.Single(foo.Items);
            var bar = foo.Items[0];
            Assert.Equal("Test1.Foo.Bar(System.ValueTuple{System.String,System.String})", bar.Name);
            Assert.Equal("public int Bar((string prefix, string uri) namespace)", bar.Syntax.Content[SyntaxLanguage.CSharp]);
        }

        [Fact]
        [Trait("Related", "ValueTuple")]
        public void TestGenerateMetadataAsyncWithUnnamedTupleParameter()
        {
            string code = @"
namespace Test1
{
    public class Foo
    {
        public int Bar((string, string) @namespace) => 1;
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Single(output.Items);
            var ns = output.Items[0];
            Assert.NotNull(ns);
            var foo = ns.Items[0];
            Assert.NotNull(foo);
            Assert.Equal("Test1.Foo", foo.Name);
            Assert.Single(foo.Items);
            var bar = foo.Items[0];
            Assert.Equal("Test1.Foo.Bar(System.ValueTuple{System.String,System.String})", bar.Name);
            Assert.Equal("public int Bar((string, string) namespace)", bar.Syntax.Content[SyntaxLanguage.CSharp]);
        }

        [Fact]
        [Trait("Related", "ValueTuple")]
        public void TestGenerateMetadataAsyncWithPartiallyUnnamedTupleParameter()
        {
            string code = @"
namespace Test1
{
    public class Foo
    {
        public int Bar((string, string uri) @namespace) => 1;
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Single(output.Items);
            var ns = output.Items[0];
            Assert.NotNull(ns);
            var foo = ns.Items[0];
            Assert.NotNull(foo);
            Assert.Equal("Test1.Foo", foo.Name);
            Assert.Single(foo.Items);
            var bar = foo.Items[0];
            Assert.Equal("Test1.Foo.Bar(System.ValueTuple{System.String,System.String})", bar.Name);
            Assert.Equal("public int Bar((string, string uri) namespace)", bar.Syntax.Content[SyntaxLanguage.CSharp]);
        }

        [Fact]
        [Trait("Related", "ValueTuple")]
        public void TestGenerateMetadataAsyncWithTupleArrayParameter()
        {
            string code = @"
namespace Test1
{
    public class Foo
    {
        public int Bar((string prefix, string uri)[] namespaces) => 1;
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Single(output.Items);
            var ns = output.Items[0];
            Assert.NotNull(ns);
            var foo = ns.Items[0];
            Assert.NotNull(foo);
            Assert.Equal("Test1.Foo", foo.Name);
            Assert.Single(foo.Items);
            var bar = foo.Items[0];
            Assert.Equal("Test1.Foo.Bar(System.ValueTuple{System.String,System.String}[])", bar.Name);
            Assert.Equal("public int Bar((string prefix, string uri)[] namespaces)", bar.Syntax.Content[SyntaxLanguage.CSharp]);
        }

        [Fact]
        [Trait("Related", "ValueTuple")]
        public void TestGenerateMetadataAsyncWithTupleEnumerableParameter()
        {
            string code = @"
using System.Collections.Generic;

namespace Test1
{
    public class Foo
    {
        public int Bar(IEnumerable<(string prefix, string uri)> namespaces) => 1;
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Single(output.Items);
            var ns = output.Items[0];
            Assert.NotNull(ns);
            var foo = ns.Items[0];
            Assert.NotNull(foo);
            Assert.Equal("Test1.Foo", foo.Name);
            Assert.Single(foo.Items);
            var bar = foo.Items[0];
            Assert.Equal("Test1.Foo.Bar(System.Collections.Generic.IEnumerable{System.ValueTuple{System.String,System.String}})", bar.Name);
            Assert.Equal("public int Bar(IEnumerable<(string prefix, string uri)> namespaces)", bar.Syntax.Content[SyntaxLanguage.CSharp]);
        }

        [Fact]
        [Trait("Related", "ValueTuple")]
        public void TestGenerateMetadataAsyncWithTupleResult()
        {
            string code = @"
namespace Test1
{
    public class Foo
    {
        public (string prefix, string uri) Bar() => (string.Empty, string.Empty);
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Single(output.Items);
            var ns = output.Items[0];
            Assert.NotNull(ns);
            var foo = ns.Items[0];
            Assert.NotNull(foo);
            Assert.Equal("Test1.Foo", foo.Name);
            Assert.Single(foo.Items);
            var bar = foo.Items[0];
            Assert.Equal("Test1.Foo.Bar", bar.Name);
            Assert.Equal("public (string prefix, string uri) Bar()", bar.Syntax.Content[SyntaxLanguage.CSharp]);
        }

        [Fact]
        [Trait("Related", "ValueTuple")]
        public void TestGenerateMetadataAsyncWithUnnamedTupleResult()
        {
            string code = @"
namespace Test1
{
    public class Foo
    {
        public (string, string) Bar() => (string.Empty, string.Empty);
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Single(output.Items);
            var ns = output.Items[0];
            Assert.NotNull(ns);
            var foo = ns.Items[0];
            Assert.NotNull(foo);
            Assert.Equal("Test1.Foo", foo.Name);
            Assert.Single(foo.Items);
            var bar = foo.Items[0];
            Assert.Equal("Test1.Foo.Bar", bar.Name);
            Assert.Equal("public (string, string) Bar()", bar.Syntax.Content[SyntaxLanguage.CSharp]);
        }

        [Fact]
        [Trait("Related", "ValueTuple")]
        public void TestGenerateMetadataAsyncWithPartiallyUnnamedTupleResult()
        {
            string code = @"
namespace Test1
{
    public class Foo
    {
        public (string, string uri) Bar() => (string.Empty, string.Empty);
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Single(output.Items);
            var ns = output.Items[0];
            Assert.NotNull(ns);
            var foo = ns.Items[0];
            Assert.NotNull(foo);
            Assert.Equal("Test1.Foo", foo.Name);
            Assert.Single(foo.Items);
            var bar = foo.Items[0];
            Assert.Equal("Test1.Foo.Bar", bar.Name);
            Assert.Equal("public (string, string uri) Bar()", bar.Syntax.Content[SyntaxLanguage.CSharp]);
        }

        [Fact]
        [Trait("Related", "ValueTuple")]
        public void TestGenerateMetadataAsyncWithEnumerableTupleResult()
        {
            string code = @"
using System.Collections.Generic;

namespace Test1
{
    public class Foo
    {
        public IEnumerable<(string prefix, string uri)> Bar() => new (string.Empty, string.Empty)[0];
    }
}
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromCSharpCode(code));
            Assert.Single(output.Items);
            var ns = output.Items[0];
            Assert.NotNull(ns);
            var foo = ns.Items[0];
            Assert.NotNull(foo);
            Assert.Equal("Test1.Foo", foo.Name);
            Assert.Single(foo.Items);
            var bar = foo.Items[0];
            Assert.Equal("Test1.Foo.Bar", bar.Name);
            Assert.Equal("public IEnumerable<(string prefix, string uri)> Bar()", bar.Syntax.Content[SyntaxLanguage.CSharp]);
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
