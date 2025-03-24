// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.DataContracts.ManagedReference;
using Microsoft.CodeAnalysis;

namespace Docfx.Dotnet.Tests;

[TestProperty("EntityType", "Model")]
[DoNotParallelize]
[TestClass]
public class GenerateMetadataFromVBUnitTest
{
    private static readonly Dictionary<string, string> EmptyMSBuildProperties = [];

    private static MetadataItem Verify(string code, ExtractMetadataConfig config = null, IDictionary<string, string> msbuildProperties = null, MetadataReference[] references = null)
    {
        var compilation = CompilationHelper.CreateCompilationFromVBCode(code, msbuildProperties ?? EmptyMSBuildProperties, "test.dll", references);
        return compilation.Assembly.GenerateMetadataItem(compilation, config);
    }

    [TestProperty("Related", "Generic")]
    [TestMethod]
    public void TestGenerateMetadataWithClass()
    {
        string code = @"
Imports System.Collections.Generic
Namespace Test1
    Public Class Class1
    End Class
    Public Class Class2(Of T)
        Inherits List(Of T)
    End Class
    Public Class Class3(Of T1, T2 As T1)
    End Class
    Public Class Class4(Of T1 As { Structure, IEnumerable(Of T2) }, T2 As { Class, New })
    End Class
End Namespace
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var type = output.Items[0].Items[0];
            Assert.IsNotNull(type);
            Assert.AreEqual("Class1", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Class1", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Class1", type.Name);
            Assert.AreEqual("Public Class Class1", type.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var type = output.Items[0].Items[1];
            Assert.IsNotNull(type);
            Assert.AreEqual("Class2(Of T)", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Class2(Of T)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Class2`1", type.Name);
            Assert.AreEqual("Public Class Class2(Of T) Inherits List(Of T) Implements IList(Of T), ICollection(Of T), IReadOnlyList(Of T), IReadOnlyCollection(Of T), IEnumerable(Of T), IList, ICollection, IEnumerable", type.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var type = output.Items[0].Items[2];
            Assert.IsNotNull(type);
            Assert.AreEqual("Class3(Of T1, T2)", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Class3(Of T1, T2)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Class3`2", type.Name);
            Assert.AreEqual("Public Class Class3(Of T1, T2 As T1)", type.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var type = output.Items[0].Items[3];
            Assert.IsNotNull(type);
            Assert.AreEqual("Class4(Of T1, T2)", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Class4(Of T1, T2)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Class4`2", type.Name);
            Assert.AreEqual("Public Class Class4(Of T1 As {Structure, IEnumerable(Of T2)}, T2 As {Class, New})", type.Syntax.Content[SyntaxLanguage.VB]);
        }
    }

    [TestMethod]
    public void TestGenerateMetadataWithEnum()
    {
        string code = @"
Namespace Test1
    Public Enum Enum1
    End Enum
    Public Enum Enum2 As Byte
    End Enum
    Public Enum Enum3 As Integer
    End Enum
End Namespace
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var type = output.Items[0].Items[0];
            Assert.IsNotNull(type);
            Assert.AreEqual("Enum1", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Enum1", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Enum1", type.Name);
            Assert.AreEqual("Public Enum Enum1", type.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var type = output.Items[0].Items[1];
            Assert.IsNotNull(type);
            Assert.AreEqual("Enum2", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Enum2", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Enum2", type.Name);
            Assert.AreEqual("Public Enum Enum2 As Byte", type.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var type = output.Items[0].Items[2];
            Assert.IsNotNull(type);
            Assert.AreEqual("Enum3", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Enum3", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Enum3", type.Name);
            Assert.AreEqual("Public Enum Enum3", type.Syntax.Content[SyntaxLanguage.VB]);
        }
    }

    [TestProperty("Related", "Generic")]
    [TestMethod]
    public void TestGenerateMetadataWithInterface()
    {
        string code = @"
Namespace Test1
    Public Interface IA
    End Interface
    Public Interface IB(Of T As Class)
    End Interface
    Public Interface IC(Of TItem As {IA, New})
        Inherits IA, IB(Of TItem())
    End Interface
End Namespace
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var type = output.Items[0].Items[0];
            Assert.IsNotNull(type);
            Assert.AreEqual("IA", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IA", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IA", type.Name);
            Assert.AreEqual("Public Interface IA", type.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var type = output.Items[0].Items[1];
            Assert.IsNotNull(type);
            Assert.AreEqual("IB(Of T)", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IB(Of T)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IB`1", type.Name);
            Assert.AreEqual("Public Interface IB(Of T As Class)", type.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var type = output.Items[0].Items[2];
            Assert.IsNotNull(type);
            Assert.AreEqual("IC(Of TItem)", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IC(Of TItem)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IC`1", type.Name);
            Assert.AreEqual("Public Interface IC(Of TItem As {IA, New}) Inherits IA, IB(Of TItem())", type.Syntax.Content[SyntaxLanguage.VB]);
        }
    }

    [TestProperty("Related", "Generic")]
    [TestMethod]
    public void TestGenerateMetadataWithStructure()
    {
        string code = @"
Namespace Test1
    Public Structure S1
    End Structure
    Public Structure S2(Of T As Class)
    End Structure
    Public Structure S3(Of T1 As {Class, IA, New}, T2 As IB(Of T1))
        Implements IA, IB(Of T1())
    End Structure
    Public Interface IA
    End Interface
    Public Interface IB(Of T As Class)
    End Interface
End Namespace
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var type = output.Items[0].Items[0];
            Assert.IsNotNull(type);
            Assert.AreEqual("S1", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.S1", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.S1", type.Name);
            Assert.AreEqual("Public Structure S1", type.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var type = output.Items[0].Items[1];
            Assert.IsNotNull(type);
            Assert.AreEqual("S2(Of T)", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.S2(Of T)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.S2`1", type.Name);
            Assert.AreEqual("Public Structure S2(Of T As Class)", type.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var type = output.Items[0].Items[2];
            Assert.IsNotNull(type);
            Assert.AreEqual("S3(Of T1, T2)", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.S3(Of T1, T2)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.S3`2", type.Name);
            Assert.AreEqual("Public Structure S3(Of T1 As {Class, IA, New}, T2 As IB(Of T1)) Implements IA, IB(Of T1())", type.Syntax.Content[SyntaxLanguage.VB]);
        }
    }

    [TestMethod]
    public void TestGenerateMetadataWithInternalInterfaceAndInherits()
    {
        string code = @"
Namespace Test1
    Public Class Foo
       Implements IFoo 
    End Class
    Internal Interface IFoo
    End Interface
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);

        var foo = output.Items[0].Items[0];
        Assert.IsNotNull(foo);
        Assert.AreEqual("Foo", foo.DisplayNames[SyntaxLanguage.VB]);
        Assert.AreEqual("Foo", foo.DisplayNamesWithType[SyntaxLanguage.VB]);
        Assert.AreEqual("Test1.Foo", foo.DisplayQualifiedNames[SyntaxLanguage.VB]);
        Assert.AreEqual("Public Class Foo", foo.Syntax.Content[SyntaxLanguage.VB]);
        Assert.IsNull(foo.Implements);
    }

    [TestMethod]
    public void TestGenerateMetadataWithProtectedInterfaceAndInherits()
    {
        string code = @"
Namespace Test1
    Public Class Foo
       Protected Interface IFoo
       End Interface
       Public Class SubFoo 
          Implements IFoo 
       End Class
    End Class

";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);

        var subFoo = output.Items[0].Items[2];
        Assert.IsNotNull(subFoo);
        Assert.AreEqual("Foo.SubFoo", subFoo.DisplayNames[SyntaxLanguage.VB]);
        Assert.AreEqual("Foo.SubFoo", subFoo.DisplayNamesWithType[SyntaxLanguage.VB]);
        Assert.AreEqual("Test1.Foo.SubFoo", subFoo.DisplayQualifiedNames[SyntaxLanguage.VB]);
        Assert.AreEqual("Public Class Foo.SubFoo Implements Foo.IFoo", subFoo.Syntax.Content[SyntaxLanguage.VB]);
        Assert.IsNotNull(subFoo.Implements);
        Assert.AreEqual("Test1.Foo.IFoo", subFoo.Implements[0]);
    }

    [TestMethod]
    public void TestGenerateMetadataWithPublicInterfaceNestedInternal()
    {
        string code = @"
Namespace Test1
    Internal Class FooInternal
        Public Interface IFoo
        End Interface
    End Class
    Public Class Foo
       Implements FooInternal.IFoo 
    End Class
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);

        var foo = output.Items[0].Items[0];
        Assert.IsNotNull(foo);
        Assert.AreEqual("Foo", foo.DisplayNames[SyntaxLanguage.VB]);
        Assert.AreEqual("Foo", foo.DisplayNamesWithType[SyntaxLanguage.VB]);
        Assert.AreEqual("Test1.Foo", foo.DisplayQualifiedNames[SyntaxLanguage.VB]);
        Assert.AreEqual("Public Class Foo", foo.Syntax.Content[SyntaxLanguage.VB]);
        Assert.IsNull(foo.Implements);
    }

    [TestProperty("Related", "Generic")]
    [TestMethod]
    public void TestGenerateMetadataWithDelegate()
    {
        string code = @"
Namespace Test1
    Public Delegate Sub D1
    Public Delegate Sub D2(Of T As Class)(x() as integer)
    Public Delegate Function D3(Of T1 As Class, T2 As {T1, New})(ByRef x As T1) As T2
End Namespace
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var type = output.Items[0].Items[0];
            Assert.IsNotNull(type);
            Assert.AreEqual("D1", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.D1", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.D1", type.Name);
            Assert.AreEqual("Public Delegate Sub D1()", type.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var type = output.Items[0].Items[1];
            Assert.IsNotNull(type);
            Assert.AreEqual("D2(Of T)", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.D2(Of T)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.D2`1", type.Name);
            Assert.AreEqual("Public Delegate Sub D2(Of T As Class)(x As Integer())", type.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var type = output.Items[0].Items[2];
            Assert.IsNotNull(type);
            Assert.AreEqual("D3(Of T1, T2)", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.D3(Of T1, T2)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.D3`2", type.Name);
            Assert.AreEqual("Public Delegate Function D3(Of T1 As Class, T2 As {T1, New})(ByRef x As T1) As T2", type.Syntax.Content[SyntaxLanguage.VB]);
        }
    }

    [TestMethod]
    public void TestGenerateMetadataWithModule()
    {
        string code = @"
Namespace Test1
    Public Module M1
    End Module
End Namespace
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var type = output.Items[0].Items[0];
            Assert.IsNotNull(type);
            Assert.AreEqual("M1", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.M1", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.M1", type.Name);
            Assert.AreEqual("Public Module M1", type.Syntax.Content[SyntaxLanguage.VB]);
        }
    }

    [TestProperty("Related", "Generic")]
    [TestProperty("Related", "Inheritance")]
    [TestMethod]
    public void TestGenerateMetadataWithMethod()
    {
        string code = @"
Namespace Test1
    Public MustInherit Class Foo(Of T)
        Public Overridable Sub M1(x As Integer, ParamArray y() As Integer)
        End Sub
        Public MustOverride Sub M2(Of T1 As Class, T2 As Foo(Of T1))(x As T1, ByRef y As T2())
        Public Sub M3
        End Sub
        Protected Friend Shared Function M4(Of T1 As Class)(x As T) As T1
            Return Nothing
        End Function
    End Class
    Public MustInherit Class Bar
        Inherits Foo(Of String)
        Implements IFooBar
        Public Overrides Sub M1(x As Integer, y() As Integer)
        End Sub
        Public NotOverridable Overrides Sub M2(Of T1 As Class, T2 As Foo(Of T1))(x As T1, ByRef y() As T2)
        End Sub
        Public Shadows Sub M3()
        End Sub
    End Class
    Public Interface IFooBar
        Sub M1(x As Integer, ParamArray y() As Integer)
        Sub M2(Of T1 As Class, T2 As Foo(Of T1))(x As T1, ByRef y() As T2)
        Sub M3()
    End Interface
End Namespace
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        // Foo<T>
        {
            var method = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(method);
            Assert.AreEqual("M1(Integer, ParamArray Integer())", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo(Of T).M1(Integer, ParamArray Integer())", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.M1(System.Int32,System.Int32[])", method.Name);
            Assert.AreEqual("Public Overridable Sub M1(x As Integer, ParamArray y As Integer())", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(method);
            Assert.AreEqual("M2(Of T1, T2)(T1, ByRef T2())", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo(Of T).M2(Of T1, T2)(T1, ByRef T2())", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.M2``2(``0,``1[]@)", method.Name);
            Assert.AreEqual("Public MustOverride Sub M2(Of T1 As Class, T2 As Foo(Of T1))(x As T1, ByRef y As T2())", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[2];
            Assert.IsNotNull(method);
            Assert.AreEqual("M3()", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo(Of T).M3()", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.M3", method.Name);
            Assert.AreEqual("Public Sub M3()", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[3];
            Assert.IsNotNull(method);
            Assert.AreEqual("M4(Of T1)(T)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo(Of T).M4(Of T1)(T)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.M4``1(`0)", method.Name);
            Assert.AreEqual("Protected Shared Function M4(Of T1 As Class)(x As T) As T1", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        // Bar
        {
            var method = output.Items[0].Items[1].Items[0];
            Assert.IsNotNull(method);
            Assert.AreEqual("M1(Integer, Integer())", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.M1(Integer, Integer())", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.M1(System.Int32,System.Int32[])", method.Name);
            Assert.AreEqual("Public Overrides Sub M1(x As Integer, y As Integer())", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[1].Items[1];
            Assert.IsNotNull(method);
            Assert.AreEqual("M2(Of T1, T2)(T1, ByRef T2())", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.M2(Of T1, T2)(T1, ByRef T2())", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.M2``2(``0,``1[]@)", method.Name);
            Assert.AreEqual("Public NotOverridable Overrides Sub M2(Of T1 As Class, T2 As Foo(Of T1))(x As T1, ByRef y As T2())", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[1].Items[2];
            Assert.IsNotNull(method);
            Assert.AreEqual("M3()", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.M3()", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.M3", method.Name);
            Assert.AreEqual("Public Sub M3()", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        // IFooBar
        {
            var method = output.Items[0].Items[2].Items[0];
            Assert.IsNotNull(method);
            Assert.AreEqual("M1(Integer, ParamArray Integer())", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IFooBar.M1(Integer, ParamArray Integer())", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IFooBar.M1(System.Int32,System.Int32[])", method.Name);
            Assert.AreEqual("Sub M1(x As Integer, ParamArray y As Integer())", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[2].Items[1];
            Assert.IsNotNull(method);
            Assert.AreEqual("M2(Of T1, T2)(T1, ByRef T2())", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IFooBar.M2(Of T1, T2)(T1, ByRef T2())", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IFooBar.M2``2(``0,``1[]@)", method.Name);
            Assert.AreEqual("Sub M2(Of T1 As Class, T2 As Foo(Of T1))(x As T1, ByRef y As T2())", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[2].Items[2];
            Assert.IsNotNull(method);
            Assert.AreEqual("M3()", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IFooBar.M3()", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IFooBar.M3", method.Name);
            Assert.AreEqual("Sub M3()", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        // inheritance of Foo<T>
        {
            var inheritedMembers = output.Items[0].Items[0].InheritedMembers;
            Assert.IsNotNull(inheritedMembers);
            Assert.AreEqual(
                new string[]
                {
                    "System.Object.ToString",
                    "System.Object.Equals(System.Object)",
                    "System.Object.Equals(System.Object,System.Object)",
                    "System.Object.ReferenceEquals(System.Object,System.Object)",
                    "System.Object.GetHashCode",
                    "System.Object.GetType",
                    "System.Object.Finalize",
                    "System.Object.MemberwiseClone",
                }.OrderBy(s => s),
                inheritedMembers.OrderBy(s => s));
        }
        // inheritance of Bar
        {
            var inheritedMembers = output.Items[0].Items[1].InheritedMembers;
            Assert.IsNotNull(inheritedMembers);
            Assert.AreEqual(
                new string[]
                {
                    "Test1.Foo{System.String}.M4``1(System.String)",
                    "System.Object.ToString",
                    "System.Object.Equals(System.Object)",
                    "System.Object.Equals(System.Object,System.Object)",
                    "System.Object.ReferenceEquals(System.Object,System.Object)",
                    "System.Object.GetHashCode",
                    "System.Object.GetType",
                    "System.Object.Finalize",
                    "System.Object.MemberwiseClone",
                }.OrderBy(s => s),
                inheritedMembers.OrderBy(s => s));
        }
    }

    [TestMethod]
    public void TestGenerateMetadataWithOperator()
    {
        string code = @"
Namespace Test1
    Public Class Foo
        Public Shared Operator +(x As Foo) As Foo
            Return x
        End Operator
        Public Shared Operator -(x As Foo) As Foo
            Return x
        End Operator
        Public Shared Operator Not(x As Foo) As Foo
            Return x
        End Operator
        Public Shared Operator IsTrue(x As Foo) As Boolean
            Return True
        End Operator
        Public Shared Operator IsFalse(x As Foo) As Boolean
            Return False
        End Operator

        Public Shared Operator +(x As Foo, y As Foo) As Foo
            Return x
        End Operator
        Public Shared Operator -(x As Foo, y As Foo) As Foo
            Return x
        End Operator
        Public Shared Operator *(x As Foo, y As Foo) As Foo
            Return x
        End Operator
        Public Shared Operator /(x As Foo, y As Foo) As Foo
            Return x
        End Operator
        Public Shared Operator Mod(x As Foo, y As Foo) As Foo
            Return x
        End Operator
        Public Shared Operator And(x As Foo, y As Foo) As Foo
            Return x
        End Operator
        Public Shared Operator Or(x As Foo, y As Foo) As Foo
            Return x
        End Operator
        Public Shared Operator Xor(x As Foo, y As Foo) As Foo
            Return x
        End Operator
        Public Shared Operator >>(x As Foo, y As Integer) As Foo
            Return x
        End Operator
        Public Shared Operator <<(x As Foo, y As Integer) As Foo
            Return x
        End Operator

        Public Shared Operator =(x As Foo, y As Integer) As Boolean
            Return True
        End Operator
        Public Shared Operator <>(x As Foo, y As Integer) As Boolean
            Return True
        End Operator
        Public Shared Operator >(x As Foo, y As Integer) As Boolean
            Return True
        End Operator
        Public Shared Operator <(x As Foo, y As Integer) As Boolean
            Return True
        End Operator
        Public Shared Operator >=(x As Foo, y As Integer) As Boolean
            Return True
        End Operator
        Public Shared Operator <=(x As Foo, y As Integer) As Boolean
            Return True
        End Operator

        Public Shared Widening Operator CType(x As Integer) As Foo
            Return Nothing
        End Operator
        Public Shared Narrowing Operator CType(x As Foo) As Integer
            Return 1
        End Operator
    End Class
End Namespace
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        // unary
        {
            var method = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(method);
            Assert.AreEqual("+(Foo)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.+(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.op_UnaryPlus(Test1.Foo)", method.Name);
            Assert.AreEqual("Public Shared Operator +(x As Foo) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(method);
            Assert.AreEqual("-(Foo)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.-(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.op_UnaryNegation(Test1.Foo)", method.Name);
            Assert.AreEqual("Public Shared Operator -(x As Foo) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[2];
            Assert.IsNotNull(method);
            Assert.AreEqual("Not(Foo)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.Not(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.op_OnesComplement(Test1.Foo)", method.Name);
            Assert.AreEqual("Public Shared Operator Not(x As Foo) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[3];
            Assert.IsNotNull(method);
            Assert.AreEqual("IsTrue(Foo)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.IsTrue(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.op_True(Test1.Foo)", method.Name);
            Assert.AreEqual("Public Shared Operator IsTrue(x As Foo) As Boolean", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[4];
            Assert.IsNotNull(method);
            Assert.AreEqual("IsFalse(Foo)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.IsFalse(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.op_False(Test1.Foo)", method.Name);
            Assert.AreEqual("Public Shared Operator IsFalse(x As Foo) As Boolean", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        // binary
        {
            var method = output.Items[0].Items[0].Items[5];
            Assert.IsNotNull(method);
            Assert.AreEqual("+(Foo, Foo)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.+(Test1.Foo, Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.op_Addition(Test1.Foo,Test1.Foo)", method.Name);
            Assert.AreEqual("Public Shared Operator +(x As Foo, y As Foo) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[6];
            Assert.IsNotNull(method);
            Assert.AreEqual("-(Foo, Foo)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.-(Test1.Foo, Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.op_Subtraction(Test1.Foo,Test1.Foo)", method.Name);
            Assert.AreEqual("Public Shared Operator -(x As Foo, y As Foo) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[7];
            Assert.IsNotNull(method);
            Assert.AreEqual("*(Foo, Foo)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.*(Test1.Foo, Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.op_Multiply(Test1.Foo,Test1.Foo)", method.Name);
            Assert.AreEqual("Public Shared Operator *(x As Foo, y As Foo) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[8];
            Assert.IsNotNull(method);
            Assert.AreEqual("/(Foo, Foo)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo./(Test1.Foo, Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.op_Division(Test1.Foo,Test1.Foo)", method.Name);
            Assert.AreEqual("Public Shared Operator /(x As Foo, y As Foo) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[9];
            Assert.IsNotNull(method);
            Assert.AreEqual("Mod(Foo, Foo)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.Mod(Test1.Foo, Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.op_Modulus(Test1.Foo,Test1.Foo)", method.Name);
            Assert.AreEqual("Public Shared Operator Mod(x As Foo, y As Foo) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[10];
            Assert.IsNotNull(method);
            Assert.AreEqual("And(Foo, Foo)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.And(Test1.Foo, Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.op_BitwiseAnd(Test1.Foo,Test1.Foo)", method.Name);
            Assert.AreEqual("Public Shared Operator And(x As Foo, y As Foo) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[11];
            Assert.IsNotNull(method);
            Assert.AreEqual("Or(Foo, Foo)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.Or(Test1.Foo, Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.op_BitwiseOr(Test1.Foo,Test1.Foo)", method.Name);
            Assert.AreEqual("Public Shared Operator Or(x As Foo, y As Foo) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[12];
            Assert.IsNotNull(method);
            Assert.AreEqual("Xor(Foo, Foo)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.Xor(Test1.Foo, Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.op_ExclusiveOr(Test1.Foo,Test1.Foo)", method.Name);
            Assert.AreEqual("Public Shared Operator Xor(x As Foo, y As Foo) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[13];
            Assert.IsNotNull(method);
            Assert.AreEqual(">>(Foo, Integer)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.>>(Test1.Foo, Integer)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.op_RightShift(Test1.Foo,System.Int32)", method.Name);
            Assert.AreEqual("Public Shared Operator >>(x As Foo, y As Integer) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[14];
            Assert.IsNotNull(method);
            Assert.AreEqual("<<(Foo, Integer)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.<<(Test1.Foo, Integer)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.op_LeftShift(Test1.Foo,System.Int32)", method.Name);
            Assert.AreEqual("Public Shared Operator <<(x As Foo, y As Integer) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        // comparison
        {
            var method = output.Items[0].Items[0].Items[15];
            Assert.IsNotNull(method);
            Assert.AreEqual("=(Foo, Integer)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.=(Test1.Foo, Integer)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.op_Equality(Test1.Foo,System.Int32)", method.Name);
            Assert.AreEqual("Public Shared Operator =(x As Foo, y As Integer) As Boolean", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[16];
            Assert.IsNotNull(method);
            Assert.AreEqual("<>(Foo, Integer)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.<>(Test1.Foo, Integer)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.op_Inequality(Test1.Foo,System.Int32)", method.Name);
            Assert.AreEqual("Public Shared Operator <>(x As Foo, y As Integer) As Boolean", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[17];
            Assert.IsNotNull(method);
            Assert.AreEqual(">(Foo, Integer)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.>(Test1.Foo, Integer)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.op_GreaterThan(Test1.Foo,System.Int32)", method.Name);
            Assert.AreEqual("Public Shared Operator >(x As Foo, y As Integer) As Boolean", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[18];
            Assert.IsNotNull(method);
            Assert.AreEqual("<(Foo, Integer)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.<(Test1.Foo, Integer)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.op_LessThan(Test1.Foo,System.Int32)", method.Name);
            Assert.AreEqual("Public Shared Operator <(x As Foo, y As Integer) As Boolean", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[19];
            Assert.IsNotNull(method);
            Assert.AreEqual(">=(Foo, Integer)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.>=(Test1.Foo, Integer)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.op_GreaterThanOrEqual(Test1.Foo,System.Int32)", method.Name);
            Assert.AreEqual("Public Shared Operator >=(x As Foo, y As Integer) As Boolean", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[20];
            Assert.IsNotNull(method);
            Assert.AreEqual("<=(Foo, Integer)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.<=(Test1.Foo, Integer)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.op_LessThanOrEqual(Test1.Foo,System.Int32)", method.Name);
            Assert.AreEqual("Public Shared Operator <=(x As Foo, y As Integer) As Boolean", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        // conversion
        {
            var method = output.Items[0].Items[0].Items[21];
            Assert.IsNotNull(method);
            Assert.AreEqual("CType(Integer)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.CType(Integer)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.op_Implicit(System.Int32)~Test1.Foo", method.Name);
            Assert.AreEqual("Public Shared Widening Operator CType(x As Integer) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[0].Items[22];
            Assert.IsNotNull(method);
            Assert.AreEqual("CType(Foo)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.CType(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo.op_Explicit(Test1.Foo)~System.Int32", method.Name);
            Assert.AreEqual("Public Shared Narrowing Operator CType(x As Foo) As Integer", method.Syntax.Content[SyntaxLanguage.VB]);
        }
    }

    [TestProperty("Related", "Generic")]
    [TestMethod]
    public void TestGenerateMetadataWithConstructor()
    {
        string code = @"
Namespace Test1
    Public MustInherit Class Foo(Of T)
        Protected Sub New(x As T())
        End Sub
    End Class
    Public Class Bar
        Inherits Foo(Of String)
        Protected Friend Sub New()
            MyBase.New(New String() {})
        End Sub
        Public Sub New(x As String())
        End Sub
    End Class
End Namespace
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        // Foo<T>
        {
            var method = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(method);
            Assert.AreEqual("New(T())", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo(Of T).New(T())", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.#ctor(`0[])", method.Name);
            Assert.AreEqual("Protected Sub New(x As T())", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        // Bar
        {
            var method = output.Items[0].Items[1].Items[0];
            Assert.IsNotNull(method);
            Assert.AreEqual("New()", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.New()", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.#ctor", method.Name);
            Assert.AreEqual("Protected Sub New()", method.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var method = output.Items[0].Items[1].Items[1];
            Assert.IsNotNull(method);
            Assert.AreEqual("New(String())", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.New(String())", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.#ctor(System.String[])", method.Name);
            Assert.AreEqual("Public Sub New(x As String())", method.Syntax.Content[SyntaxLanguage.VB]);
        }
    }

    [TestProperty("Related", "Generic")]
    [TestMethod]
    public void TestGenerateMetadataWithField()
    {
        string code = @"
Namespace Test1
    Public Class Foo(Of T)
        Public X As Integer
        Protected Shared ReadOnly Y As Foo(Of T) = Nothing
        Protected Friend Const Z As String = """"
    End Class
    Public Enum Bar
        Black,
        Red,
        Blue = 2,
        Green = 4,
        White = Red Or Blue Or Green,
    End Enum
End Namespace
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        {
            var field = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(field);
            Assert.AreEqual("X", field.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo(Of T).X", field.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.X", field.Name);
            Assert.AreEqual("Public X As Integer", field.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var field = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(field);
            Assert.AreEqual("Y", field.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo(Of T).Y", field.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.Y", field.Name);
            Assert.AreEqual("Protected Shared ReadOnly Y As Foo(Of T)", field.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var field = output.Items[0].Items[0].Items[2];
            Assert.IsNotNull(field);
            Assert.AreEqual("Z", field.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo(Of T).Z", field.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.Z", field.Name);
            Assert.AreEqual(@"Protected Const Z As String = """"", field.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var field = output.Items[0].Items[1].Items[0];
            Assert.IsNotNull(field);
            Assert.AreEqual("Black", field.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.Black", field.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.Black", field.Name);
            Assert.AreEqual("Black = 0", field.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var field = output.Items[0].Items[1].Items[1];
            Assert.IsNotNull(field);
            Assert.AreEqual("Red", field.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.Red", field.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.Red", field.Name);
            Assert.AreEqual("Red = 1", field.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var field = output.Items[0].Items[1].Items[2];
            Assert.IsNotNull(field);
            Assert.AreEqual("Blue", field.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.Blue", field.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.Blue", field.Name);
            Assert.AreEqual("Blue = 2", field.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var field = output.Items[0].Items[1].Items[3];
            Assert.IsNotNull(field);
            Assert.AreEqual("Green", field.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.Green", field.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.Green", field.Name);
            Assert.AreEqual("Green = 4", field.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var field = output.Items[0].Items[1].Items[4];
            Assert.IsNotNull(field);
            Assert.AreEqual("White", field.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.White", field.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.White", field.Name);
            Assert.AreEqual("White = 7", field.Syntax.Content[SyntaxLanguage.VB]);
        }
    }

    [TestProperty("Related", "Generic")]
    [TestMethod]
    public void TestGenerateMetadataWithEvent()
    {
        string code = @"
Imports System
Namespace Test1
    Public MustInherit Class Foo(Of T As EventArgs)
        Implements IFooBar(Of T)
        Public Event A As EventHandler
        Protected Shared Custom Event B As EventHandler
            AddHandler(value As EventHandler)
            End AddHandler
            RemoveHandler(value As EventHandler)
            End RemoveHandler
            RaiseEvent(sender As Object, e As EventArgs)
            End RaiseEvent
        End Event
        Private Event C As EventHandler(Of T) Implements IFooBar(Of T).Bar
    End Class
    Public Interface IFooBar(Of TEventArgs As EventArgs)
        Event Bar As EventHandler(Of TEventArgs)
    End Interface
End Namespace
";
        MetadataItem output = Verify(code, new() { IncludePrivateMembers = true });
        Assert.ContainsSingle(output.Items);
        {
            var a = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(a);
            Assert.AreEqual("A", a.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo(Of T).A", a.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.A", a.Name);
            Assert.AreEqual("Public Event A As EventHandler", a.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var b = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(b);
            Assert.AreEqual("B", b.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo(Of T).B", b.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.B", b.Name);
            Assert.AreEqual("Protected Shared Event B As EventHandler", b.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var c = output.Items[0].Items[0].Items[2];
            Assert.IsNotNull(c);
            Assert.AreEqual("C", c.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo(Of T).C", c.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.C", c.Name);
            Assert.AreEqual("Event C As EventHandler(Of T)", c.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var a = output.Items[0].Items[1].Items[0];
            Assert.IsNotNull(a);
            Assert.AreEqual("Bar", a.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IFooBar(Of TEventArgs).Bar", a.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IFooBar`1.Bar", a.Name);
            Assert.AreEqual("Event Bar As EventHandler(Of TEventArgs)", a.Syntax.Content[SyntaxLanguage.VB]);
        }
    }

    [TestProperty("Related", "Generic")]
    [TestMethod]
    public void TestGenerateMetadataWithProperty()
    {
        string code = @"
Namespace Test1
    Public MustInherit Class Foo(Of T As Class)
        Public Property A As Integer
        Public Overridable ReadOnly Property B As Integer
            Get
                Return 1
            End Get
        End Property
        Public MustOverride WriteOnly Property C As Integer
        Protected Property D As Integer
            Get
                Return 1
            End Get
            Private Set(value As Integer)
            End Set
        End Property
        Public Property E As T
            Get
                Return Nothing
            End Get
            Protected Set(value As T)
            End Set
        End Property
        Protected Friend Shared Property F As Integer
            Get
                Return 1
            End Get
            Protected Set(value As Integer)
            End Set
        End Property
    End Class
    Public Class Bar
        Inherits Foo(Of String)
        Public Overridable Shadows Property A As Integer
        Public Overrides ReadOnly Property B As Integer
            Get
                Return 2
            End Get
        End Property
        Public Overrides WriteOnly Property C As Integer
            Set(value As Integer)
            End Set
        End Property
    End Class
    Public Interface IFooBar
        Property A As Integer
        ReadOnly Property B As Integer
        WriteOnly Property C As Integer
    End Interface
End Namespace
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        // Foo
        {
            var a = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(a);
            Assert.AreEqual("A", a.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo(Of T).A", a.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.A", a.Name);
            Assert.AreEqual("Public Property A As Integer", a.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var b = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(b);
            Assert.AreEqual("B", b.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo(Of T).B", b.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.B", b.Name);
            Assert.AreEqual("Public Overridable ReadOnly Property B As Integer", b.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var c = output.Items[0].Items[0].Items[2];
            Assert.IsNotNull(c);
            Assert.AreEqual("C", c.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo(Of T).C", c.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.C", c.Name);
            Assert.AreEqual("Public MustOverride WriteOnly Property C As Integer", c.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var d = output.Items[0].Items[0].Items[3];
            Assert.IsNotNull(d);
            Assert.AreEqual("D", d.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo(Of T).D", d.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.D", d.Name);
            Assert.AreEqual("Protected Property D As Integer", d.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var e = output.Items[0].Items[0].Items[4];
            Assert.IsNotNull(e);
            Assert.AreEqual("E", e.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo(Of T).E", e.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.E", e.Name);
            Assert.AreEqual("Public Property E As T", e.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var f = output.Items[0].Items[0].Items[5];
            Assert.IsNotNull(f);
            Assert.AreEqual("F", f.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo(Of T).F", f.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.F", f.Name);
            Assert.AreEqual("Protected Shared Property F As Integer", f.Syntax.Content[SyntaxLanguage.VB]);
        }
        // Bar
        {
            var a = output.Items[0].Items[1].Items[0];
            Assert.IsNotNull(a);
            Assert.AreEqual("A", a.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.A", a.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.A", a.Name);
            Assert.AreEqual("Public Overridable Property A As Integer", a.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var b = output.Items[0].Items[1].Items[1];
            Assert.IsNotNull(b);
            Assert.AreEqual("B", b.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.B", b.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.B", b.Name);
            Assert.AreEqual("Public Overrides ReadOnly Property B As Integer", b.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var c = output.Items[0].Items[1].Items[2];
            Assert.IsNotNull(c);
            Assert.AreEqual("C", c.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.C", c.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.C", c.Name);
            Assert.AreEqual("Public Overrides WriteOnly Property C As Integer", c.Syntax.Content[SyntaxLanguage.VB]);
        }
        // IFooBar
        {
            var a = output.Items[0].Items[2].Items[0];
            Assert.IsNotNull(a);
            Assert.AreEqual("A", a.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IFooBar.A", a.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IFooBar.A", a.Name);
            Assert.AreEqual("Property A As Integer", a.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var b = output.Items[0].Items[2].Items[1];
            Assert.IsNotNull(b);
            Assert.AreEqual("B", b.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IFooBar.B", b.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IFooBar.B", b.Name);
            Assert.AreEqual("ReadOnly Property B As Integer", b.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var c = output.Items[0].Items[2].Items[2];
            Assert.IsNotNull(c);
            Assert.AreEqual("C", c.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IFooBar.C", c.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IFooBar.C", c.Name);
            Assert.AreEqual("WriteOnly Property C As Integer", c.Syntax.Content[SyntaxLanguage.VB]);
        }
    }

    [TestProperty("Related", "Generic")]
    [TestMethod]
    public void TestGenerateMetadataWithIndex()
    {
        string code = @"
Imports System
Namespace Test1
    Public MustInherit Class Foo(Of T As Class)
        Public Property A(x As Integer) As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
            End Set
        End Property
        Public Overridable ReadOnly Property B(x As String) As Integer
            Get
                Return 1
            End Get
        End Property
        Public MustOverride WriteOnly Property C(x As Object) As Integer
        Protected Property D(x As Date) As Integer
            Get
                Return 1
            End Get
            Private Set(value As Integer)
            End Set
        End Property
        Public Property E(t As T) As Integer
            Get
                Return 0
            End Get
            Protected Set(value As Integer)
            End Set
        End Property
        Protected Friend Shared Property F(x As Integer, t As T) As Integer
            Get
                Return 1
            End Get
            Protected Set(value As Integer)
            End Set
        End Property
    End Class
    Public Class Bar
        Inherits Foo(Of String)
        Public Overridable Shadows Property A(x As Integer) As Integer
            Get
                Return 1
            End Get
            Set(value As Integer)
            End Set
        End Property
        Public Overrides ReadOnly Property B(x As String) As Integer
            Get
                Return 2
            End Get
        End Property
        Public Overrides WriteOnly Property C(x As Object) As Integer
            Set(value As Integer)
            End Set
        End Property
    End Class
    Public Interface IFooBar
        Property A(x As Integer) As Integer
        ReadOnly Property B(x As String) As Integer
        WriteOnly Property C(x As Object) As Integer
    End Interface
End Namespace
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        // Foo
        {
            var a = output.Items[0].Items[0].Items[0];
            Assert.IsNotNull(a);
            Assert.AreEqual("A(Integer)", a.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo(Of T).A(Integer)", a.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.A(System.Int32)", a.Name);
            Assert.AreEqual("Public Property A(x As Integer) As Integer", a.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var b = output.Items[0].Items[0].Items[1];
            Assert.IsNotNull(b);
            Assert.AreEqual("B(String)", b.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo(Of T).B(String)", b.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.B(System.String)", b.Name);
            Assert.AreEqual("Public Overridable ReadOnly Property B(x As String) As Integer", b.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var c = output.Items[0].Items[0].Items[2];
            Assert.IsNotNull(c);
            Assert.AreEqual("C(Object)", c.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo(Of T).C(Object)", c.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.C(System.Object)", c.Name);
            Assert.AreEqual("Public MustOverride WriteOnly Property C(x As Object) As Integer", c.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var d = output.Items[0].Items[0].Items[3];
            Assert.IsNotNull(d);
            Assert.AreEqual("D(Date)", d.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo(Of T).D(Date)", d.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.D(System.DateTime)", d.Name);
            Assert.AreEqual("Protected Property D(x As Date) As Integer", d.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var e = output.Items[0].Items[0].Items[4];
            Assert.IsNotNull(e);
            Assert.AreEqual("E(T)", e.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo(Of T).E(T)", e.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.E(`0)", e.Name);
            Assert.AreEqual("Public Property E(t As T) As Integer", e.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var f = output.Items[0].Items[0].Items[5];
            Assert.IsNotNull(f);
            Assert.AreEqual("F(Integer, T)", f.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo(Of T).F(Integer, T)", f.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Foo`1.F(System.Int32,`0)", f.Name);
            Assert.AreEqual("Protected Shared Property F(x As Integer, t As T) As Integer", f.Syntax.Content[SyntaxLanguage.VB]);
        }
        // Bar
        {
            var a = output.Items[0].Items[1].Items[0];
            Assert.IsNotNull(a);
            Assert.AreEqual("A(Integer)", a.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.A(Integer)", a.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.A(System.Int32)", a.Name);
            Assert.AreEqual("Public Overridable Property A(x As Integer) As Integer", a.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var b = output.Items[0].Items[1].Items[1];
            Assert.IsNotNull(b);
            Assert.AreEqual("B(String)", b.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.B(String)", b.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.B(System.String)", b.Name);
            Assert.AreEqual("Public Overrides ReadOnly Property B(x As String) As Integer", b.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var c = output.Items[0].Items[1].Items[2];
            Assert.IsNotNull(c);
            Assert.AreEqual("C(Object)", c.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.C(Object)", c.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.Bar.C(System.Object)", c.Name);
            Assert.AreEqual("Public Overrides WriteOnly Property C(x As Object) As Integer", c.Syntax.Content[SyntaxLanguage.VB]);
        }
        // IFooBar
        {
            var a = output.Items[0].Items[2].Items[0];
            Assert.IsNotNull(a);
            Assert.AreEqual("A(Integer)", a.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IFooBar.A(Integer)", a.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IFooBar.A(System.Int32)", a.Name);
            Assert.AreEqual("Property A(x As Integer) As Integer", a.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var b = output.Items[0].Items[2].Items[1];
            Assert.IsNotNull(b);
            Assert.AreEqual("B(String)", b.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IFooBar.B(String)", b.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IFooBar.B(System.String)", b.Name);
            Assert.AreEqual("ReadOnly Property B(x As String) As Integer", b.Syntax.Content[SyntaxLanguage.VB]);
        }
        {
            var c = output.Items[0].Items[2].Items[2];
            Assert.IsNotNull(c);
            Assert.AreEqual("C(Object)", c.DisplayNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IFooBar.C(Object)", c.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.AreEqual("Test1.IFooBar.C(System.Object)", c.Name);
            Assert.AreEqual("WriteOnly Property C(x As Object) As Integer", c.Syntax.Content[SyntaxLanguage.VB]);
        }
    }

    [TestProperty("Related", "Generic")]
    [TestProperty("Related", "Multilanguage")]
    [TestMethod]
    public void TestGenerateMetadataAsyncWithMultilanguage()
    {
        string code = @"
Namespace Test1
    Public Class Foo(Of T)
        Public Sub Bar(Of K)(i as Integer)
        End Sub
    End Class
End Namespace
";
        MetadataItem output = Verify(code);
        var type = output.Items[0].Items[0];
        Assert.IsNotNull(type);
        Assert.AreEqual("Foo<T>", type.DisplayNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Foo(Of T)", type.DisplayNames[SyntaxLanguage.VB]);
        Assert.AreEqual("Test1.Foo<T>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Test1.Foo(Of T)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
        Assert.AreEqual("Test1.Foo`1", type.Name);

        var method = output.Items[0].Items[0].Items[0];
        Assert.IsNotNull(method);
        Assert.AreEqual("Bar<K>(int)", method.DisplayNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Bar(Of K)(Integer)", method.DisplayNames[SyntaxLanguage.VB]);
        Assert.AreEqual("Test1.Foo<T>.Bar<K>(int)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("Test1.Foo(Of T).Bar(Of K)(Integer)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
        Assert.AreEqual("Test1.Foo`1.Bar``1(System.Int32)", method.Name);
        Assert.ContainsSingle(output.Items);
        var parameter = method.Syntax.Parameters[0];
        Assert.AreEqual("i", parameter.Name);
        Assert.AreEqual("System.Int32", parameter.Type);
        var returnValue = method.Syntax.Return;
        Assert.IsNull(returnValue);
    }

    [TestProperty("Related", "Attribute")]
    [TestMethod]
    public void TestGenerateMetadataWithAttribute()
    {
        string code = @"
Imports System
Imports System.ComponentModel

Namespace Test1
    <Serializable>
    <AttributeUsage(AttributeTargets.All, Inherited := true, AllowMultiple := true)>
    <TypeConverter(GetType(TestAttribute))>
    <Test(""test"")>
    <Test(New Integer(){1,2,3})>
    <Test(New Object(){Nothing, ""abc"", ""d""c, 1.1f, 1.2, CType(2, SByte), CType(3, Byte), 4s, 5us, 6, 8L, 9UL, New Integer(){ 10, 11, 12 }})>
    <Test(New Type(){GetType(Func(Of )), GetType(Func(Of ,)), GetType(Func(Of String, String))})>
    Public Class TestAttribute
        Inherits Attribute

        <Test(1)>
        <Test(2)>
        Public Sub New(o As Object)
        End Sub
    End Class
End Namespace
";
        MetadataItem output = Verify(code);
        Assert.ContainsSingle(output.Items);
        var type = output.Items[0].Items[0];
        Assert.IsNotNull(type);
        Assert.AreEqual(@"<Serializable>
<AttributeUsage(AttributeTargets.All, Inherited:=True, AllowMultiple:=True)>
<TypeConverter(GetType(TestAttribute))>
<Test(""test"")>
<Test(New Integer() { 1, 2, 3 })>
<Test(New Object() { Nothing, ""abc"", ""d""c, 1.1, 1.2, 2, 3, 4, 5, 6, 8, 9, New Integer() { 10, 11, 12 } })>
<Test(New Type() { GetType(Func(Of )), GetType(Func(Of ,)), GetType(Func(Of String, String)) })>
Public Class TestAttribute Inherits Attribute", type.Syntax.Content[SyntaxLanguage.VB]);
        var ctor = type.Items[0];
        Assert.IsNotNull(type);
        Assert.AreEqual(@"<Test(1)>
<Test(2)>
Public Sub New(o As Object)", ctor.Syntax.Content[SyntaxLanguage.VB]);
    }

    [TestMethod]
    [TestProperty("Related", "ValueTuple")]
    public void TestGenerateMetadataAsyncWithTupleParameter()
    {
        string code = @"
Namespace Test1
    Public Class Foo
        Public Sub Bar(tuple As (prefix As String, uri As String))
        End Sub
    End Class
End Namespace
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
        Assert.AreEqual("Public Sub Bar(tuple As (prefix As String, uri As String))", bar.Syntax.Content[SyntaxLanguage.VB]);
    }

    [TestMethod]
    [TestProperty("Related", "ValueTuple")]
    public void TestGenerateMetadataAsyncWithUnnamedTupleParameter()
    {
        string code = @"
Namespace Test1
    Public Class Foo
        Public Sub Bar(tuple As (String, String))
        End Sub
    End Class
End Namespace
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
        Assert.AreEqual("Public Sub Bar(tuple As (String, String))", bar.Syntax.Content[SyntaxLanguage.VB]);
    }

    [TestMethod]
    [TestProperty("Related", "ValueTuple")]
    public void TestGenerateMetadataAsyncWithPartiallyUnnamedTupleParameter()
    {
        string code = @"
Namespace Test1
    Public Class Foo
        Public Sub Bar(tuple As (String, uri As String))
        End Sub
    End Class
End Namespace
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
        Assert.AreEqual("Public Sub Bar(tuple As (String, uri As String))", bar.Syntax.Content[SyntaxLanguage.VB]);
    }

    [TestMethod]
    [TestProperty("Related", "ValueTuple")]
    public void TestGenerateMetadataAsyncWithTupleArrayParameter()
    {
        string code = @"
Namespace Test1
    Public Class Foo
        Public Sub Bar(tuples As (String, String)())
        End Sub
    End Class
End Namespace
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
        Assert.AreEqual("Public Sub Bar(tuples As (String, String)())", bar.Syntax.Content[SyntaxLanguage.VB]);
    }

    [TestMethod]
    [TestProperty("Related", "ValueTuple")]
    public void TestGenerateMetadataAsyncWithTupleEnumerableParameter()
    {
        string code = @"
Imports System.Collections.Generic

Namespace Test1
    Public Class Foo
        Public Sub Bar(tuples As IEnumerable(Of (prefix As String, uri As String)))
        End Sub
    End Class
End Namespace
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
        Assert.AreEqual("Public Sub Bar(tuples As IEnumerable(Of (prefix As String, uri As String)))", bar.Syntax.Content[SyntaxLanguage.VB]);
    }

    [TestMethod]
    [TestProperty("Related", "ValueTuple")]
    public void TestGenerateMetadataAsyncWithTupleResult()
    {
        string code = @"
Namespace Test1
    Public Class Foo
        Public Function Bar As (prefix As String, uri As String)
            Return (string.Empty, string.Empty)
        End Function
    End Class
End Namespace
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
        Assert.AreEqual("Public Function Bar() As (prefix As String, uri As String)", bar.Syntax.Content[SyntaxLanguage.VB]);
    }

    [TestMethod]
    [TestProperty("Related", "ValueTuple")]
    public void TestGenerateMetadataAsyncWithUnnamedTupleResult()
    {
        string code = @"
Namespace Test1
    Public Class Foo
        Public Function Bar As (String, String)
            Return (string.Empty, string.Empty)
        End Function
    End Class
End Namespace
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
        Assert.AreEqual("Public Function Bar() As (String, String)", bar.Syntax.Content[SyntaxLanguage.VB]);
    }

    [TestMethod]
    [TestProperty("Related", "ValueTuple")]
    public void TestGenerateMetadataAsyncWithPartiallyUnnamedTupleResult()
    {
        string code = @"
Namespace Test1
    Public Class Foo
        Public Function Bar As (String, uri As String)
            Return (string.Empty, string.Empty)
        End Function
    End Class
End Namespace
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
        Assert.AreEqual("Public Function Bar() As (String, uri As String)", bar.Syntax.Content[SyntaxLanguage.VB]);
    }

    [TestMethod]
    [TestProperty("Related", "ValueTuple")]
    public void TestGenerateMetadataAsyncWithEnumerableTupleResult()
    {
        string code = @"
Imports System.Collections.Generic

Namespace Test1
    Public Class Foo
        Public Function Bar As IEnumerable(Of (prefix As String, uri As String))
            Return Null
        End Function
    End Class
End Namespace
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
        Assert.AreEqual("Public Function Bar() As IEnumerable(Of (prefix As String, uri As String))", bar.Syntax.Content[SyntaxLanguage.VB]);
    }

    [TestMethod]
    public void TestDefineConstantsMSBuildProperty()
    {
        var code =
            """
            Namespace Test
                Public Class Foo
            #if TEST
                    Public Sub F1
            #endif
                    End Function
                End Class
            End Namespace
            """;

        // Test with DefineConstants
        {
            var output = Verify(code, msbuildProperties: new Dictionary<string, string> { ["DefineConstants"] = "TEST=DUMMYVALUE;DUMMY=DUMMYVALUE" });
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
Namespace Test
    Public Class Foo
      Property Tasks As TupleLibrary.XmlTasks
    End Class
End Namespace
namespace Test
{
    public class Foo
    {
        public TupleLibrary.XmlTasks Tasks { get;set; }
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
