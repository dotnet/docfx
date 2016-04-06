How-to: Filter Out Unwanted APIs 
================================

A filter configuration file is in [YAML](http://www.yaml.org/spec/1.2/spec.html) format. You could filter out unwanted APIs by providing a filter configuration file and specifying its path.

Specifying filter configuration file path
-----------------------------------------

The path of the configuration file could be specified by the following two ways. Option 1 could overwrite option 2.

1. docfx.exe metadata command argument.

   `docfx.exe metadata -filter <path relative to baseDir or absolutepath>`

2. docfx.json metadata section `filter` property.
 
   **Sample**
   ```
   {
     "metadata": [
       {
         "src": [
           {
             "files": [
               "src/**.csproj"
             ],
             "exclude": [
               "**/bin/**",
               "**/obj/**"
             ]
           }
         ],
         "dest": "obj/api",
         "filter": "filterConfig.yml"
       }
     ]
   }
   ```

The format of the filter configuration file
-------------------------------------------

You could specify `apiRules` with a list of `exclude` or `include` rules.

> *Note*

> The rules would be executed sequentially and the matching process would stop once one rule is matched. 
> Namely, you need to put the most detailed rule in the top.

> If no rule is matched, the API would be included by default.


### 1. `exclude` or `include` APIs by matching their uid with the Regex `uidRegex`.  
  
The below sample would exclude all APIs whose uid start with 'Microsoft.DevDiv' except those that start with 'Microsoft.DevDiv.SpecialCase'.
 
```
  - include:
      uidRegex: ^Microsoft\.DevDiv\.SpecialCase
  - exclude:
      uidRegex: ^Microsoft\.DevDiv
```

### 2. `exclude` or `include` APIs by matching its `type`, this is often combined with `uidRegex`.  
  
Supported `type`:
 * `Namespace`
 * `Type`
 * `Class`
 * `Struct`
 * `Enum`
 * `Interface`
 * `Delegate`
 * `Member`
 * `Event`
 * `Field`
 * `Method`
 * `Property`
  
> *Note*
  
> `Type` could be `Class`, `Struct`, `Enum`, `Interface`, or `Delegate`. `Member` could be `Event`, `Field`, `Method`, or `Property`.
  
> `Namespace` is flattened. Namely, excluding namespace 'A.B' has nothing to do with namespace 'A.B.C'.
  
> If a namespace is excluded, all types/members defined in the namespace would be excluded, too.
> If a type is excluded, all members defined in the type would be excluded, too.
  
The below sample would exclude all APIs whose uid start with 'Microsoft.DevDiv' and type is `Type`, namely `Class`, `Struct`,
`Enum`, `Interface`, or `Delegate`.
  
```
  - exclude:
      uidRegex: ^Microsoft\.DevDiv
      type: Type
```
  
### 3. `exclude` or `include` APIs by containing matched attributes.
  
You can specify an attribute by its `uid`, `ctorArguments` and `ctorNamedArguments`.
  
> *Note*

> `ctorArguments` requires a full match of the attribute's constructor arguments. While `ctorNamedArguments` support a partial match.
> Namely, `ctorArguments` should contain all the arguments while `ctorNamedArguments` could contain a subset of the named arguments. 
  
The below sample would exclude all APIs which have EditorBrowsableAttribute and its constructor argument is EditorBrowsableState.Never.
  
```
  - exclude:
      hasAttribute:
        uid: System.ComponentModel.EditorBrowsableAttribute
        ctorArguments:
        - System.ComponentModel.EditorBrowsableState.Never
```
  
A complete **Sample** of filter configuration file 

```yaml
apiRules:
- exclude:
    uidRegex: ^Microsoft\.TeamFoundation\.WorkItemTracking\.Proxy\.IRowSetsNative$
- exclude:
    uidRegex: ^Microsoft\.TeamFoundation\.WorkItemTracking\.Proxy\.MetadataRowSetsNative$
- exclude:
    uidRegex: ^Microsoft\.TeamFoundation\.WorkItemTracking\.Proxy\.RowSet\.Columns.*$
    type: Member
- exclude:
    uidRegex: ^Microsoft\.TeamFoundation\.WorkItemTracking\.Proxy\.RowSetColumn\.Name.*$
    type: Member
- exclude:
    hasAttribute:
      uid: System.ComponentModel.EditorBrowsableAttribute
      ctorArguments:
      - System.ComponentModel.EditorBrowsableState.Never
```
