# <a id="CatLibrary_Tom"></a> Class Tom

Namespace: [CatLibrary](CatLibrary.md)  
Assembly: CatLibrary.dll  

Tom class is only inherit from Object. Not any member inside itself.

```csharp
public class Tom
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Tom](CatLibrary.Tom.md)

#### Derived

[TomFromBaseClass](CatLibrary.TomFromBaseClass.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="CatLibrary_Tom_TomMethod_CatLibrary_Complex_CatLibrary_TomFromBaseClass_CatLibrary_TomFromBaseClass__System_Tuple_System_String_CatLibrary_Tom__"></a> TomMethod\(Complex<TomFromBaseClass, TomFromBaseClass\>, Tuple<string, Tom\>\)

This is a Tom Method with complex type as return

```csharp
public Complex<string, TomFromBaseClass> TomMethod(Complex<TomFromBaseClass, TomFromBaseClass> a, Tuple<string, Tom> b)
```

#### Parameters

`a` [Complex](CatLibrary.Complex\-2.md)<[TomFromBaseClass](CatLibrary.TomFromBaseClass.md), [TomFromBaseClass](CatLibrary.TomFromBaseClass.md)\>

A complex input

`b` [Tuple](https://learn.microsoft.com/dotnet/api/system.tuple\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [Tom](CatLibrary.Tom.md)\>

Another complex input

#### Returns

 [Complex](CatLibrary.Complex\-2.md)<[string](https://learn.microsoft.com/dotnet/api/system.string), [TomFromBaseClass](CatLibrary.TomFromBaseClass.md)\>

Complex @CatLibrary.TomFromBaseClass

#### Exceptions

 [NotImplementedException](https://learn.microsoft.com/dotnet/api/system.notimplementedexception)

This is not implemented

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

This is the exception to be thrown when implemented

 [CatException](CatLibrary.CatException\-1.md)<T\>

This is the exception in current documentation

