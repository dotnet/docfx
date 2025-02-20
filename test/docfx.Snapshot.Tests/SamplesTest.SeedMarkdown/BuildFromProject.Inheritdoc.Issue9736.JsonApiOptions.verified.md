# <a id="BuildFromProject_Inheritdoc_Issue9736_JsonApiOptions"></a> Class Inheritdoc.Issue9736.JsonApiOptions

Namespace: [BuildFromProject](BuildFromProject.md)  
Assembly: BuildFromProject.dll  

```csharp
public sealed class Inheritdoc.Issue9736.JsonApiOptions : Inheritdoc.Issue9736.IJsonApiOptions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Inheritdoc.Issue9736.JsonApiOptions](BuildFromProject.Inheritdoc.Issue9736.JsonApiOptions.md)

#### Implements

[Inheritdoc.Issue9736.IJsonApiOptions](BuildFromProject.Inheritdoc.Issue9736.IJsonApiOptions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Properties

### <a id="BuildFromProject_Inheritdoc_Issue9736_JsonApiOptions_UseRelativeLinks"></a> UseRelativeLinks

Whether to use relative links for all resources. <code>false</code> by default.

```csharp
public bool UseRelativeLinks { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Examples

<pre><code class="lang-csharp">options.UseRelativeLinks = true;</code></pre>

<pre><code class="lang-csharp">{
  "type": "articles",
  "id": "4309",
  "relationships": {
     "author": {
       "links": {
         "self": "/api/shopping/articles/4309/relationships/author",
         "related": "/api/shopping/articles/4309/author"
       }
     }
  }
}</code></pre>

