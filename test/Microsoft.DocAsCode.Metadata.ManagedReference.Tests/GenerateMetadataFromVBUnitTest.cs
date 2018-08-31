// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Xunit;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.VisualBasic;
    using Microsoft.CodeAnalysis.Emit;
    using Microsoft.CodeAnalysis.MSBuild;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    using static Microsoft.DocAsCode.Metadata.ManagedReference.RoslynIntermediateMetadataExtractor;

    [Trait("Owner", "vwxyzh")]
    [Trait("Language", "VB")]
    [Trait("EntityType", "Model")]
    [Collection("docfx STA")]
    public class GenerateMetadataFromVBUnitTest
    {
        private static readonly MSBuildWorkspace Workspace = MSBuildWorkspace.Create();

        [Trait("Related", "Generic")]
        [Fact]
        public void TestGenereateMetadataWithClass()
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromVBCode(code));
            Assert.Equal(1, output.Items.Count);
            {
                var type = output.Items[0].Items[0];
                Assert.NotNull(type);
                Assert.Equal("Class1", type.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Class1", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Class1", type.Name);
                Assert.Equal(@"Public Class Class1", type.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Class" }, type.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var type = output.Items[0].Items[1];
                Assert.NotNull(type);
                Assert.Equal("Class2(Of T)", type.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Class2(Of T)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Class2`1", type.Name);
                Assert.Equal(@"Public Class Class2(Of T)
    Inherits List(Of T)
    Implements IList(Of T), ICollection(Of T), IList, ICollection, IReadOnlyList(Of T), IReadOnlyCollection(Of T), IEnumerable(Of T), IEnumerable", type.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Class" }, type.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var type = output.Items[0].Items[2];
                Assert.NotNull(type);
                Assert.Equal("Class3(Of T1, T2)", type.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Class3(Of T1, T2)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Class3`2", type.Name);
                Assert.Equal(@"Public Class Class3(Of T1, T2 As T1)", type.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Class" }, type.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var type = output.Items[0].Items[3];
                Assert.NotNull(type);
                Assert.Equal("Class4(Of T1, T2)", type.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Class4(Of T1, T2)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Class4`2", type.Name);
                Assert.Equal(@"Public Class Class4(Of T1 As {Structure, IEnumerable(Of T2)}, T2 As {Class, New})", type.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Class" }, type.Modifiers[SyntaxLanguage.VB]);
            }
        }

        [Fact]
        public void TestGenereateMetadataWithEnum()
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromVBCode(code));
            Assert.Equal(1, output.Items.Count);
            {
                var type = output.Items[0].Items[0];
                Assert.NotNull(type);
                Assert.Equal("Enum1", type.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Enum1", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Enum1", type.Name);
                Assert.Equal(@"Public Enum Enum1", type.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Enum" }, type.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var type = output.Items[0].Items[1];
                Assert.NotNull(type);
                Assert.Equal("Enum2", type.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Enum2", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Enum2", type.Name);
                Assert.Equal(@"Public Enum Enum2 As Byte", type.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Enum" }, type.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var type = output.Items[0].Items[2];
                Assert.NotNull(type);
                Assert.Equal("Enum3", type.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Enum3", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Enum3", type.Name);
                Assert.Equal(@"Public Enum Enum3", type.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Enum" }, type.Modifiers[SyntaxLanguage.VB]);
            }
        }

        [Trait("Related", "Generic")]
        [Fact]
        public void TestGenereateMetadataWithInterface()
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromVBCode(code));
            Assert.Equal(1, output.Items.Count);
            {
                var type = output.Items[0].Items[0];
                Assert.NotNull(type);
                Assert.Equal("IA", type.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IA", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IA", type.Name);
                Assert.Equal(@"Public Interface IA", type.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Interface" }, type.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var type = output.Items[0].Items[1];
                Assert.NotNull(type);
                Assert.Equal("IB(Of T)", type.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IB(Of T)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IB`1", type.Name);
                Assert.Equal(@"Public Interface IB(Of T As Class)", type.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Interface" }, type.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var type = output.Items[0].Items[2];
                Assert.NotNull(type);
                Assert.Equal("IC(Of TItem)", type.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IC(Of TItem)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IC`1", type.Name);
                Assert.Equal(@"Public Interface IC(Of TItem As {IA, New})
    Inherits IA, IB(Of TItem())", type.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Interface" }, type.Modifiers[SyntaxLanguage.VB]);
            }
        }

        [Trait("Related", "Generic")]
        [Fact]
        public void TestGenereateMetadataWithStructure()
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromVBCode(code));
            Assert.Equal(1, output.Items.Count);
            {
                var type = output.Items[0].Items[0];
                Assert.NotNull(type);
                Assert.Equal("S1", type.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.S1", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.S1", type.Name);
                Assert.Equal(@"Public Structure S1", type.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Structure" }, type.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var type = output.Items[0].Items[1];
                Assert.NotNull(type);
                Assert.Equal("S2(Of T)", type.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.S2(Of T)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.S2`1", type.Name);
                Assert.Equal(@"Public Structure S2(Of T As Class)", type.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Structure" }, type.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var type = output.Items[0].Items[2];
                Assert.NotNull(type);
                Assert.Equal("S3(Of T1, T2)", type.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.S3(Of T1, T2)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.S3`2", type.Name);
                Assert.Equal(@"Public Structure S3(Of T1 As {Class, IA, New}, T2 As IB(Of T1))
    Implements IA, IB(Of T1())", type.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Structure" }, type.Modifiers[SyntaxLanguage.VB]);
            }
        }

        [Trait("Related", "Generic")]
        [Fact]
        public void TestGenereateMetadataWithDelegate()
        {
            string code = @"
Namespace Test1
    Public Delegate Sub D1
    Public Delegate Sub D2(Of T As Class)(x() as integer)
    Public Delegate Function D3(Of T1 As Class, T2 As {T1, New})(ByRef x As T1) As T2
End Namespace
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromVBCode(code));
            Assert.Equal(1, output.Items.Count);
            {
                var type = output.Items[0].Items[0];
                Assert.NotNull(type);
                Assert.Equal("D1", type.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.D1", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.D1", type.Name);
                Assert.Equal(@"Public Delegate Sub D1", type.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Delegate" }, type.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var type = output.Items[0].Items[1];
                Assert.NotNull(type);
                Assert.Equal("D2(Of T)", type.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.D2(Of T)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.D2`1", type.Name);
                Assert.Equal(@"Public Delegate Sub D2(Of T As Class)(x As Integer())", type.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Delegate" }, type.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var type = output.Items[0].Items[2];
                Assert.NotNull(type);
                Assert.Equal("D3(Of T1, T2)", type.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.D3(Of T1, T2)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.D3`2", type.Name);
                Assert.Equal(@"Public Delegate Function D3(Of T1 As Class, T2 As {T1, New})(ByRef x As T1) As T2", type.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Delegate" }, type.Modifiers[SyntaxLanguage.VB]);
            }
        }

        [Fact]
        public void TestGenereateMetadataWithModule()
        {
            string code = @"
Namespace Test1
    Public Module M1
    End Module
End Namespace
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromVBCode(code));
            Assert.Equal(1, output.Items.Count);
            {
                var type = output.Items[0].Items[0];
                Assert.NotNull(type);
                Assert.Equal("M1", type.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.M1", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.M1", type.Name);
                Assert.Equal(@"Public Module M1", type.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Module" }, type.Modifiers[SyntaxLanguage.VB]);
            }
        }

        [Trait("Related", "Generic")]
        [Trait("Related", "Inheritance")]
        [Fact]
        public void TestGenereateMetadataWithMethod()
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromVBCode(code));
            Assert.Equal(1, output.Items.Count);
            // Foo<T>
            {
                var method = output.Items[0].Items[0].Items[0];
                Assert.NotNull(method);
                Assert.Equal("M1(Int32, Int32())", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo(Of T).M1(System.Int32, System.Int32())", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.M1(System.Int32,System.Int32[])", method.Name);
                Assert.Equal("Public Overridable Sub M1(x As Integer, ParamArray y As Integer())", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Overridable" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[1];
                Assert.NotNull(method);
                Assert.Equal("M2(Of T1, T2)(T1, ByRef T2())", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo(Of T).M2(Of T1, T2)(T1, ByRef T2())", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.M2``2(``0,``1[]@)", method.Name);
                Assert.Equal("Public MustOverride Sub M2(Of T1 As Class, T2 As Foo(Of T1))(x As T1, ByRef y As T2())", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "MustOverride" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[2];
                Assert.NotNull(method);
                Assert.Equal("M3()", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo(Of T).M3()", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.M3", method.Name);
                Assert.Equal("Public Sub M3", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[3];
                Assert.NotNull(method);
                Assert.Equal("M4(Of T1)(T)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo(Of T).M4(Of T1)(T)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.M4``1(`0)", method.Name);
                Assert.Equal("Protected Shared Function M4(Of T1 As Class)(x As T) As T1", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Protected", "Shared" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            // Bar
            {
                var method = output.Items[0].Items[1].Items[0];
                Assert.NotNull(method);
                Assert.Equal("M1(Int32, Int32())", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.M1(System.Int32, System.Int32())", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.M1(System.Int32,System.Int32[])", method.Name);
                Assert.Equal("Public Overrides Sub M1(x As Integer, y As Integer())", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Overrides" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[1].Items[1];
                Assert.NotNull(method);
                Assert.Equal("M2(Of T1, T2)(T1, ByRef T2())", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.M2(Of T1, T2)(T1, ByRef T2())", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.M2``2(``0,``1[]@)", method.Name);
                Assert.Equal("Public NotOverridable Overrides Sub M2(Of T1 As Class, T2 As Foo(Of T1))(x As T1, ByRef y As T2())", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Overrides", "NotOverridable" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[1].Items[2];
                Assert.NotNull(method);
                Assert.Equal("M3()", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.M3()", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.M3", method.Name);
                Assert.Equal("Public Sub M3", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            // IFooBar
            {
                var method = output.Items[0].Items[2].Items[0];
                Assert.NotNull(method);
                Assert.Equal("M1(Int32, Int32())", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IFooBar.M1(System.Int32, System.Int32())", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IFooBar.M1(System.Int32,System.Int32[])", method.Name);
                Assert.Equal("Sub M1(x As Integer, ParamArray y As Integer())", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new string[0], method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[2].Items[1];
                Assert.NotNull(method);
                Assert.Equal("M2(Of T1, T2)(T1, ByRef T2())", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IFooBar.M2(Of T1, T2)(T1, ByRef T2())", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IFooBar.M2``2(``0,``1[]@)", method.Name);
                Assert.Equal("Sub M2(Of T1 As Class, T2 As Foo(Of T1))(x As T1, ByRef y As T2())", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new string[0], method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[2].Items[2];
                Assert.NotNull(method);
                Assert.Equal("M3()", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IFooBar.M3()", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IFooBar.M3", method.Name);
                Assert.Equal("Sub M3", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new string[0], method.Modifiers[SyntaxLanguage.VB]);
            }
            // inheritance of Foo<T>
            {
                var inheritedMembers = output.Items[0].Items[0].InheritedMembers;
                Assert.NotNull(inheritedMembers);
                Assert.Equal(
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
                Assert.NotNull(inheritedMembers);
                Assert.Equal(
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

        [Fact]
        public void TestGenereateMetadataWithOperator()
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromVBCode(code));
            Assert.Equal(1, output.Items.Count);
            // unary
            {
                var method = output.Items[0].Items[0].Items[0];
                Assert.NotNull(method);
                Assert.Equal("UnaryPlus(Foo)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.UnaryPlus(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.op_UnaryPlus(Test1.Foo)", method.Name);
                Assert.Equal(@"Public Shared Operator +(x As Foo) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Shared" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[1];
                Assert.NotNull(method);
                Assert.Equal("UnaryNegation(Foo)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.UnaryNegation(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.op_UnaryNegation(Test1.Foo)", method.Name);
                Assert.Equal(@"Public Shared Operator -(x As Foo) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Shared" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[2];
                Assert.NotNull(method);
                Assert.Equal("OnesComplement(Foo)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.OnesComplement(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.op_OnesComplement(Test1.Foo)", method.Name);
                Assert.Equal(@"Public Shared Operator Not(x As Foo) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Shared" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[3];
                Assert.NotNull(method);
                Assert.Equal("True(Foo)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.True(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.op_True(Test1.Foo)", method.Name);
                Assert.Equal(@"Public Shared Operator IsTrue(x As Foo) As Boolean", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Shared" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[4];
                Assert.NotNull(method);
                Assert.Equal("False(Foo)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.False(Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.op_False(Test1.Foo)", method.Name);
                Assert.Equal(@"Public Shared Operator IsFalse(x As Foo) As Boolean", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Shared" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            // binary
            {
                var method = output.Items[0].Items[0].Items[5];
                Assert.NotNull(method);
                Assert.Equal("Addition(Foo, Foo)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.Addition(Test1.Foo, Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.op_Addition(Test1.Foo,Test1.Foo)", method.Name);
                Assert.Equal(@"Public Shared Operator +(x As Foo, y As Foo) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Shared" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[6];
                Assert.NotNull(method);
                Assert.Equal("Subtraction(Foo, Foo)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.Subtraction(Test1.Foo, Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.op_Subtraction(Test1.Foo,Test1.Foo)", method.Name);
                Assert.Equal(@"Public Shared Operator -(x As Foo, y As Foo) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Shared" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[7];
                Assert.NotNull(method);
                Assert.Equal("Multiply(Foo, Foo)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.Multiply(Test1.Foo, Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.op_Multiply(Test1.Foo,Test1.Foo)", method.Name);
                Assert.Equal(@"Public Shared Operator *(x As Foo, y As Foo) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Shared" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[8];
                Assert.NotNull(method);
                Assert.Equal("Division(Foo, Foo)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.Division(Test1.Foo, Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.op_Division(Test1.Foo,Test1.Foo)", method.Name);
                Assert.Equal(@"Public Shared Operator /(x As Foo, y As Foo) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Shared" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[9];
                Assert.NotNull(method);
                Assert.Equal("Modulus(Foo, Foo)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.Modulus(Test1.Foo, Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.op_Modulus(Test1.Foo,Test1.Foo)", method.Name);
                Assert.Equal(@"Public Shared Operator Mod(x As Foo, y As Foo) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Shared" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[10];
                Assert.NotNull(method);
                Assert.Equal("BitwiseAnd(Foo, Foo)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.BitwiseAnd(Test1.Foo, Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.op_BitwiseAnd(Test1.Foo,Test1.Foo)", method.Name);
                Assert.Equal(@"Public Shared Operator And(x As Foo, y As Foo) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Shared" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[11];
                Assert.NotNull(method);
                Assert.Equal("BitwiseOr(Foo, Foo)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.BitwiseOr(Test1.Foo, Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.op_BitwiseOr(Test1.Foo,Test1.Foo)", method.Name);
                Assert.Equal(@"Public Shared Operator Or(x As Foo, y As Foo) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Shared" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[12];
                Assert.NotNull(method);
                Assert.Equal("ExclusiveOr(Foo, Foo)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.ExclusiveOr(Test1.Foo, Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.op_ExclusiveOr(Test1.Foo,Test1.Foo)", method.Name);
                Assert.Equal(@"Public Shared Operator Xor(x As Foo, y As Foo) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Shared" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[13];
                Assert.NotNull(method);
                Assert.Equal("RightShift(Foo, Int32)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.RightShift(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.op_RightShift(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"Public Shared Operator >>(x As Foo, y As Integer) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Shared" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[14];
                Assert.NotNull(method);
                Assert.Equal("LeftShift(Foo, Int32)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.LeftShift(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.op_LeftShift(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"Public Shared Operator <<(x As Foo, y As Integer) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Shared" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            // comparison
            {
                var method = output.Items[0].Items[0].Items[15];
                Assert.NotNull(method);
                Assert.Equal("Equality(Foo, Int32)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.Equality(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.op_Equality(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"Public Shared Operator =(x As Foo, y As Integer) As Boolean", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Shared" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[16];
                Assert.NotNull(method);
                Assert.Equal("Inequality(Foo, Int32)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.Inequality(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.op_Inequality(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"Public Shared Operator <>(x As Foo, y As Integer) As Boolean", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Shared" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[17];
                Assert.NotNull(method);
                Assert.Equal("GreaterThan(Foo, Int32)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.GreaterThan(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.op_GreaterThan(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"Public Shared Operator>(x As Foo, y As Integer) As Boolean", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Shared" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[18];
                Assert.NotNull(method);
                Assert.Equal("LessThan(Foo, Int32)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.LessThan(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.op_LessThan(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"Public Shared Operator <(x As Foo, y As Integer) As Boolean", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Shared" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[19];
                Assert.NotNull(method);
                Assert.Equal("GreaterThanOrEqual(Foo, Int32)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.GreaterThanOrEqual(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.op_GreaterThanOrEqual(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"Public Shared Operator >=(x As Foo, y As Integer) As Boolean", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Shared" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[20];
                Assert.NotNull(method);
                Assert.Equal("LessThanOrEqual(Foo, Int32)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.LessThanOrEqual(Test1.Foo, System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.op_LessThanOrEqual(Test1.Foo,System.Int32)", method.Name);
                Assert.Equal(@"Public Shared Operator <=(x As Foo, y As Integer) As Boolean", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Shared" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            // conversion
            {
                var method = output.Items[0].Items[0].Items[21];
                Assert.NotNull(method);
                Assert.Equal("Widening(Int32 to Foo)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.Widening(System.Int32 to Test1.Foo)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.op_Implicit(System.Int32)~Test1.Foo", method.Name);
                Assert.Equal(@"Public Shared Widening Operator CType(x As Integer) As Foo", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Shared" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[0].Items[22];
                Assert.NotNull(method);
                Assert.Equal("Narrowing(Foo to Int32)", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.Narrowing(Test1.Foo to System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo.op_Explicit(Test1.Foo)~System.Int32", method.Name);
                Assert.Equal(@"Public Shared Narrowing Operator CType(x As Foo) As Integer", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Shared" }, method.Modifiers[SyntaxLanguage.VB]);
            }
        }

        [Trait("Related", "Generic")]
        [Fact]
        public void TestGenereateMetadataWithConstructor()
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromVBCode(code));
            Assert.Equal(1, output.Items.Count);
            // Foo<T>
            {
                var method = output.Items[0].Items[0].Items[0];
                Assert.NotNull(method);
                Assert.Equal("Foo(T())", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo(Of T).Foo(T())", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.#ctor(`0[])", method.Name);
                Assert.Equal("Protected Sub New(x As T())", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Protected" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            // Bar
            {
                var method = output.Items[0].Items[1].Items[0];
                Assert.NotNull(method);
                Assert.Equal("Bar()", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.Bar()", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.#ctor", method.Name);
                Assert.Equal("Protected Sub New", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Protected" }, method.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var method = output.Items[0].Items[1].Items[1];
                Assert.NotNull(method);
                Assert.Equal("Bar(String())", method.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.Bar(System.String())", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.#ctor(System.String[])", method.Name);
                Assert.Equal("Public Sub New(x As String())", method.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public" }, method.Modifiers[SyntaxLanguage.VB]);
            }
        }

        [Trait("Related", "Generic")]
        [Fact]
        public void TestGenereateMetadataWithField()
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromVBCode(code));
            Assert.Equal(1, output.Items.Count);
            {
                var field = output.Items[0].Items[0].Items[0];
                Assert.NotNull(field);
                Assert.Equal("X", field.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo(Of T).X", field.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.X", field.Name);
                Assert.Equal("Public X As Integer", field.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public" }, field.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var field = output.Items[0].Items[0].Items[1];
                Assert.NotNull(field);
                Assert.Equal("Y", field.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo(Of T).Y", field.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.Y", field.Name);
                Assert.Equal("Protected Shared ReadOnly Y As Foo(Of T)", field.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Protected", "Shared", "ReadOnly" }, field.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var field = output.Items[0].Items[0].Items[2];
                Assert.NotNull(field);
                Assert.Equal("Z", field.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo(Of T).Z", field.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.Z", field.Name);
                Assert.Equal(@"Protected Const Z As String = """"", field.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Protected", "Const" }, field.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var field = output.Items[0].Items[1].Items[0];
                Assert.NotNull(field);
                Assert.Equal("Black", field.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.Black", field.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.Black", field.Name);
                Assert.Equal("Black = 0", field.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Const" }, field.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var field = output.Items[0].Items[1].Items[1];
                Assert.NotNull(field);
                Assert.Equal("Red", field.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.Red", field.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.Red", field.Name);
                Assert.Equal("Red = 1", field.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Const" }, field.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var field = output.Items[0].Items[1].Items[2];
                Assert.NotNull(field);
                Assert.Equal("Blue", field.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.Blue", field.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.Blue", field.Name);
                Assert.Equal(@"Blue = 2", field.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Const" }, field.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var field = output.Items[0].Items[1].Items[3];
                Assert.NotNull(field);
                Assert.Equal("Green", field.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.Green", field.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.Green", field.Name);
                Assert.Equal("Green = 4", field.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Const" }, field.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var field = output.Items[0].Items[1].Items[4];
                Assert.NotNull(field);
                Assert.Equal("White", field.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.White", field.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.White", field.Name);
                Assert.Equal(@"White = 7", field.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Const" }, field.Modifiers[SyntaxLanguage.VB]);
            }
        }

        [Trait("Related", "Generic")]
        [Fact]
        public void TestGenereateMetadataWithEvent()
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromVBCode(code));
            Assert.Equal(1, output.Items.Count);
            {
                var a = output.Items[0].Items[0].Items[0];
                Assert.NotNull(a);
                Assert.Equal("A", a.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo(Of T).A", a.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.A", a.Name);
                Assert.Equal("Public Event A As EventHandler", a.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public" }, a.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var b = output.Items[0].Items[0].Items[1];
                Assert.NotNull(b);
                Assert.Equal("B", b.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo(Of T).B", b.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.B", b.Name);
                Assert.Equal("Protected Shared Event B As EventHandler", b.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Protected", "Shared" }, b.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var c = output.Items[0].Items[0].Items[2];
                Assert.NotNull(c);
                Assert.Equal("C", c.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo(Of T).C", c.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.C", c.Name);
                Assert.Equal("Event C As EventHandler(Of T) Implements IFooBar(Of T).Bar", c.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new string[0], c.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var a = output.Items[0].Items[1].Items[0];
                Assert.NotNull(a);
                Assert.Equal("Bar", a.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IFooBar(Of TEventArgs).Bar", a.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IFooBar`1.Bar", a.Name);
                Assert.Equal("Event Bar As EventHandler(Of TEventArgs)", a.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new string[0], a.Modifiers[SyntaxLanguage.VB]);
            }
        }

        [Trait("Related", "Generic")]
        [Fact]
        public void TestGenereateMetadataWithProperty()
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromVBCode(code));
            Assert.Equal(1, output.Items.Count);
            // Foo
            {
                var a = output.Items[0].Items[0].Items[0];
                Assert.NotNull(a);
                Assert.Equal("A", a.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo(Of T).A", a.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.A", a.Name);
                Assert.Equal("Public Property A As Integer", a.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public" }, a.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var b = output.Items[0].Items[0].Items[1];
                Assert.NotNull(b);
                Assert.Equal("B", b.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo(Of T).B", b.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.B", b.Name);
                Assert.Equal("Public Overridable ReadOnly Property B As Integer", b.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Overridable", "ReadOnly" }, b.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var c = output.Items[0].Items[0].Items[2];
                Assert.NotNull(c);
                Assert.Equal("C", c.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo(Of T).C", c.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.C", c.Name);
                Assert.Equal("Public MustOverride WriteOnly Property C As Integer", c.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "MustOverride", "WriteOnly" }, c.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var d = output.Items[0].Items[0].Items[3];
                Assert.NotNull(d);
                Assert.Equal("D", d.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo(Of T).D", d.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.D", d.Name);
                Assert.Equal(@"Protected ReadOnly Property D As Integer", d.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Protected", "ReadOnly" }, d.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var e = output.Items[0].Items[0].Items[4];
                Assert.NotNull(e);
                Assert.Equal("E", e.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo(Of T).E", e.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.E", e.Name);
                Assert.Equal(@"Public Property E As T", e.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Get", "Protected Set" }, e.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var f = output.Items[0].Items[0].Items[5];
                Assert.NotNull(f);
                Assert.Equal("F", f.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo(Of T).F", f.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.F", f.Name);
                Assert.Equal(@"Protected Shared Property F As Integer", f.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Protected", "Shared" }, f.Modifiers[SyntaxLanguage.VB]);
            }
            // Bar
            {
                var a = output.Items[0].Items[1].Items[0];
                Assert.NotNull(a);
                Assert.Equal("A", a.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.A", a.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.A", a.Name);
                Assert.Equal("Public Overridable Property A As Integer", a.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Overridable" }, a.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var b = output.Items[0].Items[1].Items[1];
                Assert.NotNull(b);
                Assert.Equal("B", b.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.B", b.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.B", b.Name);
                Assert.Equal("Public Overrides ReadOnly Property B As Integer", b.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Overrides", "ReadOnly" }, b.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var c = output.Items[0].Items[1].Items[2];
                Assert.NotNull(c);
                Assert.Equal("C", c.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.C", c.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.C", c.Name);
                Assert.Equal("Public Overrides WriteOnly Property C As Integer", c.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Overrides", "WriteOnly" }, c.Modifiers[SyntaxLanguage.VB]);
            }
            // IFooBar
            {
                var a = output.Items[0].Items[2].Items[0];
                Assert.NotNull(a);
                Assert.Equal("A", a.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IFooBar.A", a.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IFooBar.A", a.Name);
                Assert.Equal("Property A As Integer", a.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new string[0], a.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var b = output.Items[0].Items[2].Items[1];
                Assert.NotNull(b);
                Assert.Equal("B", b.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IFooBar.B", b.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IFooBar.B", b.Name);
                Assert.Equal("ReadOnly Property B As Integer", b.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "ReadOnly" }, b.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var c = output.Items[0].Items[2].Items[2];
                Assert.NotNull(c);
                Assert.Equal("C", c.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IFooBar.C", c.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IFooBar.C", c.Name);
                Assert.Equal("WriteOnly Property C As Integer", c.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "WriteOnly" }, c.Modifiers[SyntaxLanguage.VB]);
            }
        }

        [Trait("Related", "Generic")]
        [Fact]
        public void TestGenereateMetadataWithIndex()
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromVBCode(code));
            Assert.Equal(1, output.Items.Count);
            // Foo
            {
                var a = output.Items[0].Items[0].Items[0];
                Assert.NotNull(a);
                Assert.Equal("A(Int32)", a.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo(Of T).A(System.Int32)", a.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.A(System.Int32)", a.Name);
                Assert.Equal("Public Property A(x As Integer) As Integer", a.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public" }, a.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var b = output.Items[0].Items[0].Items[1];
                Assert.NotNull(b);
                Assert.Equal("B(String)", b.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo(Of T).B(System.String)", b.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.B(System.String)", b.Name);
                Assert.Equal("Public Overridable ReadOnly Property B(x As String) As Integer", b.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Overridable", "ReadOnly" }, b.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var c = output.Items[0].Items[0].Items[2];
                Assert.NotNull(c);
                Assert.Equal("C(Object)", c.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo(Of T).C(System.Object)", c.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.C(System.Object)", c.Name);
                Assert.Equal("Public MustOverride WriteOnly Property C(x As Object) As Integer", c.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "MustOverride", "WriteOnly" }, c.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var d = output.Items[0].Items[0].Items[3];
                Assert.NotNull(d);
                Assert.Equal("D(DateTime)", d.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo(Of T).D(System.DateTime)", d.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.D(System.DateTime)", d.Name);
                Assert.Equal(@"Protected ReadOnly Property D(x As Date) As Integer", d.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Protected", "ReadOnly" }, d.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var e = output.Items[0].Items[0].Items[4];
                Assert.NotNull(e);
                Assert.Equal("E(T)", e.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo(Of T).E(T)", e.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.E(`0)", e.Name);
                Assert.Equal(@"Public Property E(t As T) As Integer", e.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Get", "Protected Set" }, e.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var f = output.Items[0].Items[0].Items[5];
                Assert.NotNull(f);
                Assert.Equal("F(Int32, T)", f.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo(Of T).F(System.Int32, T)", f.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Foo`1.F(System.Int32,`0)", f.Name);
                Assert.Equal(@"Protected Shared Property F(x As Integer, t As T) As Integer", f.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Protected", "Shared" }, f.Modifiers[SyntaxLanguage.VB]);
            }
            // Bar
            {
                var a = output.Items[0].Items[1].Items[0];
                Assert.NotNull(a);
                Assert.Equal("A(Int32)", a.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.A(System.Int32)", a.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.A(System.Int32)", a.Name);
                Assert.Equal("Public Overridable Property A(x As Integer) As Integer", a.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Overridable" }, a.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var b = output.Items[0].Items[1].Items[1];
                Assert.NotNull(b);
                Assert.Equal("B(String)", b.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.B(System.String)", b.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.B(System.String)", b.Name);
                Assert.Equal("Public Overrides ReadOnly Property B(x As String) As Integer", b.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Overrides", "ReadOnly" }, b.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var c = output.Items[0].Items[1].Items[2];
                Assert.NotNull(c);
                Assert.Equal("C(Object)", c.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.C(System.Object)", c.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.Bar.C(System.Object)", c.Name);
                Assert.Equal("Public Overrides WriteOnly Property C(x As Object) As Integer", c.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "Public", "Overrides", "WriteOnly" }, c.Modifiers[SyntaxLanguage.VB]);
            }
            // IFooBar
            {
                var a = output.Items[0].Items[2].Items[0];
                Assert.NotNull(a);
                Assert.Equal("A(Int32)", a.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IFooBar.A(System.Int32)", a.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IFooBar.A(System.Int32)", a.Name);
                Assert.Equal("Property A(x As Integer) As Integer", a.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new string[0], a.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var b = output.Items[0].Items[2].Items[1];
                Assert.NotNull(b);
                Assert.Equal("B(String)", b.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IFooBar.B(System.String)", b.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IFooBar.B(System.String)", b.Name);
                Assert.Equal("ReadOnly Property B(x As String) As Integer", b.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "ReadOnly" }, b.Modifiers[SyntaxLanguage.VB]);
            }
            {
                var c = output.Items[0].Items[2].Items[2];
                Assert.NotNull(c);
                Assert.Equal("C(Object)", c.DisplayNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IFooBar.C(System.Object)", c.DisplayQualifiedNames[SyntaxLanguage.VB]);
                Assert.Equal("Test1.IFooBar.C(System.Object)", c.Name);
                Assert.Equal("WriteOnly Property C(x As Object) As Integer", c.Syntax.Content[SyntaxLanguage.VB]);
                Assert.Equal(new[] { "WriteOnly" }, c.Modifiers[SyntaxLanguage.VB]);
            }
        }

        [Trait("Related", "Generic")]
        [Trait("Related", "Multilanguage")]
        [Fact]
        public void TestGenereateMetadataAsyncWithMultilanguage()
        {
            string code = @"
Namespace Test1
    Public Class Foo(Of T)
        Public Sub Bar(Of K)(i as Integer)
        End Sub
    End Class
End Namespace
";
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromVBCode(code));
            var type = output.Items[0].Items[0];
            Assert.NotNull(type);
            Assert.Equal("Foo<T>", type.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.Equal("Foo(Of T)", type.DisplayNames[SyntaxLanguage.VB]);
            Assert.Equal("Test1.Foo<T>", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.Equal("Test1.Foo(Of T)", type.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.Equal("Test1.Foo`1", type.Name);

            var method = output.Items[0].Items[0].Items[0];
            Assert.NotNull(method);
            Assert.Equal("Bar<K>(Int32)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.Equal("Bar(Of K)(Int32)", method.DisplayNames[SyntaxLanguage.VB]);
            Assert.Equal("Test1.Foo<T>.Bar<K>(System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
            Assert.Equal("Test1.Foo(Of T).Bar(Of K)(System.Int32)", method.DisplayQualifiedNames[SyntaxLanguage.VB]);
            Assert.Equal("Test1.Foo`1.Bar``1(System.Int32)", method.Name);
            Assert.Equal(1, output.Items.Count);
            var parameter = method.Syntax.Parameters[0];
            Assert.Equal("i", parameter.Name);
            Assert.Equal("System.Int32", parameter.Type);
            var returnValue = method.Syntax.Return;
            Assert.Null(returnValue);
        }

        [Trait("Related", "Attribute")]
        [Fact]
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromVBCode(code, MetadataReference.CreateFromFile(typeof(System.ComponentModel.TypeConverterAttribute).Assembly.Location)));
            Assert.Equal(1, output.Items.Count);
            var type = output.Items[0].Items[0];
            Assert.NotNull(type);
            Assert.Equal(@"<Serializable>
<AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Module Or AttributeTargets.Class Or AttributeTargets.Struct Or AttributeTargets.Enum Or AttributeTargets.Constructor Or AttributeTargets.Method Or AttributeTargets.Property Or AttributeTargets.Field Or AttributeTargets.Event Or AttributeTargets.Interface Or AttributeTargets.Parameter Or AttributeTargets.Delegate Or AttributeTargets.ReturnValue Or AttributeTargets.GenericParameter Or AttributeTargets.All, Inherited:=True, AllowMultiple:=True)>
<TypeConverter(GetType(TestAttribute))>
<Test(""test"")>
<Test(New Integer() {1, 2, 3})>
<Test(New Object() {Nothing, ""abc"", ""d""c, 1.1F, 1.2, CType(2, SByte), CType(3, Byte), CType(4, Short), CType(5, UShort), 6, 8L, 9UL, New Integer() {10, 11, 12}})>
<Test(New Type() {GetType(Func(Of )), GetType(Func(Of , )), GetType(Func(Of String, String))})>
Public Class TestAttribute
    Inherits Attribute
    Implements _Attribute", type.Syntax.Content[SyntaxLanguage.VB]);
            var ctor = type.Items[0];
            Assert.NotNull(type);
            Assert.Equal(@"<Test(1)>
<Test(2)>
Public Sub New(o As Object)", ctor.Syntax.Content[SyntaxLanguage.VB]);
        }

        [Fact]
        [Trait("Related", "ValueTuple")]
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromVBCode(code));
            Assert.Single(output.Items);
            var ns = output.Items[0];
            Assert.NotNull(ns);
            var foo = ns.Items[0];
            Assert.NotNull(foo);
            Assert.Equal("Test1.Foo", foo.Name);
            Assert.Single(foo.Items);
            var bar = foo.Items[0];
            Assert.Equal("Test1.Foo.Bar(System.ValueTuple{System.String,System.String})", bar.Name);
            // TODO: when https://github.com/dotnet/roslyn/issues/29390 will be fixed add space before tuple type
            Assert.Equal("Public Sub Bar(tuple As(prefix As String, uri As String))", bar.Syntax.Content[SyntaxLanguage.VB]);
        }

        [Fact]
        [Trait("Related", "ValueTuple")]
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromVBCode(code));
            Assert.Single(output.Items);
            var ns = output.Items[0];
            Assert.NotNull(ns);
            var foo = ns.Items[0];
            Assert.NotNull(foo);
            Assert.Equal("Test1.Foo", foo.Name);
            Assert.Single(foo.Items);
            var bar = foo.Items[0];
            Assert.Equal("Test1.Foo.Bar(System.ValueTuple{System.String,System.String})", bar.Name);
            // TODO: when https://github.com/dotnet/roslyn/issues/29390 will be fixed add space before tuple type
            Assert.Equal("Public Sub Bar(tuple As(String, String))", bar.Syntax.Content[SyntaxLanguage.VB]);
        }

        [Fact]
        [Trait("Related", "ValueTuple")]
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromVBCode(code));
            Assert.Single(output.Items);
            var ns = output.Items[0];
            Assert.NotNull(ns);
            var foo = ns.Items[0];
            Assert.NotNull(foo);
            Assert.Equal("Test1.Foo", foo.Name);
            Assert.Single(foo.Items);
            var bar = foo.Items[0];
            Assert.Equal("Test1.Foo.Bar(System.ValueTuple{System.String,System.String})", bar.Name);
            // TODO: when https://github.com/dotnet/roslyn/issues/29390 will be fixed add space before namespace
            Assert.Equal("Public Sub Bar(tuple As(String, uri As String))", bar.Syntax.Content[SyntaxLanguage.VB]);
        }

        [Fact]
        [Trait("Related", "ValueTuple")]
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromVBCode(code));
            Assert.Single(output.Items);
            var ns = output.Items[0];
            Assert.NotNull(ns);
            var foo = ns.Items[0];
            Assert.NotNull(foo);
            Assert.Equal("Test1.Foo", foo.Name);
            Assert.Single(foo.Items);
            var bar = foo.Items[0];
            Assert.Equal("Test1.Foo.Bar(System.ValueTuple{System.String,System.String}[])", bar.Name);
            // TODO: when https://github.com/dotnet/roslyn/issues/29390 will be fixed add space before tuple type
            Assert.Equal("Public Sub Bar(tuples As(String, String)())", bar.Syntax.Content[SyntaxLanguage.VB]);
        }

        [Fact]
        [Trait("Related", "ValueTuple")]
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromVBCode(code));
            Assert.Single(output.Items);
            var ns = output.Items[0];
            Assert.NotNull(ns);
            var foo = ns.Items[0];
            Assert.NotNull(foo);
            Assert.Equal("Test1.Foo", foo.Name);
            Assert.Single(foo.Items);
            var bar = foo.Items[0];
            Assert.Equal("Test1.Foo.Bar(System.Collections.Generic.IEnumerable{System.ValueTuple{System.String,System.String}})", bar.Name);
            // TODO: when https://github.com/dotnet/roslyn/issues/29390 will be fixed add space before tuple type
            Assert.Equal("Public Sub Bar(tuples As IEnumerable(Of(prefix As String, uri As String)))", bar.Syntax.Content[SyntaxLanguage.VB]);
        }

        [Fact]
        [Trait("Related", "ValueTuple")]
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromVBCode(code));
            Assert.Single(output.Items);
            var ns = output.Items[0];
            Assert.NotNull(ns);
            var foo = ns.Items[0];
            Assert.NotNull(foo);
            Assert.Equal("Test1.Foo", foo.Name);
            Assert.Single(foo.Items);
            var bar = foo.Items[0];
            Assert.Equal("Test1.Foo.Bar", bar.Name);
            // TODO: when https://github.com/dotnet/roslyn/issues/29390 will be fixed add space before tuple type
            Assert.Equal("Public Function Bar As(prefix As String, uri As String)", bar.Syntax.Content[SyntaxLanguage.VB]);
        }

        [Fact]
        [Trait("Related", "ValueTuple")]
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromVBCode(code));
            Assert.Single(output.Items);
            var ns = output.Items[0];
            Assert.NotNull(ns);
            var foo = ns.Items[0];
            Assert.NotNull(foo);
            Assert.Equal("Test1.Foo", foo.Name);
            Assert.Single(foo.Items);
            var bar = foo.Items[0];
            Assert.Equal("Test1.Foo.Bar", bar.Name);
            // TODO: when https://github.com/dotnet/roslyn/issues/29390 will be fixed add space before tuple type
            Assert.Equal("Public Function Bar As(String, String)", bar.Syntax.Content[SyntaxLanguage.VB]);
        }

        [Fact]
        [Trait("Related", "ValueTuple")]
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromVBCode(code));
            Assert.Single(output.Items);
            var ns = output.Items[0];
            Assert.NotNull(ns);
            var foo = ns.Items[0];
            Assert.NotNull(foo);
            Assert.Equal("Test1.Foo", foo.Name);
            Assert.Single(foo.Items);
            var bar = foo.Items[0];
            Assert.Equal("Test1.Foo.Bar", bar.Name);
            // TODO: when https://github.com/dotnet/roslyn/issues/29390 will be fixed add space before tuple type
            Assert.Equal("Public Function Bar As(String, uri As String)", bar.Syntax.Content[SyntaxLanguage.VB]);
        }

        [Fact]
        [Trait("Related", "ValueTuple")]
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
            MetadataItem output = GenerateYamlMetadata(CreateCompilationFromVBCode(code));
            Assert.Single(output.Items);
            var ns = output.Items[0];
            Assert.NotNull(ns);
            var foo = ns.Items[0];
            Assert.NotNull(foo);
            Assert.Equal("Test1.Foo", foo.Name);
            Assert.Single(foo.Items);
            var bar = foo.Items[0];
            Assert.Equal("Test1.Foo.Bar", bar.Name);
            // TODO: when https://github.com/dotnet/roslyn/issues/29390 will be fixed add space before tuple type
            Assert.Equal("Public Function Bar As IEnumerable(Of(prefix As String, uri As String))", bar.Syntax.Content[SyntaxLanguage.VB]);
        }

        private static Compilation CreateCompilationFromVBCode(string code, params MetadataReference[] references)
        {
            return CreateCompilationFromVBCode(code, "test.dll", references);
        }

        private static Compilation CreateCompilationFromVBCode(string code, string assemblyName, params MetadataReference[] references)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(code);
            var defaultReferences = new List<MetadataReference> { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };
            if (references != null)
            {
                defaultReferences.AddRange(references);
            }

            var compilation = VisualBasicCompilation.Create(
                assemblyName,
                options: new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                syntaxTrees: new[] { tree },
                references: defaultReferences);
            return compilation;
        }

        private static Assembly CreateAssemblyFromVBCode(string code, string assemblyName)
        {
            // MemoryStream fails when MetadataReference.CreateFromAssembly with error: Empty path name is not legal
            var compilation = CreateCompilationFromVBCode(code);
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
