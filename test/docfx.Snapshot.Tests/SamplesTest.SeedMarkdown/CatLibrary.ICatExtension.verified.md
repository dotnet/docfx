# <a id="CatLibrary_ICatExtension"></a> Class ICatExtension

Namespace: [CatLibrary](CatLibrary.md)  
Assembly: CatLibrary.dll  

It's the class that contains ICat interface's extension method.
<p>This class must be <b>public</b> and <b>static</b>.</p><p>Also it shouldn't be a geneic class</p>

```csharp
public static class ICatExtension
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ICatExtension](CatLibrary.ICatExtension.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="CatLibrary_ICatExtension_Play_CatLibrary_ICat_CatLibrary_Core_ContainersRefType_ColorType_"></a> Play\(ICat, ColorType\)

Extension method to let cat play

```csharp
public static void Play(this ICat icat, ContainersRefType.ColorType toy)
```

#### Parameters

`icat` [ICat](CatLibrary.ICat.md)

Cat

`toy` [ContainersRefType](CatLibrary.Core.ContainersRefType.md).[ColorType](CatLibrary.Core.ContainersRefType.ColorType.md)

Something to play

### <a id="CatLibrary_ICatExtension_Sleep_CatLibrary_ICat_System_Int64_"></a> Sleep\(ICat, long\)

Extension method hint that how long the cat can sleep.

```csharp
public static void Sleep(this ICat icat, long hours)
```

#### Parameters

`icat` [ICat](CatLibrary.ICat.md)

The type will be extended.

`hours` [long](https://learn.microsoft.com/dotnet/api/system.int64)

The length of sleep.

