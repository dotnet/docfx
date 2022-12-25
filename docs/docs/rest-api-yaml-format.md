DocFx: Metadata Format for .NET Languages
===============================================

## 0. Introduction


### 0.1 Goal and Non-goals

### 0.2 Terminology

## 1. Items

The following .NET elements are defined as *items* in metadata:

1. Namespaces
2. Types, including class, struct, interface, enum, delegate
3. Type members, including field, property, method, event

Other elements such as parameters and generic parameters are not standalone *items*, they're part of other *items*.

## 2. Identifiers

### 2.1 Unique Identifiers

For any *item* in .NET languages, its *UID* is defined by concatenating its *parent*'s *UID* and its own *ID* with a dot.
The *ID* for each kind of item is defined in following sections. The basic principle here is to make *ID* format close to source code and easy for human reading.

*UID* is similar to the document comment id, which is started with type prefix, for example, `T:`, or `M:`, but *UID* do not.

There **MUST NOT** be any whitespace between method name, parentheses, parameters, and commas.

### 2.2 Spec Identifiers

Spec identifier is another form of *UID*.
It can spec a generic type with type arguments (for example, for parameters, return types or inheritances) and these *UID*s are unique in one yaml file. 

It is a simple modified Unique Identifiers, when it contains generic type arguments, it will use `{Name}` instead `` `N ``.
For type parameter, it will be `{Name}`.
And it also supports array and pointer.

> Example 2.2 Spec Identifier
>
> C#:
> ```csharp
> namespace Foo
> {
>    public class Bar
>    {
>       public unsafe List<String> FooBar<TArg>(int[] arg1, byte* arg2, TArg arg3, List<TArg[]> arg4)
>       {
>           return null;
>       }
>    }
> }
> ```
> YAML:
> ```yaml
> references:
> - uid: System.Collections.Generic.List{System.String}
> - uid: System.Int32[]
> - uid: System.Byte*
> - uid: {TArg}
> - uid: System.Collections.Generic.List{{TArg}[]}
> ```

## 3. Namespaces

For all namespaces, they are flat, e.i. namespaces do not have the parent namespace.
So for any namespace, *ID* is always same with its *UID*. 

> Example 3 Namespace
>
> C#:
> ```csharp
> namespace System.IO
> {
> }
> ```
> YAML:
> ```yaml
> uid: System.IO
> id: System.IO
> name: System.IO
> fullName: System.IO
> ```

The children of namespace are all the visible types in the namespace.

## 4. Types

Types include classes, structs, interfaces, enums, and delegates.
They have following properties: summary, remarks, syntax, namespace, assemblies, inheritance.
The *parents* of types are namespaces.
The *children* of types are members.

#### ID

*ID* for a type is also its *name*.

> Example 4 Type
> 
> C#:
> ```csharp
> namespace System
> {
>     public class String {}
>     public struct Boolean {}
>     public interface IComparable {}
>     public enum ConsoleColor {}
>     public delegate void Action();
> }
> ```
> YAML:
> ```yaml
> - uid: System.String
>   id: String
>   name.csharp: String
>   fullName.csharp: System.String
> - uid: System.Boolean
>   id: Boolean
>   name.csharp: Boolean
>   fullName.csharp: System.String
> - uid: System.IComparable
>   id: IComparable
>   name.csharp: IComparable
>   fullName.csharp: System.IComparable
> - uid: System.ConsoleColor
>   id: ConsoleColor
>   name.csharp: ConsoleColor
>   fullName.csharp: System.ConsoleColor
> - uid: System.Action
>   id: Action
>   name.csharp: Action
>   fullName.csharp: System.Action
> ```

#### 4.1 ID for Nested Types

For nested types, *ID* is defined by concatenating the *ID* of all its containing types and the *ID* of itself, separated by a dot.

The parent type of a nested type is its containing namespace, rather than its containing type.

> Example 4.1 Nested type
>
> C#:
> ```csharp
> namespace System
> {
>     public class Environment
>     {
>         public enum SpecialFolder {}
>     }
> }
> ```
> YAML:
> ```yaml
> uid: System.Environment.SpecialFolder
> id: Environment.SpecialFolder
> name.csharp: Environment.SpecialFolder
> fullName.csharp: System.Environment.SpecialFolder
> ```

#### 4.2 Inheritance

Only class contains inheritance, and the inheritance is a list of spec id.

> Example 4.2 Inheritance
>
> C#:
> ```csharp
> namespace System.Collections.Generic
> {
>     public class KeyedByTypeCollection<TItem> : KeyedCollection<Type, TItem>
>     {
>     }
> }
> ```
> YAML:
> ```yaml
> uid : System.Collections.Generic.KeyedByTypeCollection`1
> inheritance:
> - System.Collections.ObjectModel.KeyedCollection{System.Type,{TItem}}
> - System.Collections.ObjectModel.Collection{{TItem}}
> - System.Object
> ```

#### 4.3 Syntax

The syntax part for type contains declaration, and descriptions of type parameters for different languages.
For delegates, it also contains descriptions of parameters and a return type.

## 5. Members

Members include fields, properties, methods, and events.
They have the following properties: summary, remarks, exceptions, and syntax.
The parents of members are types.
Members never have children, and
all parameter types or return types are spec id.

#### 5.1 Constructors

The *ID* of a constructor is defined by `#ctor`, followed by the list of the *UIDs* of its parameter types:
When a constructor does not have parameter, its *ID* **MUST NOT** end with parentheses.

The syntax part for constructors contains a special language declaration, and descriptions of parameters.

> Example 5.1 Constructor
>
> C#:
> ```csharp
> namespace System
> {
>     public sealed class String
>     {
>         public String();
>         public String(char[] chars);
>     }
> }
> ```
> YAML:
> ```yaml
> - uid: System.String.#ctor
>   id: #ctor
>   name.csharp: String()
>   fullName.csharp: System.String.String()
> - uid: System.String.#ctor(System.Char[])
>   id: #ctor(System.Char[])
>   name.csharp: String(Char[])
>   fullName.csharp: System.String.String(System.Char[])
> ```

#### 5.2 Methods

The *ID* of a method is defined by its name, followed by the list of the *UIDs* of its parameter types:
```yaml
method_name(param1,param2,...)
```

When a method does not have parameter, its *ID* **MUST** end with parentheses.

The syntax part for method contains a special language declaration, and descriptions of type parameters for generic method, descriptions of parameters and return type.

> Example 5.2 Method
>
> C#:
> ```csharp
> namespace System
> {
>     public sealed class String
>     {
>         public String ToString();
>         public String ToString(IFormatProvider provider);
>     }
> }
> ```
> YAML:
> ```yaml
> - uid: System.String.ToString
>   id: ToString
>   name.csharp: ToString()
>   fullName.csharp: System.String.ToString()
> - uid: System.String.ToString(System.IFormatProvider)
>   id: ToString(System.IFormatProvider)
>   name.csharp: ToString(IFormatProvider)
>   fullName.csharp: System.String.ToString(System.IFormatProvider)
> ```

#### 5.2.1 Explicit Interface Implementation

The *ID* of an explicit interface implementation (EII) member **MUST** be prefixed by the *UID* of the interface it implements and replace `.` to `#`.

> Example 2.6 Explicit interface implementation (EII)
>
> C#:
> ```csharp
> namespace System
> {
>     using System.Collections;
>
>     public sealed class String : IEnumerable
>     {
>         IEnumerator IEnumerable.GetEnumerator();
>     }
> }
> ```
> YAML:
> ```yaml
> - uid: "System.String.System#Collections#IEnumerable#GetEnumerator"
>   id: "System#Collections#IEnumerable#GetEnumerator"
>   name.csharp: IEnumerable.GetEnumerator()
>   fullName.csharp: System.String.System.Collections.IEnumerable.GetEnumerator()
> ```

#### 5.4 Operator Overloads

The *IDs* of operator overloads are same with the metadata name (for example, `op_Equality`).
The names of operator overloads are similar to MSDN, just remove `op_` from the metadata name of the method.
For instance, the name of the equals (`==`) operator is `Equality`.

Type conversion operator can be considered a special operator whose name is the UID of the target type, with one parameter of the source type. For example, an operator that converts from string to int should be `Explicit(System.String to System.Int32)`.

The syntax part for methods contains a special language declaration, descriptions of parameters and return type.

> Example 5.4 Operator overload
>
> ```csharp
> namespace System
> {
>     public struct Decimal
>     {
>         public static implicit operator Decimal(Char value);
>     }
>
>     public sealed class String
>     {
>         public static bool operator ==(String a, String b);
>     }
> }
> ```
> YAML:
> ```yaml
> - uid: System.Decimal.op_Implicit(System.Char)~System.Decimal
>   id: op_Implicit(System.Char)~System.Decimal
>   name.csharp: Implicit(Char to Decimal)
>   fullName.csharp: System.Decimal.Implicit(System.Char to System.Decimal)
> - uid: System.String.op_Equality(System.String,System.String)
>   id: op_Equality(System.String,System.String)
>   name.csharp: Equality(String,String)
>   fullName.csharp: System.String.Equality(System.String,System.String)
> ```

Please check [overloadable operators][1] for all overloadable operators.

#### 5.5 Field, Property or Event

The *ID* of field, property or event is its name.
The syntax part for field contains a special language declaration and descriptions of field type.
For property, it contains a special language declaration, descriptions of parameters, and return type.
For event, it contains a special language declaration and descriptions of event handler type.

> Example 5.5 Field, Property and Event
>
> C#:
> ```csharp
> namespace System
> {
>     public sealed class String
>     {
>         public static readonly String Empty;
>         public int Length { get; }
>     }
>
>     public static class Console
>     {
>         public static event ConsoleCancelEventHandler CancelKeyPress;
>     }
> }
> ```
> YAML:
> ```yaml
> - uid: System.String.Empty
>   id: Empty
>   name.csharp: Empty
>   fullName.csharp: System.String.Empty
> - uid: System.String.Length
>   id: Length
>   name.csharp: Length
>   fullName.csharp: System.String.Length
> - uid: System.Console.CancelKeyPress
>   id: CancelKeyPress
>   name.csharp: CancelKeyPress
>   fullName.csharp: System.Console.CancelKeyPress
> ```

#### 5.6 Indexer

Indexer operator's name is metadata name, by default, it is `Item`, with brackets and parameters.

> Example 5.6 Indexer
>
> ```csharp
> namespace System.Collections
> {
>     public interface IList
>     {
>         object this[int index] { get; set; }
>     }
> }
> ```
> YAML:
> ```yaml
> - uid: "System.Collections.IList.Item[System.Int32]"
>   id: "Item[System.Int32]"
>   name.csharp: Item[Int32]
>   fullName.csharp: System.Collections.IList.Item[System.Int32]
> ```

## 6. Generics

The *ID* of a generic type is its name with followed by `` `n ``, `n` and the count of generic type count, which is the same as the rule for document comment ID.
For example, ``Dictionary`2``.

The *ID* of a generic method uses postfix ``` ``n ```, `n` is the count of in method parameters, for example, ```System.Tuple.Create``1(``0)```.

> Example 2.7 Generic
>
> ```csharp
> namespace System
> {
>     public static class Tuple
>     {
>         public static Tuple<T1> Create<T1>(T1 item1);
>         public static Tuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2);
>     }
> }
> ```
> YAML:
> ```yaml
> - uid: System.Tuple.Create``1(``0)
>   id: Create``1(``0)
>   name.csharp:  Create<T1>(T1)
>   fullName.csharp: System.Tuple.Create<T1>(T1)
> - uid: System.Tuple.Create``2(``0,``1)
>   id: Create``2(``0,``1)
>   name.csharp:  Create<T1,T2>(T1,T2)
>   fullName.csharp: System.Tuple.Create<T1,T2>(T1,T2)
> ```

## 7. Reference

The reference contains the following members:
  name, fullName, summary, isExternal, href, and more.

The *UID* in reference can be a *Spec Id*, then it contains one more member: spec.
The *spec* in reference is very like a list of lightweight references, it describes how to compose the generic type in some special language.

> Example 7 *spec* for references
>
> YAML:
> ```yaml
> references:
> - uid: System.Collections.Generic.Dictionary{System.String,System.Collections.Generic.List{System.Int32}}
>   name.csharp: Dictionary<String, List<Int32>>
>   fullName.csharp: System.Collections.Generic.Dictionary<System.String, System.Collections.Generic.List<System.Int32>>
>   spec.csharp:
>   - uid: System.Collections.Generic.Dictionary`2
>     name: Dictionary
>     fullName: System.Collections.Generic.Dictionary
>     isExternal: true
>   - name: <
>     fullName: <
>   - uid: System.String
>     name: String
>     fullName: System.String
>     isExternal: true
>   - name: ', '
>     fullName: ', '
>   - uid: System.Collections.Generic.List`1
>     name: List
>     fullName: System.Collections.Generic.List
>     isExternal: true
>   - name: <
>     fullName: <
>   - uid: System.Int32
>     name: Int32
>     fullName: System.Int32
>     isExternal: true
>   - name: '>'
>     fullName: '>'
>   - name: '>'
>     fullName: '>'
> ```

[1]: https://msdn.microsoft.com/en-us/library/8edha89s.aspx
