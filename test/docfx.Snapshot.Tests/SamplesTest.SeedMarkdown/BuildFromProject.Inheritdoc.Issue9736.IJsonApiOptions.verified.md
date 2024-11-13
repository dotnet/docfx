# <a id="BuildFromProject_Inheritdoc_Issue9736_IJsonApiOptions"></a> Interface Inheritdoc.Issue9736.IJsonApiOptions

Namespace: [BuildFromProject](BuildFromProject.md)  
Assembly: BuildFromProject.dll  

```csharp
public interface Inheritdoc.Issue9736.IJsonApiOptions
```

## Properties

### <a id="BuildFromProject_Inheritdoc_Issue9736_IJsonApiOptions_UseRelativeLinks"></a> UseRelativeLinks

Whether to use relative links for all resources. <code>false</code> by default.

```csharp
bool UseRelativeLinks { get; }
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

