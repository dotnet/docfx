# Interface IAnimal

__Namespace:__ [CatLibrary](CatLibrary.md)  
__Assembly:__ CatLibrary.dll

This is <b>basic</b> interface of all animal.

```csharp
public interface IAnimal
```

## Properties

### Name

Name of Animal.

```csharp
string Name { get; }
```

Property Value

[string](https://learn.microsoft.com/dotnet/api/system.string)

### this[int]

Return specific number animal's name.

```csharp
string this[int index] { get; }
```

Property Value

[string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### Eat()

Animal's eat method.

```csharp
void Eat()
```

### Eat<Tool>(Tool)

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

### Eat(string)

Feed the animal with some food

```csharp
void Eat(string food)
```

#### Parameters

`food` [string](https://learn.microsoft.com/dotnet/api/system.string)

Food to eat

