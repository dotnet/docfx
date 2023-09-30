# Interface ICat

Namespace: [CatLibrary](CatLibrary.md)  
Assembly: CatLibrary.dll

Cat's interface

```csharp
public interface ICat : IAnimal
```

###### Implements

[IAnimal](CatLibrary.IAnimal.md)

###### Extension Methods

[ICatExtension.Play(ICat, ContainersRefType.ColorType)](CatLibrary.ICatExtension.md#CatLibrary_ICatExtension_Play_CatLibrary_ICat_CatLibrary_Core_ContainersRefType_ColorType_), 
[ICatExtension.Sleep(ICat, long)](CatLibrary.ICatExtension.md#CatLibrary_ICatExtension_Sleep_CatLibrary_ICat_System_Int64_)

### <a id="CatLibrary_ICat_eat"></a>eat

eat event of cat. Every cat must implement this event.

```csharp
event EventHandler eat
```

#### Event Type

[EventHandler](https://learn.microsoft.com/dotnet/api/system.eventhandler)

