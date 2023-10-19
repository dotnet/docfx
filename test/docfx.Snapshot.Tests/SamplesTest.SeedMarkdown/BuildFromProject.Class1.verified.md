# <a id="BuildFromProject_Class1"></a> Class Class1

Namespace: [BuildFromProject](BuildFromProject.md)  
Assembly: BuildFromProject.dll  

```csharp
public class Class1 : IClass1
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Class1](BuildFromProject.Class1.md)

#### Implements

IClass1

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="BuildFromProject_Class1_Issue1651"></a> Issue1651\(\)

Pricing models are used to calculate theoretical option values
<ul><li><span class="term">1</span>Black Scholes</li><li><span class="term">2</span>Black76</li><li><span class="term">3</span>Black76Fut</li><li><span class="term">4</span>Equity Tree</li><li><span class="term">5</span>Variance Swap</li><li><span class="term">6</span>Dividend Forecast</li></ul>

```csharp
public void Issue1651()
```

### <a id="BuildFromProject_Class1_Issue1887"></a> Issue1887\(\)

IConfiguration related helper and extension routines.

```csharp
public void Issue1887()
```

### <a id="BuildFromProject_Class1_Issue2623"></a> Issue2623\(\)

```csharp
public void Issue2623()
```

#### Examples

```csharp
MyClass myClass = new MyClass();

void Update()
{
    myClass.Execute();
}
```

#### Remarks

For example:

    MyClass myClass = new MyClass();

    void Update()
    {
        myClass.Execute();
    }

### <a id="BuildFromProject_Class1_Issue2723"></a> Issue2723\(\)

```csharp
public void Issue2723()
```

#### Remarks

> [!NOTE]
> This is a &lt;note&gt;. &amp; " '

Inline `<angle brackets>`.

[link](https://www.github.com "title")

```csharp
for (var i = 0; i > 10; i++) // & " '
var range = new Range<int> { Min = 0, Max = 10 };
```

<pre><code class="lang-csharp">var range = new Range&lt;int&gt; { Min = 0, Max = 10 };</code></pre>

### <a id="BuildFromProject_Class1_Issue4017"></a> Issue4017\(\)

```csharp
public void Issue4017()
```

#### Examples

<pre><code class="lang-cs">public void HookMessageDeleted(BaseSocketClient client)
{
    client.MessageDeleted += HandleMessageDelete;
}

public Task HandleMessageDelete(Cacheable&lt;IMessage, ulong&gt; cachedMessage, ISocketMessageChannel channel)
{
    // check if the message exists in cache; if not, we cannot report what was removed
    if (!cachedMessage.HasValue) return;
    var message = cachedMessage.Value;
    Console.WriteLine($"A message ({message.Id}) from {message.Author} was removed from the channel {channel.Name} ({channel.Id}):"
        + Environment.NewLine
        + message.Content);
    return Task.CompletedTask;
}</code></pre>

#### Remarks

<pre><code class="lang-csharp">void Update()
{
    myClass.Execute();
}</code></pre>

### <a id="BuildFromProject_Class1_Issue4392"></a> Issue4392\(\)

```csharp
public void Issue4392()
```

#### Remarks

<code>@"\\?\"</code> `@"\\?\"`

### <a id="BuildFromProject_Class1_Issue7484"></a> Issue7484\(\)

```csharp
public void Issue7484()
```

#### Remarks

There's really no reason to not believe that this class can test things.
<table><thead><tr><th class="term">Term</th><th class="description">Description</th></tr></thead><tbody><tr><td class="term">A Term</td><td class="description">A Description</td></tr><tr><td class="term">Bee Term</td><td class="description">Bee Description</td></tr></tbody></table>

### <a id="BuildFromProject_Class1_Issue8764__1"></a> Issue8764<T\>\(\)

```csharp
public void Issue8764<T>() where T : unmanaged
```

#### Type Parameters

`T` 

### <a id="BuildFromProject_Class1_Issue896"></a> Issue896\(\)

Test

```csharp
public void Issue896()
```

#### See Also

[Class1](BuildFromProject.Class1.md).[Test](BuildFromProject.Class1.Test\-1.md)<T\>, 
[Class1](BuildFromProject.Class1.md)

### <a id="BuildFromProject_Class1_Issue9216"></a> Issue9216\(\)

Calculates the determinant of a 3-dimensional matrix:

$$A = \begin{vmatrix} a_{11} & a_{12} & a_{13} \\ a_{21} & a_{22} & a_{23} \\ a_{31} & a_{32} & a_{33} \end{vmatrix}$$

Returns the smallest value:

$$\left\{\begin{matrix}a, a<b \\ b, b>a\\ \end{matrix} \right.$$

```csharp
public static double Issue9216()
```

#### Returns

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="BuildFromProject_Class1_XmlCommentIncludeTag"></a> XmlCommentIncludeTag\(\)

This method should do something...

```csharp
public void XmlCommentIncludeTag()
```

#### Remarks

This is remarks.

