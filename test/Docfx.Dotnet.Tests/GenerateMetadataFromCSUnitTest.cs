// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Docfx.DataContracts.ManagedReference;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

namespace Docfx.Dotnet.Tests;

[DoNotParallelize]
[TestClass]
public class GenerateMetadataFromCSUnitTest
{
    private static readonly Dictionary<string, string> EmptyMSBuildProperties = [];

    private static MetadataItem Verify(string code, ExtractMetadataConfig config = null, IDictionary<string, string> msbuildProperties = null, MetadataReference[] references = null)
    {
        var compilation = CompilationHelper.CreateCompilationFromCSharpCode(code, msbuildProperties ?? EmptyMSBuildProperties, "test.dll", references);
        var extensionMethods = compilation.Assembly.FindExtensionMethods(new(new(), new())).ToArray();
        return compilation.Assembly.GenerateMetadataItem(compilation, config, extensionMethods: extensionMethods);
    }

    [TestMethod]
    [TestProperty("Related", "Attribute")]
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
        MetadataItem output = Verify(code);
        var @class = output.Items[0].Items[0];
        Assert.IsNotNull(@class);
        Assert.AreEqual("Class1", @class.DisplayNames.First().Value);
        Assert.AreEqual("Class1", @class.DisplayNamesWithType.First().Value);
        Assert.AreEqual("Test1.Class1", @class.DisplayQualifiedNames.First().Value);
        Assert.AreEqual("This is a test", @class.Summary);
        Assert.AreEqual("Test1.Class1.Func1(System.Int32)", @class.SeeAlsos[0].LinkId);
        Assert.AreEqual(@"[Serializable]
public class Class1", @class.Syntax.Content[SyntaxLanguage.CSharp]);

        var function = output.Items[0].Items[0].Items[0];
        Assert.IsNotNull(function);
        Assert.AreEqual("Func1(int)", function.DisplayNames.First().Value);
        Assert.AreEqual("Class1.Func1(int)", function.DisplayNamesWithType.First().Value);
        Assert.AreEqual("Test1.Class1.Func1(int)", function.DisplayQualifiedNames.First().Value);
        Assert.AreEqual("Test1.Class1.Func1(System.Int32)", function.Name);
        Assert.AreEqual("This is a function", function.Summary);
        Assert.AreEqual("System.Int32", function.SeeAlsos[0].LinkId);
        Assert.AreEqual("This is a param as <xref href=\"System.Int32\" data-throw-if-not-resolved=\"false\"></xref>", function.Syntax.Parameters[0].Description);
        Assert.ContainsSingle(output.Items);
        var parameter = function.Syntax.Parameters[0];
        Assert.AreEqual("i", parameter.Name);
        Assert.AreEqual("System.Int32", parameter.Type);
        var returnValue = function.Syntax.Return;
        Assert.IsNull(returnValue);
    }

    [TestMethod]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        var ns = output.Items[0];
        Assert.IsNotNull(ns);
        Assert.AreEqual("Test1.Test2", ns.DisplayNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Test1.Test2", ns.DisplayNamesWithType[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Test1.Test2", ns.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
    }

    [TestProperty("Related", "Generic")]
    [TestProperty("Related", "Reference")]
    [TestMethod]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var type = output.Items[0].Items[0];
            Assert.IsNotNull(type);
            Assert.AreEqual("Class1<T>", type.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Class1<T>", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Class1<T>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Class1`1", type.Name);
            Assert.AreEqual("public sealed class Class1<T> where T : struct, IEnumerable<T>", type.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.IsNotNull(type.Syntax.TypeParameters);
            Assert.ContainsSingle(type.Syntax.TypeParameters);
            Assert.AreEqual("T", type.Syntax.TypeParameters[0].Name);
            Assert.IsNull(type.Syntax.TypeParameters[0].Type);
            Assert.AreEqual("The type", type.Syntax.TypeParameters[0].Description);
        }
        {
            var function = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(function);
            Assert.AreEqual("Func1<TResult>(T?, IEnumerable<T>)", function.DisplayNames.First().Value);
            Assert.AreEqual("Class1<T>.Func1<TResult>(T?, IEnumerable<T>)", function.DisplayNamesWithType.First().Value);
            Assert.AreEqual("Test1.Class1<T>.Func1<TResult>(T?, System.Collections.Generic.IEnumerable<T>)", function.DisplayQualifiedNames.First().Value);
            Assert.AreEqual("Test1.Class1`1.Func1``1(System.Nullable{`0},System.Collections.Generic.IEnumerable{`0})", function.Name);

            var parameterX = function.Syntax.Parameters[0];
            Assert.AreEqual("x", parameterX.Name);
            Assert.AreEqual("System.Nullable{{T}}", parameterX.Type);

            var parameterY = function.Syntax.Parameters[1];
            Assert.AreEqual("y", parameterY.Name);
            Assert.AreEqual("System.Collections.Generic.IEnumerable{{T}}", parameterY.Type);

            var returnValue = function.Syntax.Return;
            Assert.IsNotNull(returnValue);
            Assert.IsNotNull(returnValue.Type);
            Assert.AreEqual("System.Nullable{{TResult}}", returnValue.Type);
            Assert.AreEqual("public TResult? Func1<TResult>(T? x, IEnumerable<T> y) where TResult : struct", function.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var property = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(property);
            Assert.AreEqual("Items", property.DisplayNames.First().Value);
            Assert.AreEqual("Class1<T>.Items", property.DisplayNamesWithType.First().Value);
            Assert.AreEqual("Test1.Class1<T>.Items", property.DisplayQualifiedNames.First().Value);
            Assert.AreEqual("Test1.Class1`1.Items", property.Name);
            Assert.IsEmpty(property.Syntax.Parameters);
            var returnValue = property.Syntax.Return;
            Assert.IsNotNull(returnValue.Type);
            Assert.AreEqual("System.Collections.Generic.IEnumerable{{T}}", returnValue.Type);
            Assert.AreEqual("public IEnumerable<T> Items { get; set; }", property.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var event1 = output.Items[0].Items[0].Items[2];
            Assert.IsNotNull(event1);
            Assert.AreEqual("Event1", event1.DisplayNames.First().Value);
            Assert.AreEqual("Class1<T>.Event1", event1.DisplayNamesWithType.First().Value);
            Assert.AreEqual("Test1.Class1<T>.Event1", event1.DisplayQualifiedNames.First().Value);
            Assert.AreEqual("Test1.Class1`1.Event1", event1.Name);
            Assert.IsNull(event1.Syntax.Parameters);
            Assert.AreEqual("EventHandler", event1.Syntax.Return.Type);
            Assert.AreEqual("public event EventHandler Event1", event1.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var operator1 = output.Items[0].Items[0].Items[3];
            Assert.IsNotNull(operator1);
            Assert.AreEqual("operator ==(Class1<T>, Class1<T>)", operator1.DisplayNames.First().Value);
            Assert.AreEqual("Class1<T>.operator ==(Class1<T>, Class1<T>)", operator1.DisplayNamesWithType.First().Value);
            Assert.AreEqual("Test1.Class1<T>.operator ==(Test1.Class1<T>, Test1.Class1<T>)", operator1.DisplayQualifiedNames.First().Value);
            Assert.AreEqual("Test1.Class1`1.op_Equality(Test1.Class1{`0},Test1.Class1{`0})", operator1.Name);
            Assert.IsNotNull(operator1.Syntax.Parameters);

            var parameterX = operator1.Syntax.Parameters[0];
            Assert.AreEqual("x", parameterX.Name);
            Assert.AreEqual("Test1.Class1`1", parameterX.Type);

            var parameterY = operator1.Syntax.Parameters[1];
            Assert.AreEqual("y", parameterY.Name);
            Assert.AreEqual("Test1.Class1`1", parameterY.Type);

            Assert.IsNotNull(operator1.Syntax.Return);
            Assert.AreEqual("System.Boolean", operator1.Syntax.Return.Type);

            Assert.AreEqual("public static bool operator ==(Class1<T> x, Class1<T> y)", operator1.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var property = output.Items[0].Items[0].Items[4];
            Assert.IsNotNull(property);
            Assert.AreEqual("Items2", property.DisplayNames.First().Value);
            Assert.AreEqual("Class1<T>.Items2", property.DisplayNamesWithType.First().Value);
            Assert.AreEqual("Test1.Class1<T>.Items2", property.DisplayQualifiedNames.First().Value);
            Assert.AreEqual("Test1.Class1`1.Items2", property.Name);
            Assert.IsEmpty(property.Syntax.Parameters);
            var returnValue = property.Syntax.Return;
            Assert.IsNotNull(returnValue.Type);
            Assert.AreEqual("System.Collections.Generic.IEnumerable{{T}}", returnValue.Type);
            Assert.AreEqual("public IEnumerable<T> Items2 { get; }", property.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        // check references
        {
            Assert.IsNotNull(output.References);
            Assert.IsTrue(output.References.Count > 0);

            Assert.IsTrue(output.References.ContainsKey("Test1.Class1`1"));
            var reference = output.References["Test1.Class1`1"];
            Assert.IsTrue(reference.IsDefinition);
            Assert.AreEqual("Test1", reference.Parent);
            Assert.IsTrue(output.References.ContainsKey("Test1"));
            reference = output.References["Test1"];
            Assert.IsTrue(reference.IsDefinition);
            Assert.IsNull(reference.Parent);

            Assert.IsTrue(output.References.ContainsKey("System.Collections.Generic.Dictionary`2"));
            Assert.IsNotNull(output.References["System.Collections.Generic.Dictionary`2"]);
            Assert.IsTrue(output.Items[0].Items[0].References.ContainsKey("System.Collections.Generic.Dictionary`2"));
            Assert.IsNull(output.Items[0].Items[0].References["System.Collections.Generic.Dictionary`2"]);
        }
    }

    [TestMethod]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var method = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(method);
            Assert.AreEqual("Bar(int)", method.DisplayNames.First().Value);
            Assert.AreEqual("IFoo.Bar(int)", method.DisplayNamesWithType.First().Value);
            Assert.AreEqual("Test1.IFoo.Bar(int)", method.DisplayQualifiedNames.First().Value);
            Assert.AreEqual("Test1.IFoo.Bar(System.Int32)", method.Name);
            var parameter = method.Syntax.Parameters[0];
            Assert.AreEqual("x", parameter.Name);
            Assert.AreEqual("System.Int32", parameter.Type);
            var returnValue = method.Syntax.Return;
            Assert.IsNotNull(returnValue);
            Assert.IsNotNull(returnValue.Type);
            Assert.AreEqual("System.String", returnValue.Type);
        }
        {
            var property = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(property);
            Assert.AreEqual("Count", property.DisplayNames.First().Value);
            Assert.AreEqual("IFoo.Count", property.DisplayNamesWithType.First().Value);
            Assert.AreEqual("Test1.IFoo.Count", property.DisplayQualifiedNames.First().Value);
            Assert.AreEqual("Test1.IFoo.Count", property.Name);
            Assert.IsEmpty(property.Syntax.Parameters);
            var returnValue = property.Syntax.Return;
            Assert.IsNotNull(returnValue);
            Assert.IsNotNull(returnValue.Type);
            Assert.AreEqual("System.Int32", returnValue.Type);
        }
        {
            var @event = output.Items[0].Items[0].Items[2];
            Assert.IsNotNull(@event);
            Assert.AreEqual("FooBar", @event.DisplayNames.First().Value);
            Assert.AreEqual("IFoo.FooBar", @event.DisplayNamesWithType.First().Value);
            Assert.AreEqual("Test1.IFoo.FooBar", @event.DisplayQualifiedNames.First().Value);
            Assert.AreEqual("Test1.IFoo.FooBar", @event.Name);
            Assert.AreEqual("event EventHandler FooBar", @event.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.IsNull(@event.Syntax.Parameters);
            Assert.AreEqual("EventHandler", @event.Syntax.Return.Type);
        }
    }

    [TestMethod]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);

        var iFoo = output.Items[0].Items[0];
        Assert.IsNotNull(iFoo);
        Assert.AreEqual("IFoo", iFoo.DisplayNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("IFoo", iFoo.DisplayNamesWithType[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Test1.IFoo", iFoo.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("public interface IFoo", iFoo.Syntax.Content[SyntaxLanguage.CSharp]);

        var iBar = output.Items[0].Items[1];
        Assert.IsNotNull(iBar);
        Assert.AreEqual("IBar", iBar.DisplayNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("IBar", iBar.DisplayNamesWithType[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Test1.IBar", iBar.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("public interface IBar : IFoo", iBar.Syntax.Content[SyntaxLanguage.CSharp]);

        var iFooBar = output.Items[0].Items[2];
        Assert.IsNotNull(iFooBar);
        Assert.AreEqual("IFooBar", iFooBar.DisplayNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("IFooBar", iFooBar.DisplayNamesWithType[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Test1.IFooBar", iFooBar.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("public interface IFooBar : IBar, IFoo", iFooBar.Syntax.Content[SyntaxLanguage.CSharp]);
    }

    [TestMethod]
    public void TestGenerateMetadataWithInternalInterfaceAndInherits()
    {
        string code = @"
namespace Test1
{
    public class Foo : IFoo { }
    internal interface IFoo { }
}
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);

        var foo = output.Items[0].Items[0];
        Assert.IsNotNull(foo);
        Assert.AreEqual("Foo", foo.DisplayNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Foo", foo.DisplayNamesWithType[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Test1.Foo", foo.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("public class Foo", foo.Syntax.Content[SyntaxLanguage.CSharp]);
        Assert.IsNull(foo.Implements);
    }

    [TestMethod]
    public void TestGenerateMetadataWithProtectedInterfaceAndInherits()
    {
        string code = @"
namespace Test1
{
    public class Foo {
       protected interface IFoo { }
       public class SubFoo : IFoo { }
    }
}
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);

        var subFoo = output.Items[0].Items[2];
        Assert.IsNotNull(subFoo);
        Assert.AreEqual("Foo.SubFoo", subFoo.DisplayNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Foo.SubFoo", subFoo.DisplayNamesWithType[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Test1.Foo.SubFoo", subFoo.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("public class Foo.SubFoo : Foo.IFoo", subFoo.Syntax.Content[SyntaxLanguage.CSharp]);
        Assert.IsNotNull(subFoo.Implements);
        Assert.AreEqual("Test1.Foo.IFoo", subFoo.Implements[0]);
    }

    [TestMethod]
    public void TestGenerateMetadataWithPublicInterfaceNestedInternal()
    {
        string code = @"
namespace Test1
{
    internal class FooInternal
    {
        public interface IFoo { }
    }
    public class Foo : FooInternal.IFoo { }
}
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);

        var foo = output.Items[0].Items[0];
        Assert.IsNotNull(foo);
        Assert.AreEqual("Foo", foo.DisplayNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Foo", foo.DisplayNamesWithType[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Test1.Foo", foo.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("public class Foo", foo.Syntax.Content[SyntaxLanguage.CSharp]);
        Assert.IsNull(foo.Implements);
    }

    [TestProperty("Related", "Generic")]
    [TestProperty("Related", "Inheritance")]
    [TestProperty("Related", "Reference")]
    [TestMethod]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);

        var foo = output.Items[0].Items[0];
        Assert.IsNotNull(foo);
        Assert.AreEqual("Foo<T>", foo.DisplayNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Foo<T>", foo.DisplayNamesWithType[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Test1.Foo<T>", foo.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("public class Foo<T> : IFoo", foo.Syntax.Content[SyntaxLanguage.CSharp]);
        Assert.IsNotNull(foo.Implements);
        Assert.ContainsSingle(foo.Implements);
        CollectionAssert.AreEqual(new[] { "Test1.IFoo" }, foo.Implements.ToArray());

        var bar = output.Items[0].Items[1];
        Assert.IsNotNull(bar);
        Assert.AreEqual("Bar<T>", bar.DisplayNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Bar<T>", bar.DisplayNamesWithType[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Test1.Bar<T>", bar.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("public class Bar<T> : Foo<T[]>, IFoo, IBar", bar.Syntax.Content[SyntaxLanguage.CSharp]);
        CollectionAssert.AreEqual(new[] { "System.Object", "Test1.Foo{{T}[]}" }, bar.Inheritance.ToArray());
        CollectionAssert.AreEqual(new[] { "Test1.IFoo", "Test1.IBar" }, bar.Implements.ToArray());

        var fooBar = output.Items[0].Items[2];
        Assert.IsNotNull(fooBar);
        Assert.AreEqual("FooBar", fooBar.DisplayNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("FooBar", fooBar.DisplayNamesWithType[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Test1.FooBar", fooBar.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("public class FooBar : Bar<string>, IFooBar, IFoo, IBar", fooBar.Syntax.Content[SyntaxLanguage.CSharp]);
        CollectionAssert.AreEqual(new[] { "System.Object", "Test1.Foo{System.String[]}", "Test1.Bar{System.String}" }, fooBar.Inheritance.ToArray());
        CollectionAssert.AreEqual(new[] { "Test1.IFoo", "Test1.IBar", "Test1.IFooBar" }.OrderBy(s => s).ToArray(), fooBar.Implements.OrderBy(s => s).ToArray());

        Assert.IsNotNull(output.References);
        Assert.AreEqual(19, output.References.Count);
        {
            var item = output.References["System.Object"];
            Assert.AreEqual("System", item.Parent);
            Assert.IsNotNull(item);
            Assert.ContainsSingle(item.NameParts[SyntaxLanguage.CSharp]);

            Assert.AreEqual("System.Object", item.NameParts[SyntaxLanguage.CSharp][0].Name);
            Assert.AreEqual("object", string.Concat(item.NameParts[SyntaxLanguage.CSharp].Select(p => p.DisplayName)));
            Assert.AreEqual("object", string.Concat(item.NameWithTypeParts[SyntaxLanguage.CSharp].Select(p => p.DisplayName)));
            Assert.AreEqual("object", string.Concat(item.QualifiedNameParts[SyntaxLanguage.CSharp].Select(p => p.DisplayName)));
        }
        {
            var item = output.References["Test1.Bar{System.String}"];
            Assert.IsNotNull(item);
            Assert.AreEqual("Test1.Bar`1", item.Definition);
            Assert.AreEqual("Test1", item.Parent);

            CollectionAssert.AreEqual(
                new[] { "Test1.Bar`1", null, "System.String", null },
                item.NameParts[SyntaxLanguage.CSharp].Select(p => p.Name).ToArray());
            CollectionAssert.AreEqual(
                new[] { "Bar", "<", "string", ">" },
                item.NameParts[SyntaxLanguage.CSharp].Select(p => p.DisplayName).ToArray());
            CollectionAssert.AreEqual(
                new[] { "Bar", "<", "string", ">" },
                item.NameWithTypeParts[SyntaxLanguage.CSharp].Select(p => p.DisplayName).ToArray());
            CollectionAssert.AreEqual(
                new[] { "Test1", ".", "Bar", "<", "string", ">" },
                item.QualifiedNameParts[SyntaxLanguage.CSharp].Select(p => p.DisplayName).ToArray());
        }
        {
            var item = output.References["Test1.Foo{{T}[]}"];
            Assert.IsNotNull(item);
            Assert.AreEqual("Test1.Foo`1", item.Definition);
            Assert.AreEqual("Test1", item.Parent);

            Assert.AreEqual(6, item.NameParts[SyntaxLanguage.CSharp].Count);

            CollectionAssert.AreEqual(
                new string[] { "Test1.Foo`1", null, null, null, null, null },
                item.NameParts[SyntaxLanguage.CSharp].Select(p => p.Name).ToArray());
            CollectionAssert.AreEqual(
                new string[] { "Foo", "<", "T", "[", "]", ">" },
                item.NameParts[SyntaxLanguage.CSharp].Select(p => p.DisplayName).ToArray());
            CollectionAssert.AreEqual(
                new string[] { "Foo", "<", "T", "[", "]", ">" },
                item.NameWithTypeParts[SyntaxLanguage.CSharp].Select(p => p.DisplayName).ToArray());
            CollectionAssert.AreEqual(
                new string[] { "Test1", ".", "Foo", "<", "T", "[", "]", ">" },
                item.QualifiedNameParts[SyntaxLanguage.CSharp].Select(p => p.DisplayName).ToArray());
        }
        {
            var item = output.References["Test1.Foo{System.String[]}"];
            Assert.IsNotNull(item);
            Assert.AreEqual("Test1.Foo`1", item.Definition);
            Assert.AreEqual("Test1", item.Parent);

            CollectionAssert.AreEqual(
                new[] { "Test1.Foo`1", null, "System.String", null, null, null },
                item.NameParts[SyntaxLanguage.CSharp].Select(p => p.Name).ToArray());
            CollectionAssert.AreEqual(
                new[] { "Foo", "<", "string", "[", "]", ">" },
                item.NameParts[SyntaxLanguage.CSharp].Select(p => p.DisplayName).ToArray());
            CollectionAssert.AreEqual(
                new[] { "Foo", "<", "string", "[", "]", ">" },
                item.NameWithTypeParts[SyntaxLanguage.CSharp].Select(p => p.DisplayName).ToArray());
            CollectionAssert.AreEqual(
                new[] { "Test1", ".", "Foo", "<", "string", "[", "]", ">" },
                item.QualifiedNameParts[SyntaxLanguage.CSharp].Select(p => p.DisplayName).ToArray());
        }
    }

    [TestMethod]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var type = output.Items[0].Items[0];
            Assert.IsNotNull(type);
            Assert.AreEqual("ABC", type.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("ABC", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.ABC", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.ABC", type.Name);
            Assert.AreEqual("public enum ABC", type.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var type = output.Items[0].Items[1];
            Assert.IsNotNull(type);
            Assert.AreEqual("YN", type.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("YN", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.YN", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.YN", type.Name);
            Assert.AreEqual("public enum YN : byte", type.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var type = output.Items[0].Items[2];
            Assert.IsNotNull(type);
            Assert.AreEqual("XYZ", type.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("XYZ", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.XYZ", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.XYZ", type.Name);
            Assert.AreEqual("public enum XYZ", type.Syntax.Content[SyntaxLanguage.CSharp]);
        }
    }

    [TestProperty("Related", "Inheritance")]
    [TestMethod]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var type = output.Items[0].Items[0];
            Assert.IsNotNull(type);
            Assert.AreEqual("Foo", type.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo", type.Name);
            Assert.AreEqual("public struct Foo", type.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.IsNull(type.Implements);
        }
        {
            var type = output.Items[0].Items[1];
            Assert.IsNotNull(type);
            Assert.AreEqual("Bar<T>", type.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Bar<T>", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar<T>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar`1", type.Name);
            Assert.AreEqual("public struct Bar<T> : IEnumerable<T>, IEnumerable", type.Syntax.Content[SyntaxLanguage.CSharp]);
            CollectionAssert.AreEqual(new[] { "System.Collections.Generic.IEnumerable{{T}}", "System.Collections.IEnumerable" }, type.Implements.ToArray());
        }
        // inheritance of Foo
        {
            var inheritedMembers = output.Items[0].Items[0].InheritedMembers;
            Assert.IsNotNull(inheritedMembers);
            CollectionAssert.AreEqual(
                new string[]
                {
                    "System.ValueType.ToString",
                    "System.ValueType.Equals(System.Object)",
                    "System.ValueType.GetHashCode",
                    "System.Object.Equals(System.Object,System.Object)",
                    "System.Object.ReferenceEquals(System.Object,System.Object)",
                    "System.Object.GetType",
                }.OrderBy(s => s).ToArray(),
                inheritedMembers.OrderBy(s => s).ToArray());
        }
    }

    [TestProperty("Related", "Generic")]
    [TestMethod]
    public void TestGenerateMetadataWithDelegate()
    {
        string code = @"
using System.Collections.Generic
namespace Test1
{
    public delegate void Foo();
    public delegate T Bar<T>(IEnumerable<T> x = null) where T : class;
    public delegate void FooBar(ref int x, out string y, in bool z, params byte[] w);
    public delegate ref int Ref();
    public delegate ref readonly int RefReadonly();
}
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var type = output.Items[0].Items[0];
            Assert.IsNotNull(type);
            Assert.AreEqual("Foo", type.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo", type.Name);
            Assert.AreEqual("public delegate void Foo()", type.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.IsNull(type.Syntax.Parameters);
            Assert.IsNull(type.Syntax.Return);
        }
        {
            var type = output.Items[0].Items[1];
            Assert.IsNotNull(type);
            Assert.AreEqual("Bar<T>", type.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Bar<T>", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar<T>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar`1", type.Name);
            Assert.AreEqual("public delegate T Bar<T>(IEnumerable<T> x = null) where T : class", type.Syntax.Content[SyntaxLanguage.CSharp]);

            Assert.IsNotNull(type.Syntax.Parameters);
            Assert.ContainsSingle(type.Syntax.Parameters);
            Assert.AreEqual("x", type.Syntax.Parameters[0].Name);
            Assert.AreEqual("System.Collections.Generic.IEnumerable{{T}}", type.Syntax.Parameters[0].Type);
            Assert.IsNotNull(type.Syntax.Return);
            Assert.AreEqual("{T}", type.Syntax.Return.Type);
        }
        {
            var type = output.Items[0].Items[2];
            Assert.IsNotNull(type);
            Assert.AreEqual("FooBar", type.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("FooBar", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.FooBar", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.FooBar", type.Name);
            Assert.AreEqual("public delegate void FooBar(ref int x, out string y, in bool z, params byte[] w)", type.Syntax.Content[SyntaxLanguage.CSharp]);

            Assert.IsNotNull(type.Syntax.Parameters);
            Assert.AreEqual(4, type.Syntax.Parameters.Count);
            Assert.AreEqual("x", type.Syntax.Parameters[0].Name);
            Assert.AreEqual("System.Int32", type.Syntax.Parameters[0].Type);
            Assert.AreEqual("y", type.Syntax.Parameters[1].Name);
            Assert.AreEqual("System.String", type.Syntax.Parameters[1].Type);
            Assert.AreEqual("z", type.Syntax.Parameters[2].Name);
            Assert.AreEqual("System.Boolean", type.Syntax.Parameters[2].Type);
            Assert.AreEqual("w", type.Syntax.Parameters[3].Name);
            Assert.AreEqual("System.Byte[]", type.Syntax.Parameters[3].Type);
            Assert.IsNull(type.Syntax.Return);
        }
        {
            var type = output.Items[0].Items[3];
            Assert.IsNotNull(type);
            Assert.AreEqual("Ref", type.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Ref", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Ref", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Ref", type.Name);
            Assert.AreEqual("public delegate ref int Ref()", type.Syntax.Content[SyntaxLanguage.CSharp]);

            Assert.IsNull(type.Syntax.Parameters);
            Assert.AreEqual("System.Int32", type.Syntax.Return.Type);
        }
        {
            var type = output.Items[0].Items[4];
            Assert.IsNotNull(type);
            Assert.AreEqual("RefReadonly", type.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("RefReadonly", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.RefReadonly", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.RefReadonly", type.Name);
            Assert.AreEqual("public delegate ref readonly int RefReadonly()", type.Syntax.Content[SyntaxLanguage.CSharp]);

            Assert.IsNull(type.Syntax.Parameters);
            Assert.AreEqual("System.Int32", type.Syntax.Return.Type);
        }
    }

    [TestProperty("Related", "Generic")]
    [TestMethod]
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
        public void M5(ref int x, out string y, in bool z){}
        public ref int M6(){}
        public ref readonly int M7(){}
    }
    public class Bar : Foo<string>, IFooBar
    {
        public override void M1(){}
        protected sealed override Foo<T> M2<TArg>(TArg arg) where TArg : Foo<string> => this;
        public int M8<TArg>(TArg arg) where TArg : struct, new() => 2;
    }
    public interface IFooBar
    {
        void M1();
        Foo<T> M2<TArg>(TArg arg) where TArg : Foo<string>;
        int M8<TArg>(TArg arg) where TArg : struct, new();
    }
}
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        // Foo<T>
        {
            var method = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(method);
            Assert.AreEqual("M1()", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.M1()", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.M1()", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.M1", method.Name);
            Assert.AreEqual("public abstract void M1()", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(method);
            Assert.AreEqual("M2<TArg>(TArg)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.M2<TArg>(TArg)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.M2<TArg>(TArg)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.M2``1(``0)", method.Name);
            Assert.AreEqual("protected virtual Foo<T> M2<TArg>(TArg arg) where TArg : Foo<T>", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[2];
            Assert.IsNotNull(method);
            Assert.AreEqual("M3<TResult>(string)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.M3<TResult>(string)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.M3<TResult>(string)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.M3``1(System.String)", method.Name);
            Assert.AreEqual("public static TResult M3<TResult>(string x) where TResult : class", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[3];
            Assert.IsNotNull(method);
            Assert.AreEqual("M4(int)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.M4(int)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.M4(int)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.M4(System.Int32)", method.Name);
            Assert.AreEqual("public void M4(int x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[4];
            Assert.IsNotNull(method);
            Assert.AreEqual("M5(ref int, out string, in bool)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.M5(ref int, out string, in bool)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.M5(ref int, out string, in bool)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.M5(System.Int32@,System.String@,System.Boolean@)", method.Name);
            Assert.AreEqual("public void M5(ref int x, out string y, in bool z)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[5];
            Assert.IsNotNull(method);
            Assert.AreEqual("M6()", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.M6()", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.M6()", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.M6", method.Name);
            Assert.AreEqual("public ref int M6()", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[6];
            Assert.IsNotNull(method);
            Assert.AreEqual("M7()", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.M7()", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.M7()", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.M7", method.Name);
            Assert.AreEqual("public ref readonly int M7()", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        // Bar
        {
            var method = output.Items[0].Items[1].Items[0];
            Assert.IsNotNull(method);
            Assert.AreEqual("M1()", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Bar.M1()", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.M1()", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.M1", method.Name);
            Assert.AreEqual("public override void M1()", method.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo{System.String}.M1", method.Overridden);
            Assert.AreEqual("Test1.IFooBar.M1", method.Implements[0]);
        }
        {
            var method = output.Items[0].Items[1].Items[1];
            Assert.IsNotNull(method);
            Assert.AreEqual("M2<TArg>(TArg)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Bar.M2<TArg>(TArg)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.M2<TArg>(TArg)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.M2``1(``0)", method.Name);
            Assert.AreEqual("protected override sealed Foo<T> M2<TArg>(TArg arg) where TArg : Foo<string>", method.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo{System.String}.M2``1({TArg})", method.Overridden);
        }
        {
            var method = output.Items[0].Items[1].Items[2];
            Assert.IsNotNull(method);
            Assert.AreEqual("M8<TArg>(TArg)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Bar.M8<TArg>(TArg)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.M8<TArg>(TArg)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.M8``1(``0)", method.Name);
            Assert.AreEqual("public int M8<TArg>(TArg arg) where TArg : struct, new()", method.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFooBar.M8``1({TArg})", method.Implements[0]);
        }
        // IFooBar
        {
            var method = output.Items[0].Items[2].Items[0];
            Assert.IsNotNull(method);
            Assert.AreEqual("M1()", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("IFooBar.M1()", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFooBar.M1()", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFooBar.M1", method.Name);
            Assert.AreEqual("void M1()", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[2].Items[1];
            Assert.IsNotNull(method);
            Assert.AreEqual("M2<TArg>(TArg)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("IFooBar.M2<TArg>(TArg)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFooBar.M2<TArg>(TArg)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFooBar.M2``1(``0)", method.Name);
            Assert.AreEqual("Foo<T> M2<TArg>(TArg arg) where TArg : Foo<string>", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[2].Items[2];
            Assert.IsNotNull(method);
            Assert.AreEqual("M8<TArg>(TArg)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("IFooBar.M8<TArg>(TArg)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFooBar.M8<TArg>(TArg)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFooBar.M8``1(``0)", method.Name);
            Assert.AreEqual("int M8<TArg>(TArg arg) where TArg : struct, new()", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
    }

    [TestProperty("Related", "Generic")]
    [TestProperty("Related", "EII")]
    [TestMethod]
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
        MetadataItem output = Verify(code, new() { IncludePrivateMembers = true });
        Assert.ContainsSingle(output.Items);
        {
            var type = output.Items[0].Items[0];
            Assert.IsNotNull(type);
            Assert.AreEqual("Foo<T>", type.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1", type.Name);
            Assert.AreEqual("public class Foo<T> : IFoo, IFoo<string>, IFoo<T> where T : class", type.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.Contains("Test1.IFoo", type.Implements);
            Assert.Contains("Test1.IFoo{System.String}", type.Implements);
            Assert.Contains("Test1.IFoo{{T}}", type.Implements);
        }
        {
            var method = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(method);
            Assert.IsTrue(method.IsExplicitInterfaceImplementation);
            Assert.AreEqual("IFoo.Bar(ref int)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.IFoo.Bar(ref int)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.Test1.IFoo.Bar(ref int)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.Test1#IFoo#Bar(System.Int32@)", method.Name);
            Assert.AreEqual("object IFoo.Bar(ref int x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFoo.Bar(System.Int32@)", method.Implements[0]);
        }
        {
            var method = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(method);
            Assert.IsTrue(method.IsExplicitInterfaceImplementation);
            Assert.AreEqual("IFoo<string>.Bar<TArg>(TArg[])", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.IFoo<string>.Bar<TArg>(TArg[])", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.Test1.IFoo<string>.Bar<TArg>(TArg[])", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.Test1#IFoo{System#String}#Bar``1(``0[])", method.Name);
            Assert.AreEqual("string IFoo<string>.Bar<TArg>(TArg[] x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFoo{System.String}.Bar``1({TArg}[])", method.Implements[0]);
        }
        {
            var method = output.Items[0].Items[0].Items[2];
            Assert.IsNotNull(method);
            Assert.IsTrue(method.IsExplicitInterfaceImplementation);
            Assert.AreEqual("IFoo<T>.Bar<TArg>(TArg[])", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.IFoo<T>.Bar<TArg>(TArg[])", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.Test1.IFoo<T>.Bar<TArg>(TArg[])", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.Test1#IFoo{T}#Bar``1(``0[])", method.Name);
            Assert.AreEqual("T IFoo<T>.Bar<TArg>(TArg[] x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFoo{{T}}.Bar``1({TArg}[])", method.Implements[0]);
        }
        {
            var p = output.Items[0].Items[0].Items[3];
            Assert.IsNotNull(p);
            Assert.IsTrue(p.IsExplicitInterfaceImplementation);
            Assert.AreEqual("IFoo<string>.P", p.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.IFoo<string>.P", p.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.Test1.IFoo<string>.P", p.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.Test1#IFoo{System#String}#P", p.Name);
            Assert.AreEqual("string IFoo<string>.P { get; set; }", p.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFoo{System.String}.P", p.Implements[0]);
        }
        {
            var p = output.Items[0].Items[0].Items[4];
            Assert.IsNotNull(p);
            Assert.IsTrue(p.IsExplicitInterfaceImplementation);
            Assert.AreEqual("IFoo<T>.P", p.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.IFoo<T>.P", p.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.Test1.IFoo<T>.P", p.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.Test1#IFoo{T}#P", p.Name);
            Assert.AreEqual("T IFoo<T>.P { get; set; }", p.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFoo{{T}}.P", p.Implements[0]);
        }
        {
            var p = output.Items[0].Items[0].Items[5];
            Assert.IsNotNull(p);
            Assert.IsTrue(p.IsExplicitInterfaceImplementation);
            Assert.AreEqual("IFoo<string>.this[string]", p.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.IFoo<string>.this[string]", p.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.Test1.IFoo<string>.this[string]", p.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.Test1#IFoo{System#String}#Item(System.String)", p.Name);
            Assert.AreEqual("int IFoo<string>.this[string x] { get; }", p.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFoo{System.String}.Item(System.String)", p.Implements[0]);
        }
        {
            var p = output.Items[0].Items[0].Items[6];
            Assert.IsNotNull(p);
            Assert.IsTrue(p.IsExplicitInterfaceImplementation);
            Assert.AreEqual("IFoo<T>.this[T]", p.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.IFoo<T>.this[T]", p.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.Test1.IFoo<T>.this[T]", p.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.Test1#IFoo{T}#Item(`0)", p.Name);
            Assert.AreEqual("int IFoo<T>.this[T x] { get; }", p.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFoo{{T}}.Item({T})", p.Implements[0]);
        }
        {
            var e = output.Items[0].Items[0].Items[7];
            Assert.IsNotNull(e);
            Assert.IsTrue(e.IsExplicitInterfaceImplementation);
            Assert.AreEqual("IFoo.E", e.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.IFoo.E", e.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.Test1.IFoo.E", e.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.Test1#IFoo#E", e.Name);
            Assert.AreEqual("event EventHandler IFoo.E", e.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFoo.E", e.Implements[0]);
        }
    }

    [TestProperty("Related", "Generic")]
    [TestProperty("Related", "EII")]
    [TestMethod]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        var ns = output.Items[0];
        Assert.AreEqual(2, ns.Items.Count);
        {
            var type = ns.Items[0];
            Assert.IsNotNull(type);
            Assert.AreEqual("IInterface", type.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test.IInterface", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test.IInterface", type.Name);
            Assert.AreEqual("public interface IInterface", type.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.IsNull(type.Implements);

            // Verify member with EditorBrowsable.Never should be filtered out
            Assert.IsEmpty(type.Items);
        }
        {
            var type = ns.Items[1];
            Assert.IsNotNull(type);
            Assert.AreEqual("Class", type.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test.Class", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test.Class", type.Name);
            Assert.AreEqual("public class Class : IInterface", type.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test.IInterface", type.Implements[0]);

            // Verify EII member with EditorBrowsable.Never should be filtered out
            Assert.IsEmpty(type.Items);
        }
    }

    [TestProperty("Related", "Generic")]
    [TestProperty("Related", "Extension Method")]
    [TestMethod]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        // FooImple<T>
        {
            var method = output.Items[0].Items[1].Items[0];
            Assert.IsNotNull(method);
            Assert.AreEqual("M1<U>(T, U)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("FooImple<T>.M1<U>(T, U)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.FooImple<T>.M1<U>(T, U)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.FooImple`1.M1``1(`0,``0)", method.Name);
            Assert.AreEqual("public void M1<U>(T a, U b)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        var extensionMethods = output.Items[0].Items[1].ExtensionMethods;
        Assert.AreEqual(2, extensionMethods.Count);
        {
            Assert.AreEqual("Test1.FooImple`1.Test1.Extension.Eat``1", extensionMethods[0]);
            var reference = output.References[extensionMethods[0]];
            Assert.IsFalse(reference.IsDefinition);
            Assert.AreEqual("Test1.Extension.Eat``1(Test1.FooImple{``0})", reference.Definition);
            Assert.AreEqual("Eat<T>(FooImple<T>)", string.Concat(reference.NameParts[SyntaxLanguage.CSharp].Select(n => n.DisplayName)));
            Assert.AreEqual("Extension.Eat<T>(FooImple<T>)", string.Concat(reference.NameWithTypeParts[SyntaxLanguage.CSharp].Select(n => n.DisplayName)));
        }
        {
            Assert.AreEqual("Test1.Foo{`0[]}.Test1.Extension.Play``2({T}[],{Way})", extensionMethods[1]);
            var reference = output.References[extensionMethods[1]];
            Assert.IsFalse(reference.IsDefinition);
            Assert.AreEqual("Test1.Extension.Play``2(Test1.Foo{``0},``0,``1)", reference.Definition);
            Assert.AreEqual("Play<T[], Way>(Foo<T[]>, T[], Way)", string.Concat(reference.NameParts[SyntaxLanguage.CSharp].Select(n => n.DisplayName)));
            Assert.AreEqual("Extension.Play<T[], Way>(Foo<T[]>, T[], Way)", string.Concat(reference.NameWithTypeParts[SyntaxLanguage.CSharp].Select(n => n.DisplayName)));
        }
        // FooImple2<T>
        extensionMethods = output.Items[0].Items[2].ExtensionMethods;
        Assert.ContainsSingle(extensionMethods);
        {
            Assert.AreEqual("Test1.Foo{System.Object}.Test1.Extension.Play``2(System.Object,{Way})", extensionMethods[0]);
            var reference = output.References[extensionMethods[0]];
            Assert.IsFalse(reference.IsDefinition);
            Assert.AreEqual("Test1.Extension.Play``2(Test1.Foo{``0},``0,``1)", reference.Definition);
            Assert.AreEqual("Play<dynamic, Way>(Foo<dynamic>, dynamic, Way)", string.Concat(reference.NameParts[SyntaxLanguage.CSharp].Select(n => n.DisplayName)));
            Assert.AreEqual("Extension.Play<dynamic, Way>(Foo<dynamic>, dynamic, Way)", string.Concat(reference.NameWithTypeParts[SyntaxLanguage.CSharp].Select(n => n.DisplayName)));
        }
        // FooImple3<T>
        extensionMethods = output.Items[0].Items[3].ExtensionMethods;
        Assert.ContainsSingle(extensionMethods);
        {
            Assert.AreEqual("Test1.Foo{Test1.Foo{`0[]}}.Test1.Extension.Play``2(Test1.Foo{{T}[]},{Way})", extensionMethods[0]);
            var reference = output.References[extensionMethods[0]];
            Assert.IsFalse(reference.IsDefinition);
            Assert.AreEqual("Test1.Extension.Play``2(Test1.Foo{``0},``0,``1)", reference.Definition);
            Assert.AreEqual("Play<Foo<T[]>, Way>(Foo<Foo<T[]>>, Foo<T[]>, Way)", string.Concat(reference.NameParts[SyntaxLanguage.CSharp].Select(n => n.DisplayName)));
            Assert.AreEqual("Extension.Play<Foo<T[]>, Way>(Foo<Foo<T[]>>, Foo<T[]>, Way)", string.Concat(reference.NameWithTypeParts[SyntaxLanguage.CSharp].Select(n => n.DisplayName)));
        }
        // Doll
        extensionMethods = output.Items[0].Items[4].ExtensionMethods;
        Assert.AreEqual(2, extensionMethods.Count);
        {
            Assert.AreEqual("Test1.Doll.Test1.Extension.Rain", extensionMethods[0]);
            var reference = output.References[extensionMethods[0]];
            Assert.IsFalse(reference.IsDefinition);
            Assert.AreEqual("Test1.Extension.Rain(Test1.Doll)", reference.Definition);
            Assert.AreEqual("Rain(Doll)", string.Concat(reference.NameParts[SyntaxLanguage.CSharp].Select(n => n.DisplayName)));
            Assert.AreEqual("Extension.Rain(Doll)", string.Concat(reference.NameWithTypeParts[SyntaxLanguage.CSharp].Select(n => n.DisplayName)));
        }
        {
            Assert.AreEqual("Test1.Doll.Test1.Extension.Rain(Test1.Doll)", extensionMethods[1]);
            var reference = output.References[extensionMethods[1]];
            Assert.IsFalse(reference.IsDefinition);
            Assert.AreEqual("Test1.Extension.Rain(Test1.Doll,Test1.Doll)", reference.Definition);
            Assert.AreEqual("Rain(Doll, Doll)", string.Concat(reference.NameParts[SyntaxLanguage.CSharp].Select(n => n.DisplayName)));
            Assert.AreEqual("Extension.Rain(Doll, Doll)", string.Concat(reference.NameWithTypeParts[SyntaxLanguage.CSharp].Select(n => n.DisplayName)));
        }
    }

    [TestMethod]
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
        // conversion
        public static implicit operator Foo (int x) => null;
        public static explicit operator int (Foo x) => 0;
    }
}
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        // unary
        {
            var method = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(method);
            Assert.AreEqual("operator +(Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.operator +(Foo)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.operator +(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_UnaryPlus(Test1.Foo)", method.Name);
            Assert.AreEqual("public static Foo operator +(Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(method);
            Assert.AreEqual("operator -(Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.operator -(Foo)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.operator -(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_UnaryNegation(Test1.Foo)", method.Name);
            Assert.AreEqual("public static Foo operator -(Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[2];
            Assert.IsNotNull(method);
            Assert.AreEqual("operator !(Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.operator !(Foo)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.operator !(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_LogicalNot(Test1.Foo)", method.Name);
            Assert.AreEqual("public static Foo operator !(Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[3];
            Assert.IsNotNull(method);
            Assert.AreEqual("operator ~(Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.operator ~(Foo)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.operator ~(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_OnesComplement(Test1.Foo)", method.Name);
            Assert.AreEqual("public static Foo operator ~(Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[4];
            Assert.IsNotNull(method);
            Assert.AreEqual("operator ++(Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.operator ++(Foo)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.operator ++(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_Increment(Test1.Foo)", method.Name);
            Assert.AreEqual("public static Foo operator ++(Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[5];
            Assert.IsNotNull(method);
            Assert.AreEqual("operator --(Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.operator --(Foo)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.operator --(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_Decrement(Test1.Foo)", method.Name);
            Assert.AreEqual("public static Foo operator --(Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[6];
            Assert.IsNotNull(method);
            Assert.AreEqual("operator true(Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.operator true(Foo)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.operator true(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_True(Test1.Foo)", method.Name);
            Assert.AreEqual("public static Foo operator true(Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[7];
            Assert.IsNotNull(method);
            Assert.AreEqual("operator false(Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.operator false(Foo)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.operator false(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_False(Test1.Foo)", method.Name);
            Assert.AreEqual("public static Foo operator false(Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        // binary
        {
            var method = output.Items[0].Items[0].Items[8];
            Assert.IsNotNull(method);
            Assert.AreEqual("operator +(Foo, int)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.operator +(Foo, int)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.operator +(Test1.Foo, int)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_Addition(Test1.Foo,System.Int32)", method.Name);
            Assert.AreEqual("public static Foo operator +(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[9];
            Assert.IsNotNull(method);
            Assert.AreEqual("operator -(Foo, int)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.operator -(Foo, int)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.operator -(Test1.Foo, int)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_Subtraction(Test1.Foo,System.Int32)", method.Name);
            Assert.AreEqual("public static Foo operator -(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[10];
            Assert.IsNotNull(method);
            Assert.AreEqual("operator *(Foo, int)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.operator *(Foo, int)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.operator *(Test1.Foo, int)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_Multiply(Test1.Foo,System.Int32)", method.Name);
            Assert.AreEqual("public static Foo operator *(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[11];
            Assert.IsNotNull(method);
            Assert.AreEqual("operator /(Foo, int)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.operator /(Foo, int)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.operator /(Test1.Foo, int)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_Division(Test1.Foo,System.Int32)", method.Name);
            Assert.AreEqual("public static Foo operator /(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[12];
            Assert.IsNotNull(method);
            Assert.AreEqual("operator %(Foo, int)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.operator %(Foo, int)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.operator %(Test1.Foo, int)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_Modulus(Test1.Foo,System.Int32)", method.Name);
            Assert.AreEqual("public static Foo operator %(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[13];
            Assert.IsNotNull(method);
            Assert.AreEqual("operator &(Foo, int)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.operator &(Foo, int)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.operator &(Test1.Foo, int)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_BitwiseAnd(Test1.Foo,System.Int32)", method.Name);
            Assert.AreEqual("public static Foo operator &(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[14];
            Assert.IsNotNull(method);
            Assert.AreEqual("operator |(Foo, int)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.operator |(Foo, int)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.operator |(Test1.Foo, int)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_BitwiseOr(Test1.Foo,System.Int32)", method.Name);
            Assert.AreEqual("public static Foo operator |(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[15];
            Assert.IsNotNull(method);
            Assert.AreEqual("operator ^(Foo, int)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.operator ^(Foo, int)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.operator ^(Test1.Foo, int)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_ExclusiveOr(Test1.Foo,System.Int32)", method.Name);
            Assert.AreEqual("public static Foo operator ^(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[16];
            Assert.IsNotNull(method);
            Assert.AreEqual("operator >>(Foo, int)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.operator >>(Foo, int)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.operator >>(Test1.Foo, int)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_RightShift(Test1.Foo,System.Int32)", method.Name);
            Assert.AreEqual("public static Foo operator >>(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[17];
            Assert.IsNotNull(method);
            Assert.AreEqual("operator <<(Foo, int)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.operator <<(Foo, int)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.operator <<(Test1.Foo, int)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_LeftShift(Test1.Foo,System.Int32)", method.Name);
            Assert.AreEqual("public static Foo operator <<(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        // comparison
        {
            var method = output.Items[0].Items[0].Items[18];
            Assert.IsNotNull(method);
            Assert.AreEqual("operator ==(Foo, int)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.operator ==(Foo, int)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.operator ==(Test1.Foo, int)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_Equality(Test1.Foo,System.Int32)", method.Name);
            Assert.AreEqual("public static bool operator ==(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[19];
            Assert.IsNotNull(method);
            Assert.AreEqual("operator !=(Foo, int)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.operator !=(Foo, int)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.operator !=(Test1.Foo, int)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_Inequality(Test1.Foo,System.Int32)", method.Name);
            Assert.AreEqual("public static bool operator !=(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[20];
            Assert.IsNotNull(method);
            Assert.AreEqual("operator >(Foo, int)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.operator >(Foo, int)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.operator >(Test1.Foo, int)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_GreaterThan(Test1.Foo,System.Int32)", method.Name);
            Assert.AreEqual("public static bool operator >(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[21];
            Assert.IsNotNull(method);
            Assert.AreEqual("operator <(Foo, int)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.operator <(Foo, int)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.operator <(Test1.Foo, int)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_LessThan(Test1.Foo,System.Int32)", method.Name);
            Assert.AreEqual("public static bool operator <(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[22];
            Assert.IsNotNull(method);
            Assert.AreEqual("operator >=(Foo, int)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.operator >=(Foo, int)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.operator >=(Test1.Foo, int)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_GreaterThanOrEqual(Test1.Foo,System.Int32)", method.Name);
            Assert.AreEqual("public static bool operator >=(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[23];
            Assert.IsNotNull(method);
            Assert.AreEqual("operator <=(Foo, int)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.operator <=(Foo, int)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.operator <=(Test1.Foo, int)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_LessThanOrEqual(Test1.Foo,System.Int32)", method.Name);
            Assert.AreEqual("public static bool operator <=(Foo x, int y)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        // conversion
        {
            var method = output.Items[0].Items[0].Items[24];
            Assert.IsNotNull(method);
            Assert.AreEqual("implicit operator Foo(int)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.implicit operator Foo(int)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.implicit operator Test1.Foo(int)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_Implicit(System.Int32)~Test1.Foo", method.Name);
            Assert.AreEqual("public static implicit operator Foo(int x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var method = output.Items[0].Items[0].Items[25];
            Assert.IsNotNull(method);
            Assert.AreEqual("explicit operator int(Foo)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.explicit operator int(Foo)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.explicit operator int(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.op_Explicit(Test1.Foo)~System.Int32", method.Name);
            Assert.AreEqual("public static explicit operator int(Foo x)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
    }

    [TestProperty("Related", "Generic")]
    [TestMethod]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var constructor = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(constructor);
            Assert.AreEqual("Foo()", constructor.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.Foo()", constructor.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.Foo()", constructor.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.#ctor", constructor.Name);
            Assert.AreEqual("public Foo()", constructor.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var constructor = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(constructor);
            Assert.AreEqual("Foo(int)", constructor.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.Foo(int)", constructor.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.Foo(int)", constructor.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.#ctor(System.Int32)", constructor.Name);
            Assert.AreEqual("public Foo(int x)", constructor.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var constructor = output.Items[0].Items[0].Items[2];
            Assert.IsNotNull(constructor);
            Assert.AreEqual("Foo(string)", constructor.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.Foo(string)", constructor.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.Foo(string)", constructor.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.#ctor(System.String)", constructor.Name);
            Assert.AreEqual("protected Foo(string x)", constructor.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var constructor = output.Items[0].Items[1].Items[0];
            Assert.IsNotNull(constructor);
            Assert.AreEqual("Bar()", constructor.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Bar.Bar()", constructor.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.Bar()", constructor.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.#ctor", constructor.Name);
            Assert.AreEqual("public Bar()", constructor.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var constructor = output.Items[0].Items[1].Items[1];
            Assert.IsNotNull(constructor);
            Assert.AreEqual("Bar(int)", constructor.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Bar.Bar(int)", constructor.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.Bar(int)", constructor.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.#ctor(System.Int32)", constructor.Name);
            Assert.AreEqual("protected Bar(int x)", constructor.Syntax.Content[SyntaxLanguage.CSharp]);
        }
    }

    [TestProperty("Related", "Generic")]
    [TestMethod]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var field = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(field);
            Assert.AreEqual("X", field.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.X", field.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.X", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.X", field.Name);
            Assert.AreEqual("public volatile int X", field.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var field = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(field);
            Assert.AreEqual("Y", field.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.Y", field.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.Y", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.Y", field.Name);
            Assert.AreEqual("protected static readonly Foo<T> Y", field.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var field = output.Items[0].Items[0].Items[2];
            Assert.IsNotNull(field);
            Assert.AreEqual("Z", field.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.Z", field.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.Z", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.Z", field.Name);
            Assert.AreEqual("protected const string Z = \"\"", field.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var field = output.Items[0].Items[1].Items[0];
            Assert.IsNotNull(field);
            Assert.AreEqual("Black", field.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Bar.Black", field.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.Black", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.Black", field.Name);
            Assert.AreEqual("Black = 0", field.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var field = output.Items[0].Items[1].Items[1];
            Assert.IsNotNull(field);
            Assert.AreEqual("Red", field.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Bar.Red", field.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.Red", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.Red", field.Name);
            Assert.AreEqual("Red = 1", field.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var field = output.Items[0].Items[1].Items[2];
            Assert.IsNotNull(field);
            Assert.AreEqual("Blue", field.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Bar.Blue", field.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.Blue", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.Blue", field.Name);
            Assert.AreEqual("Blue = 2", field.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var field = output.Items[0].Items[1].Items[3];
            Assert.IsNotNull(field);
            Assert.AreEqual("Green", field.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Bar.Green", field.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.Green", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.Green", field.Name);
            Assert.AreEqual("Green = 4", field.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var field = output.Items[0].Items[1].Items[4];
            Assert.IsNotNull(field);
            Assert.AreEqual("White", field.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Bar.White", field.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.White", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.White", field.Name);
            Assert.AreEqual("White = 7", field.Syntax.Content[SyntaxLanguage.CSharp]);
        }
    }

    [TestProperty("Related", "Generic")]
    [TestMethod]
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
        protected internal sealed override event EventHandler<T> C;
        public override event EventHandler<T> D;
    }
    public interface IFooBar<T> where T : EventArgs
    {
        event EventHandler A;
        event EventHandler<T> D;
    }
}
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var a = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(a);
            Assert.AreEqual("A", a.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.A", a.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.A", a.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.A", a.Name);
            Assert.AreEqual("public event EventHandler A", a.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var b = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(b);
            Assert.AreEqual("B", b.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.B", b.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.B", b.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.B", b.Name);
            Assert.AreEqual("protected static event EventHandler B", b.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var c = output.Items[0].Items[0].Items[2];
            Assert.IsNotNull(c);
            Assert.AreEqual("C", c.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.C", c.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.C", c.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.C", c.Name);
            Assert.AreEqual("protected abstract event EventHandler<T> C", c.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var d = output.Items[0].Items[0].Items[3];
            Assert.IsNotNull(d);
            Assert.AreEqual("D", d.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.D", d.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.D", d.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.D", d.Name);
            Assert.AreEqual("public virtual event EventHandler<T> D", d.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var a = output.Items[0].Items[1].Items[0];
            Assert.IsNotNull(a);
            Assert.AreEqual("A", a.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Bar<T>.A", a.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar<T>.A", a.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar`1.A", a.Name);
            Assert.AreEqual("public event EventHandler A", a.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var c = output.Items[0].Items[1].Items[1];
            Assert.IsNotNull(c);
            Assert.AreEqual("C", c.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Bar<T>.C", c.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar<T>.C", c.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar`1.C", c.Name);
            Assert.AreEqual("protected override sealed event EventHandler<T> C", c.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo{{T}}.C", c.Overridden);
        }
        {
            var d = output.Items[0].Items[1].Items[2];
            Assert.IsNotNull(d);
            Assert.AreEqual("D", d.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Bar<T>.D", d.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar<T>.D", d.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar`1.D", d.Name);
            Assert.AreEqual("public override event EventHandler<T> D", d.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo{{T}}.D", d.Overridden);
        }
        {
            var a = output.Items[0].Items[2].Items[0];
            Assert.IsNotNull(a);
            Assert.AreEqual("A", a.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("IFooBar<T>.A", a.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFooBar<T>.A", a.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFooBar`1.A", a.Name);
            Assert.AreEqual("event EventHandler A", a.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var d = output.Items[0].Items[2].Items[1];
            Assert.IsNotNull(d);
            Assert.AreEqual("D", d.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("IFooBar<T>.D", d.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFooBar<T>.D", d.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFooBar`1.D", d.Name);
            Assert.AreEqual("event EventHandler<T> D", d.Syntax.Content[SyntaxLanguage.CSharp]);
        }
    }

    [TestProperty("Related", "Generic")]
    [TestMethod]
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
        public ref int G { get => throw null; }
        public ref readonly int H { get => throw null; }
    }
    public class Bar : Foo<string>, IFooBar
    {
        public new virtual int A { get; set; }
        public override int B { get { return 2; } }
        public sealed override int C { set; }
    }
    public interface IFooBar
    {
        int A { get; set; }
        int B { get; }
        int C { set; }
    }
}
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var a = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(a);
            Assert.AreEqual("A", a.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.A", a.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.A", a.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.A", a.Name);
            Assert.AreEqual("public int A { get; set; }", a.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var b = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(b);
            Assert.AreEqual("B", b.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.B", b.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.B", b.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.B", b.Name);
            Assert.AreEqual("public virtual int B { get; }", b.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var c = output.Items[0].Items[0].Items[2];
            Assert.IsNotNull(c);
            Assert.AreEqual("C", c.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.C", c.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.C", c.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.C", c.Name);
            Assert.AreEqual("public abstract int C { set; }", c.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var d = output.Items[0].Items[0].Items[3];
            Assert.IsNotNull(d);
            Assert.AreEqual("D", d.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.D", d.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.D", d.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.D", d.Name);
            Assert.AreEqual("protected int D { get; }", d.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var e = output.Items[0].Items[0].Items[4];
            Assert.IsNotNull(e);
            Assert.AreEqual("E", e.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.E", e.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.E", e.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.E", e.Name);
            Assert.AreEqual("public T E { get; protected set; }", e.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var f = output.Items[0].Items[0].Items[5];
            Assert.IsNotNull(f);
            Assert.AreEqual("F", f.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.F", f.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.F", f.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.F", f.Name);
            Assert.AreEqual("protected static int F { get; set; }", f.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var g = output.Items[0].Items[0].Items[6];
            Assert.IsNotNull(g);
            Assert.AreEqual("G", g.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.G", g.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.G", g.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.G", g.Name);
            Assert.AreEqual("public ref int G { get; }", g.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var h = output.Items[0].Items[0].Items[7];
            Assert.IsNotNull(h);
            Assert.AreEqual("H", h.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.H", h.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.H", h.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.H", h.Name);
            Assert.AreEqual("public ref readonly int H { get; }", h.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var a = output.Items[0].Items[1].Items[0];
            Assert.IsNotNull(a);
            Assert.AreEqual("A", a.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Bar.A", a.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.A", a.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.A", a.Name);
            Assert.AreEqual("public virtual int A { get; set; }", a.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var b = output.Items[0].Items[1].Items[1];
            Assert.IsNotNull(b);
            Assert.AreEqual("B", b.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Bar.B", b.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.B", b.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.B", b.Name);
            Assert.AreEqual("public override int B { get; }", b.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo{System.String}.B", b.Overridden);
        }
        {
            var c = output.Items[0].Items[1].Items[2];
            Assert.IsNotNull(c);
            Assert.AreEqual("C", c.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Bar.C", c.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.C", c.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.C", c.Name);
            Assert.AreEqual("public override sealed int C { set; }", c.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo{System.String}.C", c.Overridden);
        }
        {
            var a = output.Items[0].Items[2].Items[0];
            Assert.IsNotNull(a);
            Assert.AreEqual("A", a.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("IFooBar.A", a.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFooBar.A", a.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFooBar.A", a.Name);
            Assert.AreEqual("int A { get; set; }", a.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var b = output.Items[0].Items[2].Items[1];
            Assert.IsNotNull(b);
            Assert.AreEqual("B", b.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("IFooBar.B", b.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFooBar.B", b.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFooBar.B", b.Name);
            Assert.AreEqual("int B { get; }", b.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var c = output.Items[0].Items[2].Items[2];
            Assert.IsNotNull(c);
            Assert.AreEqual("C", c.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("IFooBar.C", c.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFooBar.C", c.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFooBar.C", c.Name);
            Assert.AreEqual("int C { set; }", c.Syntax.Content[SyntaxLanguage.CSharp]);
        }
    }

    [TestProperty("Related", "Generic")]
    [TestMethod]
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
        public sealed override int this[object x] { set; }
    }
    public interface IFooBar
    {
        int this[int x] { get; set; }
        int this[string x] { get; }
        int this[object x] { set; }
    }
}
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        // Foo<T>
        {
            var indexer = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(indexer);
            Assert.AreEqual("this[int]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.this[int]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.this[int]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.Item(System.Int32)", indexer.Name);
            Assert.AreEqual("public int this[int x] { get; set; }", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var indexer = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(indexer);
            Assert.AreEqual("this[string]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.this[string]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.this[string]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.Item(System.String)", indexer.Name);
            Assert.AreEqual("public virtual int this[string x] { get; }", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var indexer = output.Items[0].Items[0].Items[2];
            Assert.IsNotNull(indexer);
            Assert.AreEqual("this[object]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.this[object]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.this[object]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.Item(System.Object)", indexer.Name);
            Assert.AreEqual("public abstract int this[object x] { set; }", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var indexer = output.Items[0].Items[0].Items[3];
            Assert.IsNotNull(indexer);
            Assert.AreEqual("this[DateTime]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.this[DateTime]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.this[System.DateTime]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.Item(System.DateTime)", indexer.Name);
            Assert.AreEqual("protected int this[DateTime x] { get; }", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var indexer = output.Items[0].Items[0].Items[4];
            Assert.IsNotNull(indexer);
            Assert.AreEqual("this[T]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.this[T]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.this[T]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.Item(`0)", indexer.Name);
            Assert.AreEqual("public int this[T t] { get; protected set; }", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var indexer = output.Items[0].Items[0].Items[5];
            Assert.IsNotNull(indexer);
            Assert.AreEqual("this[int, T]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo<T>.this[int, T]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo<T>.this[int, T]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo`1.Item(System.Int32,`0)", indexer.Name);
            Assert.AreEqual("protected int this[int x, T t] { get; set; }", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        // Bar
        {
            var indexer = output.Items[0].Items[1].Items[0];
            Assert.IsNotNull(indexer);
            Assert.AreEqual("this[int]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Bar.this[int]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.this[int]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.Item(System.Int32)", indexer.Name);
            Assert.AreEqual("public virtual int this[int x] { get; set; }", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFooBar.Item(System.Int32)", indexer.Implements[0]);
        }
        {
            var indexer = output.Items[0].Items[1].Items[1];
            Assert.IsNotNull(indexer);
            Assert.AreEqual("this[string]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Bar.this[string]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.this[string]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.Item(System.String)", indexer.Name);
            Assert.AreEqual("public override int this[string x] { get; }", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo{System.String}.Item(System.String)", indexer.Overridden);
            Assert.AreEqual("Test1.IFooBar.Item(System.String)", indexer.Implements[0]);
        }
        {
            var indexer = output.Items[0].Items[1].Items[2];
            Assert.IsNotNull(indexer);
            Assert.AreEqual("this[object]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Bar.this[object]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.this[object]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Bar.Item(System.Object)", indexer.Name);
            Assert.AreEqual("public override sealed int this[object x] { set; }", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo{System.String}.Item(System.Object)", indexer.Overridden);
            Assert.AreEqual("Test1.IFooBar.Item(System.Object)", indexer.Implements[0]);
        }
        // IFooBar
        {
            var indexer = output.Items[0].Items[2].Items[0];
            Assert.IsNotNull(indexer);
            Assert.AreEqual("this[int]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("IFooBar.this[int]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFooBar.this[int]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFooBar.Item(System.Int32)", indexer.Name);
            Assert.AreEqual("int this[int x] { get; set; }", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var indexer = output.Items[0].Items[2].Items[1];
            Assert.IsNotNull(indexer);
            Assert.AreEqual("this[string]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("IFooBar.this[string]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFooBar.this[string]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFooBar.Item(System.String)", indexer.Name);
            Assert.AreEqual("int this[string x] { get; }", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var indexer = output.Items[0].Items[2].Items[2];
            Assert.IsNotNull(indexer);
            Assert.AreEqual("this[object]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("IFooBar.this[object]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFooBar.this[object]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.IFooBar.Item(System.Object)", indexer.Name);
            Assert.AreEqual("int this[object x] { set; }", indexer.Syntax.Content[SyntaxLanguage.CSharp]);
        }
    }

    [TestMethod]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var method = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(method);
            Assert.AreEqual(@"public void Test(int a = 1, uint b = 1, short c = 1, ushort d = 1, long e = 1, ulong f = 1, byte g = 1, sbyte h = 1, char i = '1', string j = ""1"", bool k = true, object l = null)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
    }

    [TestMethod]
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
        Directory.CreateDirectory(nameof(TestGenerateMetadataAsyncWithAssemblyInfoAndCrossReference));
        var referencedAssembly = CreateAssemblyFromCSharpCode(referenceCode, $"{nameof(TestGenerateMetadataAsyncWithAssemblyInfoAndCrossReference)}/reference.dll");
        var compilation = CreateCompilationFromCSharpCode(code, references: MetadataReference.CreateFromFile(referencedAssembly.Location));
        Assert.AreEqual("test.dll", compilation.AssemblyName);
        MetadataItem output = Verify(code);
        Assert.IsNull(output.AssemblyNameList);
        Assert.IsNull(output.NamespaceName);
        Assert.AreEqual("test.dll", output.Items[0].AssemblyNameList.First());
        Assert.IsNull(output.Items[0].NamespaceName);
        Assert.AreEqual("test.dll", output.Items[0].Items[0].AssemblyNameList.First());
        Assert.AreEqual("Test2", output.Items[0].Items[0].NamespaceName);
    }

    [TestMethod]
    [TestProperty("Related", "Multilanguage")]
    [TestProperty("Related", "Generic")]
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
        MetadataItem output = Verify(code);
        var type = output.Items[0].Items[0];
        Assert.IsNotNull(type);
        Assert.AreEqual("Foo<T>", type.DisplayNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Foo(Of T)", type.DisplayNames[SyntaxLanguage.VB]);
        Assert.AreEqual("Foo<T>", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Foo(Of T)", type.DisplayNamesWithType[SyntaxLanguage.VB]);
        Assert.AreEqual("Test1.Foo<T>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Test1.Foo(Of T)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
        Assert.AreEqual("Test1.Foo`1", type.Name);

        {
            var method = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(method);
            Assert.AreEqual("Bar<K>(int)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Bar(Of K)(Integer)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Foo<T>.Bar<K>(int)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo(Of T).Bar(Of K)(Integer)", method.DisplayNamesWithType[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo<T>.Bar<K>(int)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo(Of T).Bar(Of K)(Integer)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.Bar``1(System.Int32)", method.Name);
            Assert.ContainsSingle(method.Syntax.Parameters);
            var parameter = method.Syntax.Parameters[0];
            Assert.AreEqual("i", parameter.Name);
            Assert.AreEqual("System.Int32", parameter.Type);
            var returnValue = method.Syntax.Return;
            Assert.IsNull(returnValue);
        }

        {
            var indexer = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(indexer);
            Assert.AreEqual("this[int]", indexer.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("this[](Integer)", indexer.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Foo<T>.this[int]", indexer.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo(Of T).this[](Integer)", indexer.DisplayNamesWithType[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo<T>.this[int]", indexer.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo(Of T).this[](Integer)", indexer.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.Item(System.Int32)", indexer.Name);
            Assert.ContainsSingle(indexer.Syntax.Parameters);
            var parameter = indexer.Syntax.Parameters[0];
            Assert.AreEqual("index", parameter.Name);
            Assert.AreEqual("System.Int32", parameter.Type);
            var returnValue = indexer.Syntax.Return;
            Assert.IsNotNull(returnValue);
            Assert.AreEqual("System.Int32", returnValue.Type);
        }
    }

    [TestMethod]
    [TestProperty("Related", "Generic")]
    [TestProperty("Related", "Inheritance")]
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
        MetadataItem output = Verify(code);
        {
            var type = output.Items[0].Items[0];
            Assert.IsNotNull(type);
            Assert.AreEqual("Foo<T>", type.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo(Of T)", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Foo<T>", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo(Of T)", type.DisplayNamesWithType[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo<T>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo(Of T)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1", type.Name);
            Assert.AreEqual(2, type.Inheritance.Count);
            Assert.AreEqual("System.Collections.Generic.Dictionary{System.String,{T}}", type.Inheritance[1]);
        }
        {
            var type = output.Items[0].Items[1];
            Assert.IsNotNull(type);
            Assert.AreEqual("Foo<T1, T2, T3>", type.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo(Of T1, T2, T3)", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Foo<T1, T2, T3>", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo(Of T1, T2, T3)", type.DisplayNamesWithType[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo<T1, T2, T3>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo(Of T1, T2, T3)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`3", type.Name);
            Assert.AreEqual(2, type.Inheritance.Count);
            Assert.AreEqual("System.Collections.Generic.List{{T3}}", type.Inheritance[1]);
        }
    }

    [TestProperty("Related", "Dynamic")]
    [TestProperty("Related", "Multilanguage")]
    [TestMethod]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var field = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(field);
            Assert.AreEqual("F", field.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("F", field.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Foo.F", field.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.F", field.DisplayNamesWithType[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.F", field.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.F", field.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.F", field.Name);
            Assert.AreEqual("public dynamic F", field.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Public F As Object", field.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(method);
            Assert.AreEqual("M(dynamic)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("M(Object)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Foo.M(dynamic)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.M(Object)", method.DisplayNamesWithType[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.M(dynamic)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.M(Object)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.M(System.Object)", method.Name);
            Assert.AreEqual("public dynamic M(dynamic arg)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Public Function M(arg As Object) As Object", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[2];
            Assert.IsNotNull(method);
            Assert.AreEqual("P", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("P", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Foo.P", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.P", method.DisplayNamesWithType[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.P", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.P", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.P", method.Name);
            Assert.AreEqual("public dynamic P { get; protected set; }", method.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Public Property P As Object", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[3];
            Assert.IsNotNull(method);
            Assert.AreEqual("this[dynamic]", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("this[](Object)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Foo.this[dynamic]", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo.this[](Object)", method.DisplayNamesWithType[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.this[dynamic]", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo.this[](Object)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.Item(System.Object)", method.Name);
            Assert.AreEqual("public dynamic this[dynamic index] { get; }", method.Syntax.Content[SyntaxLanguage.CSharp]);
            // TODO: https://github.com/dotnet/roslyn/issues/14684
            Assert.AreEqual("Public ReadOnly Default Property this[](index As Object) As Object", method.Syntax.Content[SyntaxLanguage.VB]);
        }
    }

    [TestMethod]
    [TestProperty("Related", "Multilanguage")]
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
        MetadataItem output = Verify(code);
        {
            var type = output.Items[0].Items[0];
            Assert.IsNotNull(type);
            Assert.AreEqual("Foo", type.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo", type.Name);
            Assert.ContainsSingle(type.Inheritance);
            Assert.AreEqual("System.Object", type.Inheritance[0]);

            Assert.AreEqual("public static class Foo", type.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Public Module Foo", type.Syntax.Content[SyntaxLanguage.VB]);
        }
    }

    [TestMethod]
    [TestProperty("Related", "Generic")]
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
        MetadataItem output = Verify(code);
        {
            var type = output.Items[0].Items[0];
            Assert.IsNotNull(type);
            Assert.AreEqual("Foo<T1, T2>", type.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo(Of T1, T2)", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Foo<T1, T2>", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo(Of T1, T2)", type.DisplayNamesWithType[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo<T1, T2>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo(Of T1, T2)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`2", type.Name);
            Assert.ContainsSingle(type.Inheritance);
            Assert.AreEqual("System.Object", type.Inheritance[0]);
        }
        {
            var type = output.Items[0].Items[1];
            Assert.IsNotNull(type);
            Assert.AreEqual("Foo<T1, T2>.Bar<T3>", type.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo(Of T1, T2).Bar(Of T3)", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Foo<T1, T2>.Bar<T3>", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo(Of T1, T2).Bar(Of T3)", type.DisplayNamesWithType[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo<T1, T2>.Bar<T3>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo(Of T1, T2).Bar(Of T3)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`2.Bar`1", type.Name);
            Assert.ContainsSingle(type.Inheritance);
            Assert.AreEqual("System.Object", type.Inheritance[0]);
        }
        {
            var type = output.Items[0].Items[2];
            Assert.IsNotNull(type);
            Assert.AreEqual("Foo<T1, T2>.FooBar", type.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo(Of T1, T2).FooBar", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Foo<T1, T2>.FooBar", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Foo(Of T1, T2).FooBar", type.DisplayNamesWithType[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo<T1, T2>.FooBar", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.Foo(Of T1, T2).FooBar", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`2.FooBar", type.Name);
            Assert.ContainsSingle(type.Inheritance);
            Assert.AreEqual("System.Object", type.Inheritance[0]);
        }
    }

    [TestMethod]
    [TestProperty("Related", "Attribute")]
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
    [Test(new object[]{null, ""abc"", 'd', 1.1f, 1.2, (sbyte)2, (byte)3, (short)4, (ushort)5, 6, 7u, 8l, 9ul, new int[]{ 10, 11, 12 }, new byte[0]{}})]
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
        MetadataItem output = Verify(code);
        var @class = output.Items[0].Items[0];
        Assert.IsNotNull(@class);
        Assert.AreEqual("TestAttribute", @class.DisplayNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("TestAttribute", @class.DisplayNamesWithType[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Test1.TestAttribute", @class.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual(@"[Serializable]
[AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = true)]
[TypeConverter(typeof(TestAttribute))]
[TypeConverter(typeof(TestAttribute[]))]
[Test(""test"")]
[Test(new int[] { 1, 2, 3 })]
[Test(new object[] { null, ""abc"", 'd', 1.1, 1.2, 2, 3, 4, 5, 6, 7, 8, 9, new int[] { 10, 11, 12 }, new byte[] { } })]
[Test(new Type[] { typeof(Func<>), typeof(Func<,>), typeof(Func<string, string>) })]
public class TestAttribute : Attribute", @class.Syntax.Content[SyntaxLanguage.CSharp]);

        Assert.IsNotNull(@class.Attributes);
        Assert.AreEqual(5, @class.Attributes.Count);

        Assert.AreEqual("System.SerializableAttribute", @class.Attributes[0].Type);
        Assert.AreEqual("System.SerializableAttribute.#ctor", @class.Attributes[0].Constructor);
        Assert.IsNotNull(@class.Attributes[0].Arguments);
        Assert.IsEmpty(@class.Attributes[0].Arguments);
        Assert.IsNull(@class.Attributes[0].NamedArguments);

        Assert.AreEqual("System.AttributeUsageAttribute", @class.Attributes[1].Type);
        Assert.AreEqual("System.AttributeUsageAttribute.#ctor(System.AttributeTargets)", @class.Attributes[1].Constructor);
        Assert.IsNotNull(@class.Attributes[1].Arguments);
        Assert.ContainsSingle(@class.Attributes[1].Arguments);
        Assert.AreEqual("System.AttributeTargets", @class.Attributes[1].Arguments[0].Type);
        Assert.AreEqual(32767, @class.Attributes[1].Arguments[0].Value);
        Assert.IsNotNull(@class.Attributes[1].NamedArguments);
        Assert.AreEqual(2, @class.Attributes[1].NamedArguments.Count);
        Assert.AreEqual("Inherited", @class.Attributes[1].NamedArguments[0].Name);
        Assert.AreEqual("System.Boolean", @class.Attributes[1].NamedArguments[0].Type);
        Assert.AreEqual(true, @class.Attributes[1].NamedArguments[0].Value);
        Assert.AreEqual("AllowMultiple", @class.Attributes[1].NamedArguments[1].Name);
        Assert.AreEqual("System.Boolean", @class.Attributes[1].NamedArguments[1].Type);
        Assert.AreEqual(true, @class.Attributes[1].NamedArguments[1].Value);

        Assert.AreEqual("System.ComponentModel.TypeConverterAttribute", @class.Attributes[2].Type);
        Assert.AreEqual("System.ComponentModel.TypeConverterAttribute.#ctor(System.Type)", @class.Attributes[2].Constructor);
        Assert.IsNotNull(@class.Attributes[2].Arguments);
        Assert.ContainsSingle(@class.Attributes[2].Arguments);
        Assert.AreEqual("System.Type", @class.Attributes[2].Arguments[0].Type);
        Assert.AreEqual("Test1.TestAttribute", @class.Attributes[2].Arguments[0].Value);
        Assert.IsNull(@class.Attributes[2].NamedArguments);

        Assert.AreEqual("System.ComponentModel.TypeConverterAttribute", @class.Attributes[3].Type);
        Assert.AreEqual("System.ComponentModel.TypeConverterAttribute.#ctor(System.Type)", @class.Attributes[3].Constructor);
        Assert.IsNotNull(@class.Attributes[3].Arguments);
        Assert.ContainsSingle(@class.Attributes[3].Arguments);
        Assert.AreEqual("System.Type", @class.Attributes[3].Arguments[0].Type);
        Assert.AreEqual("Test1.TestAttribute[]", @class.Attributes[3].Arguments[0].Value);
        Assert.IsNull(@class.Attributes[3].NamedArguments);

        Assert.AreEqual("Test1.TestAttribute", @class.Attributes[4].Type);
        Assert.AreEqual("Test1.TestAttribute.#ctor(System.Object)", @class.Attributes[4].Constructor);
        Assert.IsNotNull(@class.Attributes[4].Arguments);
        Assert.ContainsSingle(@class.Attributes[4].Arguments);
        Assert.AreEqual("System.String", @class.Attributes[4].Arguments[0].Type);
        Assert.AreEqual("test", @class.Attributes[4].Arguments[0].Value);
        Assert.IsNull(@class.Attributes[4].NamedArguments);

        var ctor = @class.Items[0];
        Assert.IsNotNull(ctor);
        Assert.AreEqual(@"[Test(1)]
[Test(2)]
public TestAttribute(object obj)", ctor.Syntax.Content[SyntaxLanguage.CSharp]);

        var property = @class.Items[1];
        Assert.IsNotNull(property);
        Assert.AreEqual(@"[Test(5)]
public object Property { get; set; }", property.Syntax.Content[SyntaxLanguage.CSharp]);
    }

    [TestMethod]
    public void TestGenerateMetadataWithDefaultParameterEnumFlagsValues()
    {
        string code = @"
using System;

namespace Test1
{
    public class Test
    {
        public void Defined(Base64FormattingOptions options = Base64FormattingOptions.None) { }
        public void Undefined(AttributeTargets targets = (AttributeTargets)0) { }
    }
}
";
        MetadataItem output = Verify(code);

        var defined = output.Items[0].Items[0].Items[0];
        Assert.IsNotNull(defined);
        Assert.AreEqual("public void Defined(Base64FormattingOptions options = Base64FormattingOptions.None)", defined.Syntax.Content[SyntaxLanguage.CSharp]);

        var undefined = output.Items[0].Items[0].Items[1];
        Assert.IsNotNull(undefined);
        Assert.AreEqual("public void Undefined(AttributeTargets targets = (AttributeTargets)0)", undefined.Syntax.Content[SyntaxLanguage.CSharp]);
    }

    [TestMethod]
    public void TestGenerateMetadataWithDefaultParameterEnumValues()
    {
        string code = @"
using System;

namespace Test1
{
    public class Test
    {
        public void Defined(ConsoleSpecialKey key = ConsoleSpecialKey.ControlC) { }
        public void Undefined(ConsoleKey key = ConsoleKey.None) { }
    }
}
";
        MetadataItem output = Verify(code);

        var defined = output.Items[0].Items[0].Items[0];
        Assert.IsNotNull(defined);
        Assert.AreEqual("public void Defined(ConsoleSpecialKey key = ConsoleSpecialKey.ControlC)", defined.Syntax.Content[SyntaxLanguage.CSharp]);

        var undefined = output.Items[0].Items[0].Items[1];
        Assert.IsNotNull(undefined);
        Assert.AreEqual("public void Undefined(ConsoleKey key = ConsoleKey.None)", undefined.Syntax.Content[SyntaxLanguage.CSharp]);
    }

    [TestMethod]
    public void TestGenerateMetadataWithDefaultParameterNullablePrimitive()
    {
        string code = @"
using System;

namespace Test1
{
    public class Test
    {
        public void PrimitiveNull(int? i = null) { }
        public void PrimitiveDefault(int? i = 0) { }
        public void PrimitiveValue(int? i = 123) { }
    }
}
";
        MetadataItem output = Verify(code);

        var primitiveNull = output.Items[0].Items[0].Items[0];
        Assert.IsNotNull(primitiveNull);
        Assert.AreEqual("public void PrimitiveNull(int? i = null)", primitiveNull.Syntax.Content[SyntaxLanguage.CSharp]);

        var primitiveDefault = output.Items[0].Items[0].Items[1];
        Assert.IsNotNull(primitiveDefault);
        Assert.AreEqual("public void PrimitiveDefault(int? i = 0)", primitiveDefault.Syntax.Content[SyntaxLanguage.CSharp]);

        var primitiveValue = output.Items[0].Items[0].Items[2];
        Assert.IsNotNull(primitiveValue);
        Assert.AreEqual("public void PrimitiveValue(int? i = 123)", primitiveValue.Syntax.Content[SyntaxLanguage.CSharp]);
    }

    [TestMethod]
    public void TestGenerateMetadataWithDefaultParameterNullableEnum()
    {
        string code = @"
using System;

namespace Test1
{
    public class Test
    {
        public void EnumNull(ConsoleSpecialKey? key = null) { }
        public void EnumDefault(ConsoleSpecialKey? key = ConsoleSpecialKey.ControlC) { }
        public void EnumValue(ConsoleSpecialKey? key = ConsoleSpecialKey.ControlBreak) { }
        public void EnumUndefinedDefault(ConsoleKey? key = ConsoleKey.None) { }
        public void EnumUndefinedValue(ConsoleKey? key = (ConsoleKey)999) { }
    }
}
";
        MetadataItem output = Verify(code);

        var flagsNull = output.Items[0].Items[0].Items[0];
        Assert.IsNotNull(flagsNull);
        Assert.AreEqual("public void EnumNull(ConsoleSpecialKey? key = null)", flagsNull.Syntax.Content[SyntaxLanguage.CSharp]);

        var flagsDefault = output.Items[0].Items[0].Items[1];
        Assert.IsNotNull(flagsDefault);
        Assert.AreEqual("public void EnumDefault(ConsoleSpecialKey? key = ConsoleSpecialKey.ControlC)", flagsDefault.Syntax.Content[SyntaxLanguage.CSharp]);

        var flagsValue = output.Items[0].Items[0].Items[2];
        Assert.IsNotNull(flagsValue);
        Assert.AreEqual("public void EnumValue(ConsoleSpecialKey? key = ConsoleSpecialKey.ControlBreak)", flagsValue.Syntax.Content[SyntaxLanguage.CSharp]);

        var enumUndefinedDefault = output.Items[0].Items[0].Items[3];
        Assert.IsNotNull(enumUndefinedDefault);
        Assert.AreEqual("public void EnumUndefinedDefault(ConsoleKey? key = ConsoleKey.None)", enumUndefinedDefault.Syntax.Content[SyntaxLanguage.CSharp]);

        var enumUndefinedValue = output.Items[0].Items[0].Items[4];
        Assert.IsNotNull(enumUndefinedValue);
        Assert.AreEqual("public void EnumUndefinedValue(ConsoleKey? key = (ConsoleKey)999)", enumUndefinedValue.Syntax.Content[SyntaxLanguage.CSharp]);
    }

    [TestMethod]
    public void TestGenerateMetadataWithDefaultParameterNullableEnumFlags()
    {
        string code = @"
using System;

namespace Test1
{
    public class Test
    {
        public void FlagsNull(Base64FormattingOptions? options = null) { }
        public void FlagsDefault(Base64FormattingOptions? options = Base64FormattingOptions.None) { }
        public void FlagsValue(Base64FormattingOptions? options = Base64FormattingOptions.InsertLineBreaks) { }
        public void FlagsUndefinedDefault(AttributeTargets? targets = (AttributeTargets)0) { }
        public void FlagsUndefinedValue(AttributeTargets? targets = (AttributeTargets)65536) { }
    }
}
";
        MetadataItem output = Verify(code);

        var enumNull = output.Items[0].Items[0].Items[0];
        Assert.IsNotNull(enumNull);
        Assert.AreEqual("public void FlagsNull(Base64FormattingOptions? options = null)", enumNull.Syntax.Content[SyntaxLanguage.CSharp]);

        var enumDefault = output.Items[0].Items[0].Items[1];
        Assert.IsNotNull(enumDefault);
        Assert.AreEqual("public void FlagsDefault(Base64FormattingOptions? options = Base64FormattingOptions.None)", enumDefault.Syntax.Content[SyntaxLanguage.CSharp]);

        var enumValue = output.Items[0].Items[0].Items[2];
        Assert.IsNotNull(enumValue);
        Assert.AreEqual("public void FlagsValue(Base64FormattingOptions? options = Base64FormattingOptions.InsertLineBreaks)", enumValue.Syntax.Content[SyntaxLanguage.CSharp]);

        var flagsUndefinedDefault = output.Items[0].Items[0].Items[3];
        Assert.IsNotNull(flagsUndefinedDefault);
        Assert.AreEqual("public void FlagsUndefinedDefault(AttributeTargets? targets = (AttributeTargets)0)", flagsUndefinedDefault.Syntax.Content[SyntaxLanguage.CSharp]);

        var flagsUndefinedValue = output.Items[0].Items[0].Items[4];
        Assert.IsNotNull(flagsUndefinedValue);
        Assert.AreEqual("public void FlagsUndefinedValue(AttributeTargets? targets = (AttributeTargets)65536)", flagsUndefinedValue.Syntax.Content[SyntaxLanguage.CSharp]);
    }

    [TestMethod]
    public void TestGenerateMetadataWithDefaultParameterValueAttribute()
    {
        string code = @"
using System;
using System.Runtime.InteropServices;

namespace Test1
{
    public class Test
    {
        public void Double([Optional][DefaultParameterValue(0)]double i) { }
        public void Float([Optional][DefaultParameterValue(0)]float i) { }
        public void Decimal([Optional][DefaultParameterValue(0)]decimal i) { }
        public void Long([Optional][DefaultParameterValue(0)]long i) { }
        public void Uint([Optional][DefaultParameterValue(0)]uint i) { }
    }
}
";
        MetadataItem output = Verify(code);

        var metadataItems = output.Items[0].Items[0].Items;
        Assert.AreEqual(5, metadataItems.Count);
        Assert.AreEqual("public void Double(double i = 0)", metadataItems[0].Syntax.Content[SyntaxLanguage.CSharp]);
        Assert.AreEqual("public void Float(float i = 0)", metadataItems[1].Syntax.Content[SyntaxLanguage.CSharp]);
        Assert.AreEqual("public void Decimal(decimal i = 0)", metadataItems[2].Syntax.Content[SyntaxLanguage.CSharp]);
        Assert.AreEqual("public void Long(long i = 0)", metadataItems[3].Syntax.Content[SyntaxLanguage.CSharp]);
        Assert.AreEqual("public void Uint(uint i = default)", metadataItems[4].Syntax.Content[SyntaxLanguage.CSharp]);
    }

    [TestMethod]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var field = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(field);
            Assert.AreEqual("public const ushort Test = 123", field.Syntax.Content[SyntaxLanguage.CSharp]);
        }
    }

    [TestMethod]
    [TestProperty("Related", "Multilanguage")]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var field = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(field);
            Assert.AreEqual(@"public const char Test = '\udbff'", field.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Public Const Test As Char = ChrW(&HDBFF)", field.Syntax.Content[SyntaxLanguage.VB]);
        }
    }

    [TestMethod]
    [TestProperty("Related", "ExtensionMethod")]
    [TestProperty("Related", "Multilanguage")]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        var ns = output.Items[0];
        Assert.IsNotNull(ns);
        var method = ns.Items[0].Items[0];
        Assert.IsNotNull(method);
        Assert.IsTrue(method.IsExtensionMethod);
        Assert.AreEqual("public static void Method1(this object obj)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Public Shared Sub Method1(obj As Object)", method.Syntax.Content[SyntaxLanguage.VB]);
    }

    [TestMethod]
    [TestProperty("Related", "Generic")]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        var ns = output.Items[0];
        Assert.IsNotNull(ns);
        var i1 = ns.Items[0];
        Assert.IsNotNull(i1);
        Assert.AreEqual("Test1.I1`1", i1.Name);
        Assert.ContainsSingle(i1.Items);
        Assert.IsNull(i1.InheritedMembers);
        var m1 = i1.Items[0];
        Assert.AreEqual("Test1.I1`1.M1(`0)", m1.Name);

        var i2 = ns.Items[1];
        Assert.IsNotNull(i2);
        Assert.AreEqual("Test1.I2`1", i2.Name);
        Assert.IsEmpty(i2.Items);
        Assert.AreEqual(2, i2.InheritedMembers.Count);
        CollectionAssert.AreEqual(new[] { "Test1.I1{System.String}.M1(System.String)", "Test1.I1{{T}}.M1({T})" }, i2.InheritedMembers.ToArray());

        var r1 = output.References["Test1.I1{System.String}.M1(System.String)"];
        Assert.IsFalse(r1.IsDefinition);
        Assert.AreEqual("Test1.I1`1.M1(`0)", r1.Definition);

        var r2 = output.References["Test1.I1{{T}}.M1({T})"];
        Assert.IsFalse(r1.IsDefinition);
        Assert.AreEqual("Test1.I1`1.M1(`0)", r1.Definition);
    }

    [TestMethod]
    [TestProperty("Related", "Generic")]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        var ns = output.Items[0];
        Assert.IsNotNull(ns);
        var foo = ns.Items[0];
        Assert.IsNotNull(foo);
        Assert.AreEqual("Test1.Foo", foo.Name);
        Assert.ContainsSingle(foo.Items);
        var bar = foo.Items[0];
        Assert.AreEqual("Test1.Foo.Bar(System.Int32)", bar.Name);
        Assert.AreEqual("public int Bar(int x = 0)", bar.Syntax.Content[SyntaxLanguage.CSharp]);
    }

    [TestMethod]
    [TestProperty("Related", "ValueTuple")]
    public void TestGenerateMetadataAsyncWithTupleParameter()
    {
        string code = @"
namespace Test1
{
    public class Foo
    {
        public int Bar((string prefix, string uri) @namespace) => 1;

        public (int x, int y) M() => (1, 2);
    }
}
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        var ns = output.Items[0];
        Assert.IsNotNull(ns);
        var foo = ns.Items[0];
        Assert.IsNotNull(foo);
        Assert.AreEqual("Test1.Foo", foo.Name);
        Assert.AreEqual(2, foo.Items.Count);
        var bar = foo.Items[0];
        Assert.AreEqual("Test1.Foo.Bar(System.ValueTuple{System.String,System.String})", bar.Name);
        Assert.AreEqual("public int Bar((string prefix, string uri) @namespace)", bar.Syntax.Content[SyntaxLanguage.CSharp]);
        var m = foo.Items[1];
        Assert.AreEqual("Test1.Foo.M", m.Name);
        Assert.AreEqual("public (int x, int y) M()", m.Syntax.Content[SyntaxLanguage.CSharp]);
    }

    [TestMethod]
    [TestProperty("Related", "ValueTuple")]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        var ns = output.Items[0];
        Assert.IsNotNull(ns);
        var foo = ns.Items[0];
        Assert.IsNotNull(foo);
        Assert.AreEqual("Test1.Foo", foo.Name);
        Assert.ContainsSingle(foo.Items);
        var bar = foo.Items[0];
        Assert.AreEqual("Test1.Foo.Bar(System.ValueTuple{System.String,System.String})", bar.Name);
        Assert.AreEqual("public int Bar((string, string) @namespace)", bar.Syntax.Content[SyntaxLanguage.CSharp]);
    }

    [TestMethod]
    [TestProperty("Related", "ValueTuple")]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        var ns = output.Items[0];
        Assert.IsNotNull(ns);
        var foo = ns.Items[0];
        Assert.IsNotNull(foo);
        Assert.AreEqual("Test1.Foo", foo.Name);
        Assert.ContainsSingle(foo.Items);
        var bar = foo.Items[0];
        Assert.AreEqual("Test1.Foo.Bar(System.ValueTuple{System.String,System.String})", bar.Name);
        Assert.AreEqual("public int Bar((string, string uri) @namespace)", bar.Syntax.Content[SyntaxLanguage.CSharp]);
    }

    [TestMethod]
    [TestProperty("Related", "ValueTuple")]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        var ns = output.Items[0];
        Assert.IsNotNull(ns);
        var foo = ns.Items[0];
        Assert.IsNotNull(foo);
        Assert.AreEqual("Test1.Foo", foo.Name);
        Assert.ContainsSingle(foo.Items);
        var bar = foo.Items[0];
        Assert.AreEqual("Test1.Foo.Bar(System.ValueTuple{System.String,System.String}[])", bar.Name);
        Assert.AreEqual("public int Bar((string prefix, string uri)[] namespaces)", bar.Syntax.Content[SyntaxLanguage.CSharp]);
    }

    [TestMethod]
    [TestProperty("Related", "ValueTuple")]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        var ns = output.Items[0];
        Assert.IsNotNull(ns);
        var foo = ns.Items[0];
        Assert.IsNotNull(foo);
        Assert.AreEqual("Test1.Foo", foo.Name);
        Assert.ContainsSingle(foo.Items);
        var bar = foo.Items[0];
        Assert.AreEqual("Test1.Foo.Bar(System.Collections.Generic.IEnumerable{System.ValueTuple{System.String,System.String}})", bar.Name);
        Assert.AreEqual("public int Bar(IEnumerable<(string prefix, string uri)> namespaces)", bar.Syntax.Content[SyntaxLanguage.CSharp]);
    }

    [TestMethod]
    [TestProperty("Related", "ValueTuple")]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        var ns = output.Items[0];
        Assert.IsNotNull(ns);
        var foo = ns.Items[0];
        Assert.IsNotNull(foo);
        Assert.AreEqual("Test1.Foo", foo.Name);
        Assert.ContainsSingle(foo.Items);
        var bar = foo.Items[0];
        Assert.AreEqual("Test1.Foo.Bar", bar.Name);
        Assert.AreEqual("public (string prefix, string uri) Bar()", bar.Syntax.Content[SyntaxLanguage.CSharp]);
    }

    [TestMethod]
    [TestProperty("Related", "ValueTuple")]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        var ns = output.Items[0];
        Assert.IsNotNull(ns);
        var foo = ns.Items[0];
        Assert.IsNotNull(foo);
        Assert.AreEqual("Test1.Foo", foo.Name);
        Assert.ContainsSingle(foo.Items);
        var bar = foo.Items[0];
        Assert.AreEqual("Test1.Foo.Bar", bar.Name);
        Assert.AreEqual("public (string, string) Bar()", bar.Syntax.Content[SyntaxLanguage.CSharp]);
    }

    [TestMethod]
    [TestProperty("Related", "ValueTuple")]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        var ns = output.Items[0];
        Assert.IsNotNull(ns);
        var foo = ns.Items[0];
        Assert.IsNotNull(foo);
        Assert.AreEqual("Test1.Foo", foo.Name);
        Assert.ContainsSingle(foo.Items);
        var bar = foo.Items[0];
        Assert.AreEqual("Test1.Foo.Bar", bar.Name);
        Assert.AreEqual("public (string, string uri) Bar()", bar.Syntax.Content[SyntaxLanguage.CSharp]);
    }

    [TestMethod]
    [TestProperty("Related", "ValueTuple")]
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
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        var ns = output.Items[0];
        Assert.IsNotNull(ns);
        var foo = ns.Items[0];
        Assert.IsNotNull(foo);
        Assert.AreEqual("Test1.Foo", foo.Name);
        Assert.ContainsSingle(foo.Items);
        var bar = foo.Items[0];
        Assert.AreEqual("Test1.Foo.Bar", bar.Name);
        Assert.AreEqual("public IEnumerable<(string prefix, string uri)> Bar()", bar.Syntax.Content[SyntaxLanguage.CSharp]);
    }

    private static Compilation CreateCompilationFromCSharpCode(string code, IDictionary<string, string> msbuildProperties = null, params MetadataReference[] references)
    {
        return CompilationHelper.CreateCompilationFromCSharpCode(code, msbuildProperties ?? EmptyMSBuildProperties, "test.dll", references);
    }

    private static Assembly CreateAssemblyFromCSharpCode(string code, string assemblyName)
    {
        // MemoryStream fails when MetadataReference.CreateFromAssembly with error: Empty path name is not legal
        var compilation = CreateCompilationFromCSharpCode(code);
        EmitResult result;
        using (FileStream stream = new(assemblyName, FileMode.Create))
        {
            result = compilation.Emit(stream);
        }

        Assert.IsTrue(result.Success, string.Join(',', result.Diagnostics.Select(s => s.GetMessage())));
        return Assembly.LoadFile(Path.GetFullPath(assemblyName));
    }

    [TestMethod]
    [TestProperty("Related", "NativeInteger")]
    public void TestGenerateMetadataWithMethodUsingNativeInteger()
    {
        string code = @"
namespace Test1
{
    public class Foo
    {
        public void Test(
            IntPtr a, UIntPtr b,
            nint c, nuint d,
            nint e = -1, nuint f = 1)
        {
        }
    }
}
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var method = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(method);
            Assert.AreEqual("public void Test(IntPtr a, UIntPtr b, nint c, nuint d, nint e = -1, nuint f = 1)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
    }

    [TestMethod]
    [TestProperty("Related", "FunctionPointer")]
    public void TestGenerateMetadataWithImplicitManagedFunctionPointer()
    {
        string code = @"
namespace Test1
{
    public class Foo
    {
        public delegate*<void> a;
        public delegate*<int, void> b;
        public delegate*<ref int, void> c;
        public delegate*<out int, void> d;
        public delegate*<in int, void> e;
        public delegate*<int* , void> f;
        public delegate*<ref readonly int> g;
    }
}
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var fnptr = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate*<void> a", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate*<int, void> b", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[2];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate*<ref int, void> c", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[3];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate*<out int, void> d", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[4];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate*<in int, void> e", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[5];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate*<int*, void> f", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[6];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate*<ref readonly int> g", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);
        }
    }

    [TestMethod]
    [TestProperty("Related", "FunctionPointer")]
    public void TestGenerateMetadataWithExplicitManagedFunctionPointer()
    {
        string code = @"
namespace Test1
{
    public class Foo
    {
        public delegate* managed<void> a;
        public delegate* managed<int, void> b;
        public delegate* managed<ref int, void> c;
        public delegate* managed<out int, void> d;
        public delegate* managed<in int, void> e;
        public delegate* managed<int* , void> f;
        public delegate* managed<ref readonly int> g;
    }
}
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var fnptr = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate*<void> a", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate*<int, void> b", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[2];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate*<ref int, void> c", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[3];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate*<out int, void> d", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[4];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate*<in int, void> e", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[5];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate*<int*, void> f", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[6];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate*<ref readonly int> g", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);
        }
    }

    [TestMethod]
    [TestProperty("Related", "FunctionPointer")]
    public void TestGenerateMetadataWithUnmanagedFunctionPointer()
    {
        string code = @"
namespace Test1
{
    public class Foo
    {
        public delegate* unmanaged<void> a;
        public delegate* unmanaged<int, void> b;
        public delegate* unmanaged<ref int, void> c;
        public delegate* unmanaged<out int, void> d;
        public delegate* unmanaged<in int, void> e;
        public delegate* unmanaged<int* , void> f;
        public delegate* unmanaged<ref readonly int> g;
    }
}
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var fnptr = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate* unmanaged<void> a", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate* unmanaged<int, void> b", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[2];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate* unmanaged<ref int, void> c", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[3];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate* unmanaged<out int, void> d", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[4];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate* unmanaged<in int, void> e", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[5];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate* unmanaged<int*, void> f", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[6];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate* unmanaged<ref readonly int> g", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);
        }
    }

    [TestMethod]
    [TestProperty("Related", "FunctionPointer")]
    public void TestGenerateMetadataWithSingleCallConvFunctionPointer()
    {
        string code = @"
namespace Test1
{
    public class Foo
    {
        public delegate* unmanaged[Stdcall]<void> a;
        public delegate* unmanaged[Stdcall]<int, void> b;
        public delegate* unmanaged[Stdcall]<ref int, void> c;
        public delegate* unmanaged[Stdcall]<out int, void> d;
        public delegate* unmanaged[Stdcall]<in int, void> e;
        public delegate* unmanaged[Stdcall]<int* , void> f;
        public delegate* unmanaged[Stdcall]<ref readonly int> g;
    }
}
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var fnptr = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate* unmanaged[Stdcall]<void> a", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate* unmanaged[Stdcall]<int, void> b", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[2];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate* unmanaged[Stdcall]<ref int, void> c", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[3];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate* unmanaged[Stdcall]<out int, void> d", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[4];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate* unmanaged[Stdcall]<in int, void> e", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[5];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate* unmanaged[Stdcall]<int*, void> f", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[6];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate* unmanaged[Stdcall]<ref readonly int> g", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);
        }
    }

    [TestMethod]
    [TestProperty("Related", "FunctionPointer")]
    public void TestGenerateMetadataWithMultiCallConvFunctionPointer()
    {
        string code = @"
namespace Test1
{
    public class Foo
    {
        public delegate* unmanaged[Stdcall, Thiscall]<void> a;
        public delegate* unmanaged[Stdcall, Thiscall]<int, void> b;
        public delegate* unmanaged[Stdcall, Thiscall]<ref int, void> c;
        public delegate* unmanaged[Stdcall, Thiscall]<out int, void> d;
        public delegate* unmanaged[Stdcall, Thiscall]<in int, void> e;
        public delegate* unmanaged[Stdcall, Thiscall]<int* , void> f;
        public delegate* unmanaged[Stdcall, Thiscall]<ref readonly int> g;
    }
}
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var fnptr = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate* unmanaged[Stdcall, Thiscall]<void> a", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate* unmanaged[Stdcall, Thiscall]<int, void> b", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[2];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate* unmanaged[Stdcall, Thiscall]<ref int, void> c", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[3];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate* unmanaged[Stdcall, Thiscall]<out int, void> d", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[4];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate* unmanaged[Stdcall, Thiscall]<in int, void> e", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[5];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate* unmanaged[Stdcall, Thiscall]<int*, void> f", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[6];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate* unmanaged[Stdcall, Thiscall]<ref readonly int> g", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);
        }
    }

    [TestMethod]
    [TestProperty("Related", "FunctionPointer")]
    public void TestGenerateMetadataWithNestedFunctionPointer()
    {
        string code = @"
namespace Test1
{
    public class Foo
    {
        public delegate*<delegate*<void>> a;
        public delegate*<delegate* unmanaged<void>> b;
        public delegate*<delegate* unmanaged[Stdcall]<void>> c;
        public delegate*<delegate* unmanaged[Stdcall, Thiscall]<void>> d;
    }
}
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var fnptr = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate*<delegate*<void>> a", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate*<delegate* unmanaged<void>> b", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[2];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate*<delegate* unmanaged[Stdcall]<void>> c", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);

            fnptr = output.Items[0].Items[0].Items[3];
            Assert.IsNotNull(fnptr);
            Assert.AreEqual("public delegate*<delegate* unmanaged[Stdcall, Thiscall]<void>> d", fnptr.Syntax.Content[SyntaxLanguage.CSharp]);
        }
    }

    [TestProperty("Related", "ReadonlyMember")]
    [TestMethod]
    public void TestGenerateMetadataWithReadonlyMember()
    {
        string code = @"
namespace Test1
{
    public struct S
    {
        public readonly void M() {}

        public readonly int P1 { get => throw null; set => throw null; }

        public readonly int P2 { get => throw null; }

        public readonly int P3 { set => throw null; }

        public int P4 { readonly get => throw null; set => throw null; }

        public int P5 { get => throw null; readonly set => throw null; }
    }
}
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var method = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(method);
            Assert.AreEqual("M()", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("S.M()", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.S.M()", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.S.M", method.Name);
            Assert.AreEqual("public readonly void M()", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var property = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(property);
            Assert.AreEqual("P1", property.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("S.P1", property.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.S.P1", property.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.S.P1", property.Name);
            Assert.AreEqual("public readonly int P1 { get; set; }", property.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var property = output.Items[0].Items[0].Items[2];
            Assert.IsNotNull(property);
            Assert.AreEqual("P2", property.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("S.P2", property.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.S.P2", property.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.S.P2", property.Name);
            Assert.AreEqual("public readonly int P2 { get; }", property.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var property = output.Items[0].Items[0].Items[3];
            Assert.IsNotNull(property);
            Assert.AreEqual("P3", property.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("S.P3", property.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.S.P3", property.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.S.P3", property.Name);
            Assert.AreEqual("public readonly int P3 { set; }", property.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var property = output.Items[0].Items[0].Items[4];
            Assert.IsNotNull(property);
            Assert.AreEqual("P4", property.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("S.P4", property.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.S.P4", property.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.S.P4", property.Name);
            Assert.AreEqual("public int P4 { readonly get; set; }", property.Syntax.Content[SyntaxLanguage.CSharp]);
        }
        {
            var property = output.Items[0].Items[0].Items[5];
            Assert.IsNotNull(property);
            Assert.AreEqual("P5", property.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("S.P5", property.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.S.P5", property.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.S.P5", property.Name);
            Assert.AreEqual("public int P5 { get; readonly set; }", property.Syntax.Content[SyntaxLanguage.CSharp]);
        }
    }

    [TestProperty("Related", "ReadonlyStruct")]
    [TestMethod]
    public void TestGenerateMetadataWithReadonlyStruct()
    {
        string code = @"
namespace Test1
{
    public readonly struct S
    {
    }
}
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var type = output.Items[0].Items[0];
            Assert.IsNotNull(type);
            Assert.AreEqual("S", type.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("S", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.S", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.S", type.Name);
            Assert.AreEqual("public readonly struct S", type.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.IsNull(type.Implements);
        }
    }

    [TestProperty("Related", "RefStruct")]
    [TestMethod]
    public void TestGenerateMetadataWithRefStruct()
    {
        string code = @"
namespace Test1
{
    public ref struct S
    {
    }
}
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var type = output.Items[0].Items[0];
            Assert.IsNotNull(type);
            Assert.AreEqual("S", type.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("S", type.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.S", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("Test1.S", type.Name);
            Assert.AreEqual("public ref struct S", type.Syntax.Content[SyntaxLanguage.CSharp]);
            Assert.IsNull(type.Implements);
        }
    }

    [TestMethod]
    public void TestGenerateMetadataWithCastOperatorOverloads()
    {
        var code =
            """
                namespace Test
                {
                    public class Bar
                    {
                        public static implicit operator Bar(uint value) => new();
                        public static implicit operator Bar(string value) => new();

                        public static explicit operator checked uint(Bar value) => 0;
                        public static explicit operator checked string[](Bar value) => "";
                    }
                }
                """;

        var output = Verify(code);
        var type = output.Items[0].Items[0];

        CollectionAssert.AreEqual(new[] {
            "Test.Bar.op_Implicit(System.UInt32)~Test.Bar",
            "Test.Bar.op_Implicit(System.String)~Test.Bar",
            "Test.Bar.op_CheckedExplicit(Test.Bar)~System.UInt32",
            "Test.Bar.op_CheckedExplicit(Test.Bar)~System.String[]"
        }, type.Items.Select(item => item.Name).ToArray());

        CollectionAssert.AreEqual(new[] {
            "implicit operator Bar(uint)",
            "implicit operator Bar(string)",
            "explicit operator checked uint(Bar)",
            "explicit operator checked string[](Bar)"
        }, type.Items.Select(item => item.DisplayNames[SyntaxLanguage.CSharp]).ToArray());

        CollectionAssert.AreEqual(new[] {
            "Bar.implicit operator Bar(uint)",
            "Bar.implicit operator Bar(string)",
            "Bar.explicit operator checked uint(Bar)",
            "Bar.explicit operator checked string[](Bar)"
        }, type.Items.Select(item => item.DisplayNamesWithType[SyntaxLanguage.CSharp]).ToArray());

        CollectionAssert.AreEqual(new[] {
            "Test.Bar.implicit operator Test.Bar(uint)",
            "Test.Bar.implicit operator Test.Bar(string)",
            "Test.Bar.explicit operator checked uint(Test.Bar)",
            "Test.Bar.explicit operator checked string[](Test.Bar)"
        }, type.Items.Select(item => item.DisplayQualifiedNames[SyntaxLanguage.CSharp]).ToArray());

        CollectionAssert.AreEqual(new[] {
            "Test.Bar.op_Implicit*",
            "Test.Bar.op_Implicit*",
            "Test.Bar.op_CheckedExplicit*",
            "Test.Bar.op_CheckedExplicit*"
        }, type.Items.Select(item => item.Overload).ToArray());

        Assert.AreEqual("implicit operator", string.Concat(output.References["Test.Bar.op_Implicit*"].NameParts[SyntaxLanguage.CSharp].Select(p => p.DisplayName)));
        Assert.AreEqual("explicit operator checked", string.Concat(output.References["Test.Bar.op_CheckedExplicit*"].NameParts[SyntaxLanguage.CSharp].Select(p => p.DisplayName)));
    }

    [TestMethod]
    public void TestGenerateMetadataWithPrivateMembers()
    {
        var code =
            """
            namespace Test
            {
                internal class Foo : IFoo
                {
                    internal void M1();
                    protected internal void M2();
                    private protected void M3();
                    private void M4();
                }

                internal interface IFoo { }
            }
            """;

        var output = Verify(code, new() { IncludePrivateMembers = true });
        var foo = output.Items[0].Items[0];
        Assert.AreEqual("internal class Foo : IFoo", foo.Syntax.Content[SyntaxLanguage.CSharp]);
        CollectionAssert.AreEqual(new[] {
            "internal void M1()",
            "protected internal void M2()",
            "private protected void M3()",
            "private void M4()"
        }, foo.Items.Select(item => item.Syntax.Content[SyntaxLanguage.CSharp]).ToArray());
    }

    [TestMethod]
    public void TestAllowCompilationErrors()
    {
        var code =
            """
            namespace Test
            {
                public class Foo : Bar {}
            }
            """;

        var output = Verify(code, new() { AllowCompilationErrors = true });
        var foo = output.Items[0].Items[0];
        Assert.AreEqual("public class Foo : Bar", foo.Syntax.Content[SyntaxLanguage.CSharp]);
    }

    [TestMethod]
    public void TestIncludeExplicitInterfaceImplementations()
    {
        var code =
            """
            namespace Test
            {
                public class Foo : IFoo { void IFoo.Bar(); }
                public interface IFoo { void Bar(); }
            }
            """;

        var output = Verify(code, new() { IncludeExplicitInterfaceImplementations = true });
        var foo = output.Items[0].Items[0];
        Assert.AreEqual("public class Foo : IFoo", foo.Syntax.Content[SyntaxLanguage.CSharp]);
        Assert.AreEqual("void IFoo.Bar()", foo.Items[0].Syntax.Content[SyntaxLanguage.CSharp]);
    }

    [TestMethod]
    public void TestExcludeDocumentationComment()
    {
        var code =
            """
            namespace Test
            {
                public class Foo
                {
                    /// <exclude />
                    public void F1() {}
                }
            }
            """;

        var output = Verify(code);
        var foo = output.Items[0].Items[0];
        Assert.AreEqual("public class Foo", foo.Syntax.Content[SyntaxLanguage.CSharp]);
        Assert.IsEmpty(foo.Items);
    }

    [TestMethod]
    public void TestDefineConstantsMSBuildProperty()
    {
        var code =
            """
            namespace Test
            {
                public class Foo
                {
            #if TEST
                    public void F1() {}
            #endif
                }
            }
            """;

        // Test with DefineConstants
        {
            var output = Verify(code, msbuildProperties: new Dictionary<string, string> { ["DefineConstants"] = "TEST;DUMMY" });
            var foo = output.Items[0].Items[0];
            Assert.AreEqual("Test.Foo.F1", foo.Items[0].Name);
        }
        // Test without DefineConstants
        {
            var output = Verify(code, msbuildProperties: EmptyMSBuildProperties);
            var foo = output.Items[0].Items[0];
            Assert.IsEmpty(foo.Items);
        }
    }

    [TestMethod]
    public void TestGenerateMetadataWithReference()
    {
        string code = @"
namespace Test
{
    public class Foo
    {
        public TupleLibrary.XmlTasks Tasks{ get;set; }
    }
}
";
        var references = new string[] { "TestData/TupleLibrary.dll" }.Select(assemblyPath =>
        {
            var documentation = XmlDocumentationProvider.CreateFromFile(Path.ChangeExtension(assemblyPath, ".xml"));
            return MetadataReference.CreateFromFile(assemblyPath, documentation: documentation);
        }).ToArray();

        // Act
        var output = Verify(code, references: references);

        // Assert
        Assert.Contains("TupleLibrary", output.References.Keys);
        Assert.Contains("TupleLibrary.XmlTasks", output.References.Keys);
    }
}
