# <a id="CatLibrary_IAnimal"></a> Interface IAnimal

Namespace: [CatLibrary](CatLibrary.md)  
Assembly: CatLibrary.dll  

This is <b>basic</b> interface of all animal.

```csharp
public interface IAnimal
```

## Properties

### <a id="CatLibrary_IAnimal_Name"></a> Name

Name of Animal.

```csharp
string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="CatLibrary_IAnimal_Item_System_Int32_"></a> this\[int\]

Return specific number animal's name.

```csharp
string this[int index] { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="CatLibrary_IAnimal_Eat"></a> Eat\(\)

Animal's eat method.

```csharp
void Eat()
```

### <a id="CatLibrary_IAnimal_Eat__1___0_"></a> Eat<Tool\>\(Tool\)

Overload method of eat. This define the animal eat by which tool.

```csharp
void Eat<Tool>(Tool tool) where Tool : class
```

#### Parameters

`tool` Tool

Tool name.

#### Type Parameters

`Tool` 

It's a class type.

### <a id="CatLibrary_IAnimal_Eat_System_String_"></a> Eat\(string\)

Feed the animal with some food

```csharp
void Eat(string food)
```

#### Parameters

`food` [string](https://learn.microsoft.com/dotnet/api/system.string)

Food to eat

