How-to: Filter Out Unwanted APIs or Attributes 
================================

A filter configuration file is in [YAML](http://www.yaml.org/spec/1.2/spec.html) format. You may filter out unwanted APIs or attributes by providing a filter configuration file and specifying its path.

Specifying the filter configuration file path
-----------------------------------------

The path of the configuration file is specified in the following two ways. Option 1 could overwrite option 2.

1. docfx.exe metadata command argument.

   `docfx.exe metadata --filter <path relative to baseDir or absolutepath>`

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
   
DocFX has a [default filter configuration](#3-default-filter-configuration). If the user doesn't specify the filter configuration file path, default filter configuration would be used. Otherwise, user provided filter configuration would merge with the default one. If there is a conflict, user specified would overwrite the default one.


The format of the filter configuration file
-------------------------------------------

### 1. API Filter Rules

To filter out APIs, you could specify `apiRules` with a list of `exclude` or `include` rules.

> [!Note]
> The rules would be executed sequentially and the matching process would stop once one rule is matched.
> Namely, you need to put the most detailed rule in the top.
> If no rule is matched the API would be included by default.


#### 1) `exclude` or `include` APIs by matching their uid with the Regex `uidRegex`.  
  
The sample below excludes all APIs whose uids start with 'Microsoft.DevDiv' except those that start with 'Microsoft.DevDiv.SpecialCase'.
 
```
  - include:
      uidRegex: ^Microsoft\.DevDiv\.SpecialCase
  - exclude:
      uidRegex: ^Microsoft\.DevDiv
```

#### 2) `exclude` or `include` APIs by matching its `type`, this is often combined with `uidRegex`.  
  
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
  
> [!Note]
> `Type` could be `Class`, `Struct`, `Enum`, `Interface`, or `Delegate`. `Member` could be `Event`, `Field`, `Method`, or `Property`.
>
> `Namespace` is flattened. Namely, excluding namespace 'A.B' has nothing to do with namespace 'A.B.C'.

> [!Important]
>
> If a namespace is excluded, all types/members defined in the namespace would also be excluded.
> If a type is excluded, all members defined in the type would also be excluded.
  
The below sample would exclude all APIs whose uid starts with 'Microsoft.DevDiv' and type is `Type`, namely `Class`, `Struct`,
`Enum`, `Interface`, or `Delegate`.
  
```
  - exclude:
      uidRegex: ^Microsoft\.DevDiv
      type: Type
```
  
#### 3) `exclude` or `include` APIs by containing matched attributes.
  
You can specify an attribute by its `uid`, `ctorArguments` and `ctorNamedArguments`.
  
> [!Note]
>
> `ctorArguments` requires a full match of the attribute's constructor arguments, while `ctorNamedArguments` supports a partial match.
> Namely, `ctorArguments` should contain all the arguments while `ctorNamedArguments` could contain a subset of the named arguments. 
  
The sample below excludes all APIs which have EditorBrowsableAttribute and its constructor argument is EditorBrowsableState.Never.
  
```
  - exclude:
      hasAttribute:
        uid: System.ComponentModel.EditorBrowsableAttribute
        ctorArguments:
        - System.ComponentModel.EditorBrowsableState.Never
```

The sample below excludes all APIs which have AttributeUsageAttribute and its constructor argument is AttributeTargets.Class
 and its constructor has named argument [Inherited] = true

```
  - exclude:
    hasAttribute:
      uid: System.AttributeUsageAttribute
      ctorArguments:
      - System.AttributeTargets.Class
      ctorNamedArguments:
        Inherited: "true"
```

A complete **Sample** of the filter configuration file for filtering out APIs follows:

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

### 2. Attribute Filter Rules

To filter out Attributes, you could specify `attributeRules` with a list of `exclude` or `include` rules.

The rules are similar to API filter. Please refer to [API Filter Rules](#1-api-filter-rules) section.

### 3. Default Filter Configuration

```yaml
apiRules:
- exclude:
    hasAttribute:
      uid: System.ComponentModel.EditorBrowsableAttribute
      ctorArguments:
      - System.ComponentModel.EditorBrowsableState.Never
attributeRules:
- exclude:
    uidRegex: ^System\.ComponentModel\.Design$
    type: Namespace
- exclude:
    uidRegex: ^System\.ComponentModel\.Design\.Serialization$
    type: Namespace
- exclude:
    uidRegex: ^System\.Xml\.Serialization$
    type: Namespace
- exclude:
    uidRegex: ^System\.Web\.Compilation$
    type: Namespace
- exclude:
    uidRegex: ^System\.Runtime\.Versioning$
    type: Namespace
- exclude:
    uidRegex: ^System\.Runtime\.ConstrainedExecution$
    type: Namespace
- exclude:
    uidRegex: ^System\.EnterpriseServices$
    type: Namespace
- exclude:
    uidRegex: ^System\.Diagnostics\.CodeAnalysis$
    type: Namespace
- include:
    uidRegex: ^System\.Diagnostics\.(ConditionalAttribute|EventLogPermissionAttribute|PerformanceCounterPermissionAttribute)$
    type: Type
- exclude:
    uidRegex: '^System\.Diagnostics\.[^.]+$'
    type: Type
- include:
    uidRegex: ^System\.ComponentModel\.(BindableAttribute|BrowsableAttribute|ComplexBindingPropertiesAttribute|DataObjectAttribute|DefaultBindingPropertyAttribute|ListBindableAttribute|LookupBindingPropertiesAttribute|SettingsBindableAttribute|TypeConverterAttribute)$
    type: Type
- exclude:
    uidRegex: '^System\.ComponentModel\.[^.]+$'
    type: Type
- exclude:
    uidRegex: ^System\.Reflection\.DefaultMemberAttribute$
    type: Type
- exclude:
    uidRegex: ^System\.CodeDom\.Compiler\.GeneratedCodeAttribute$
    type: Type
- include:
    uidRegex: ^System\.Runtime\.CompilerServices\.ExtensionAttribute$
    type: Type
- exclude:
    uidRegex: '^System\.Runtime\.CompilerServices\.[^.]+$'
    type: Type
- include:
    uidRegex: ^System\.Runtime\.InteropServices\.(ComVisibleAttribute|GuidAttribute|ClassInterfaceAttribute|InterfaceTypeAttribute)$
    type: Type
- exclude:
    uidRegex: '^System\.Runtime\.InteropServices\.[^.]+$'
    type: Type
- include:
    uidRegex: ^System\.Security\.(SecurityCriticalAttribute|SecurityTreatAsSafeAttribute|AllowPartiallyTrustedCallersAttribute)$
    type: Type
- exclude:
    uidRegex: '^System\.Security\.[^.]+$'
    type: Type
- include:
    uidRegex: ^System\.Web\.UI\.(ControlValuePropertyAttribute|PersistenceModeAttribute|ValidationPropertyAttribute|WebResourceAttribute|TemplateContainerAttribute|ThemeableAttribute|TemplateInstanceAttribute)$
    type: Type
- exclude:
    uidRegex: '^System\.Web\.UI\.[^.]+$'
    type: Type
- include:
    uidRegex: ^System\.Windows\.Markup\.(ConstructorArgumentAttribute|DesignerSerializationOptionsAttribute|ValueSerializerAttribute|XmlnsCompatibleWithAttribute|XmlnsDefinitionAttribute|XmlnsPrefixAttribute)$
    type: Type
- exclude:
    uidRegex: '^System\.Windows\.Markup\.[^.]+$'
    type: Type
```
