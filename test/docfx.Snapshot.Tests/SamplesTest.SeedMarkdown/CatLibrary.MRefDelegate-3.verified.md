﻿# Delegate MRefDelegate<K, T, L>

__Namespace:__ [CatLibrary](CatLibrary.md)  
__Assembly:__ CatLibrary.dll

Generic delegate with many constrains.

```csharp
public delegate void MRefDelegate<K, T, L>(K k, T t, L l) where K : class, IComparable where T : struct where L : Tom, IEnumerable<long>
```

## Parameters

`k` K

Type K.

`t` T

Type T.

`l` L

Type L.

## Type Parameters

`K`

Generic K.

`T`

Generic T.

`L`

Generic L.

