# Interface ICat

__Namespace:__ [CatLibrary](CatLibrary.md)  
__Assembly:__ CatLibrary.dll

Cat's interface

```csharp
public interface ICat : IAnimal
```

#### Implements

[IAnimal](CatLibrary.IAnimal.md)

#### Extension Methods

[ICatExtension.Play(ICat, ContainersRefType.ColorType)](CatLibrary.ICatExtension.md#CatLibrary.ICatExtension.Play(CatLibrary.Core.ContainersRefType.ColorType)), 
[ICatExtension.Sleep(ICat, long)](CatLibrary.ICatExtension.md#CatLibrary.ICatExtension.Sleep(System.Int64))

## Events

### eat

eat event of cat. Every cat must implement this event.

```csharp
event EventHandler eat
```

Event Type

[EventHandler](https://learn.microsoft.com/dotnet/api/system.eventhandler)

