# <a id="CatLibrary_ICat"></a> Interface ICat

Namespace: [CatLibrary](CatLibrary.md)  
Assembly: CatLibrary.dll  

Cat's interface

```csharp
public interface ICat : IAnimal
```

#### Implements

[IAnimal](CatLibrary.IAnimal.md)

#### Extension Methods

[ICatExtension.Play\(ICat, ContainersRefType.ColorType\)](CatLibrary.ICatExtension.md\#CatLibrary\_ICatExtension\_Play\_CatLibrary\_ICat\_CatLibrary\_Core\_ContainersRefType\_ColorType\_), 
[ICatExtension.Sleep\(ICat, long\)](CatLibrary.ICatExtension.md\#CatLibrary\_ICatExtension\_Sleep\_CatLibrary\_ICat\_System\_Int64\_)

### <a id="CatLibrary_ICat_eat"></a> eat

eat event of cat. Every cat must implement this event.

```csharp
event EventHandler eat
```

#### Event Type

 [EventHandler](https://learn.microsoft.com/dotnet/api/system.eventhandler)

