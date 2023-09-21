# Class Issue8101

_Namespace:_ [BuildFromProject](BuildFromProject.md)
_Assembly:_ BuildFromProject.dll

```csharp
public class Inheritdoc.Issue8101
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Inheritdoc](BuildFromProject.Inheritdoc.md).[Issue8101](BuildFromProject.Inheritdoc.Issue8101.md)

#### Inherited Members

[object.Equals(object?)](https://learn.microsoft.com/dotnet/api/system.object.equals#system-object-equals(system-object)), 
[object.Equals(object?, object?)](https://learn.microsoft.com/dotnet/api/system.object.equals#system-object-equals(system-object-system-object)), 
[object.GetHashCode()](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType()](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone()](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals(object?, object?)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString()](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### Tween(float, float, float, Action<float>)

Create a new tween.

```csharp
public static object Tween(float from, float to, float duration, Action<float> onChange)
```

#### Parameters

`from` [float](https://learn.microsoft.com/dotnet/api/system.single)

The starting value.

`to` [float](https://learn.microsoft.com/dotnet/api/system.single)

The end value.

`duration` [float](https://learn.microsoft.com/dotnet/api/system.single)

Total tween duration in seconds.

`onChange` [Action](https://learn.microsoft.com/dotnet/api/system.action-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)>

A callback that will be invoked every time the tween value changes.

#### Returns

[object](https://learn.microsoft.com/dotnet/api/system.object)

The newly created tween instance.

### Tween(int, int, float, Action<int>)

Create a new tween.

```csharp
public static object Tween(int from, int to, float duration, Action<int> onChange)
```

#### Parameters

`from` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The starting value.

`to` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The end value.

`duration` [float](https://learn.microsoft.com/dotnet/api/system.single)

Total tween duration in seconds.

`onChange` [Action](https://learn.microsoft.com/dotnet/api/system.action-1)<[int](https://learn.microsoft.com/dotnet/api/system.int32)>

A callback that will be invoked every time the tween value changes.

#### Returns

[object](https://learn.microsoft.com/dotnet/api/system.object)

The newly created tween instance.

