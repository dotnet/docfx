# Class Cat

_Namespace:_ [CatLibrary](CatLibrary.md)

_Assembly:_ CatLibrary.dll

<p>Here's main class of this <i>Demo</i>.</p>
<p>You can see mostly type of article within this class and you for more detail, please see the remarks.</p>
<p></p>
<p>this class is a template class. It has two Generic parameter. they are: <code class="typeparamref">T</code> and <code class="typeparamref">K</code>.</p>
<p>The extension method of this class can refer to <xref href="CatLibrary.ICatExtension" data-throw-if-not-resolved="false"></xref> class</p>

```csharp
[Serializable]
public class Cat<T, K> : ICat, IAnimal where T : class, new() where K : struct
```

## Type Parameters

`T`

This type should be class and can new instance.

`K`

This type is a struct type, class type can't be used for this parameter.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Cat](CatLibrary.Cat-2.md)<T, K>

#### Implements

[ICat](CatLibrary.ICat.md), 
[IAnimal](CatLibrary.IAnimal.md)

#### Inherited Members

[object.Equals(object?)](https://learn.microsoft.com/dotnet/api/system.object.equals#system-object-equals(system-object)), 
[object.Equals(object?, object?)](https://learn.microsoft.com/dotnet/api/system.object.equals#system-object-equals(system-object-system-object)), 
[object.GetHashCode()](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType()](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone()](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals(object?, object?)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString()](https://learn.microsoft.com/dotnet/api/system.object.tostring)

#### Extension Methods

[ICatExtension.Play(ICat, ContainersRefType.ColorType)](CatLibrary.ICatExtension.md#CatLibrary.ICatExtension.Play(CatLibrary.Core.ContainersRefType.ColorType)), 
[ICatExtension.Sleep(ICat, long)](CatLibrary.ICatExtension.md#CatLibrary.ICatExtension.Sleep(System.Int64))

## Examples

<p>Here's example of how to create an instance of this class. As T is limited with <code>class</code> and K is limited with <code>struct</code>.</p>
<pre><code class="lang-c#">var a = new Cat(object, int)();
int catNumber = new int();
unsafe
{
    a.GetFeetLength(catNumber);
}</code></pre>
<p>As you see, here we bring in <b>pointer</b> so we need to add <code class="languageKeyword">unsafe</code> keyword.</p>

## Remarks

<p>Here's all the content you can see in this class.</p>

## Constructors

### Cat()

Default constructor.

```csharp
public Cat()
```

### Cat(T)

Constructor with one generic parameter.

```csharp
public Cat(T ownType)
```

#### Parameters

`ownType` T

This parameter type defined by class.

### Cat(string, out int, string, bool)

It's a complex constructor. The parameter will have some attributes.

```csharp
public Cat(string nickName, out int age, string realName, bool isHealthy)
```

#### Parameters

`nickName` [string](https://learn.microsoft.com/dotnet/api/system.string)

it's string type.

`age` [int](https://learn.microsoft.com/dotnet/api/system.int32)

It's an out and ref parameter.

`realName` [string](https://learn.microsoft.com/dotnet/api/system.string)

It's an out paramter.

`isHealthy` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

It's an in parameter.

## Fields

### isHealthy

Field with attribute.

```csharp
[ContextStatic]
[NonSerialized]
public bool isHealthy
```

Field Value

[bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Properties

### Age

Hint cat's age.

```csharp
protected int Age { get; set; }
```

Property Value

[int](https://learn.microsoft.com/dotnet/api/system.int32)

### Name

EII property.

```csharp
public string Name { get; }
```

Property Value

[string](https://learn.microsoft.com/dotnet/api/system.string)

### this[string]

This is index property of Cat. You can see that the visibility is different between <code>get</code> and <code>set</code> method.

```csharp
public int this[string a] { protected get; set; }
```

Property Value

[int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### CalculateFood(DateTime)

It's a method with complex return type.

```csharp
public Dictionary<string, List<int>> CalculateFood(DateTime date)
```

#### Parameters

`date` [DateTime](https://learn.microsoft.com/dotnet/api/system.datetime)

Date time to now.

#### Returns

[Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1)<[int](https://learn.microsoft.com/dotnet/api/system.int32)>>

It's a relationship map of different kind food.

### Equals(object)

Override the method of <code>Object.Equals(object obj).</code>

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

Can pass any class type.

#### Returns

[bool](https://learn.microsoft.com/dotnet/api/system.boolean)

The return value tell you whehter the compare operation is successful.

### GetTailLength(int*, params object[])

It's an <code>unsafe</code> method.
As you see, <code class="paramref">catName</code> is a <b>pointer</b>, so we need to add <code class="languageKeyword">unsafe</code> keyword.

```csharp
public long GetTailLength(int* catName, params object[] parameters)
```

#### Parameters

`catName` [int](https://learn.microsoft.com/dotnet/api/system.int32)*

Thie represent for cat name length.

`parameters` [object](https://learn.microsoft.com/dotnet/api/system.object)[]

Optional parameters.

#### Returns

[long](https://learn.microsoft.com/dotnet/api/system.int64)

Return cat tail's length.

### Jump(T, K, ref bool)

This method have attribute above it.

```csharp
[Conditional("Debug")]
public void Jump(T ownType, K anotherOwnType, ref bool cheat)
```

#### Parameters

`ownType` T

Type come from class define.

`anotherOwnType` K

Type come from class define.

`cheat` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Hint whether this cat has cheat mode.

#### Exceptions

[ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

This is an argument exception

## Events

### ownEat

Eat event of this cat

```csharp
public event EventHandler ownEat
```

Event Type

[EventHandler](https://learn.microsoft.com/dotnet/api/system.eventhandler)

## Operators

### operator +(Cat<T, K>, int)

Addition operator of this class.

```csharp
public static int operator +(Cat<T, K> lsr, int rsr)
```

#### Parameters

`lsr` [Cat](CatLibrary.Cat-2.md)<T, K>

..

`rsr` [int](https://learn.microsoft.com/dotnet/api/system.int32)

~~

#### Returns

[int](https://learn.microsoft.com/dotnet/api/system.int32)

Result with <i>int</i> type.

### explicit operator Tom(Cat<T, K>)

Expilicit operator of this class.
<p>It means this cat can evolve to change to Tom. Tom and Jerry.</p>

```csharp
public static explicit operator Tom(Cat<T, K> src)
```

#### Parameters

`src` [Cat](CatLibrary.Cat-2.md)<T, K>

Instance of this class.

#### Returns

[Tom](CatLibrary.Tom.md)

Advanced class type of cat.

### operator -(Cat<T, K>, int)

Similar with operaotr +, refer to that topic.

```csharp
public static int operator -(Cat<T, K> lsr, int rsr)
```

#### Parameters

`lsr` [Cat](CatLibrary.Cat-2.md)<T, K>

`rsr` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

[int](https://learn.microsoft.com/dotnet/api/system.int32)

