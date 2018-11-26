Overwrite Files
===============================

Introduction
--------------
DocFX supports processing Markdown files, as well as structured data model in YAML or JSON format.

We call Markdown files *Conceptual File*s, and the structured data model files *Metadata File*s.

Current supported *Metadata File*s include:
1. YAML files presenting managed reference model following [Metadata Format for .NET Languages](../spec/metadata_dotnet_spec.md).
2. Swagger JSON files presenting Swagger REST API model following [Swagger Specification Version 2.0](http://swagger.io/specification).

Inside DocFX, both *Conceptual File*s and *Metadata File*s are represented as *Model*s with different properties. Details on *Model* structure for these files are described in [Data model inside DocFX](#data-model-inside-docfx) section.

DocFX introduces the concept of *Overwrite File* to modify or add properties to *Model*s without changing the input *Conceptual File*s and *Metadata File*s.

The format of *Overwrite File*s
-----------------
*Overwrite File*s are Markdown files with multiple *Overwrite Section*s starting with YAML header block. A valid YAML header for an *Overwrite Section* *MUST* take the form of valid [YAML](http://www.yaml.org/spec/1.2/spec.html) set between triple-dashed lines and start with property `uid`. Here is a basic example of an *Overwrite Section*:

```md
---
uid: microsoft.com/docfx/Contacts
some_property: value
---
Further description for `microsoft.com/docfx/Contacts`
```

Each *Overwrite Section* is transformed to *Overwrite Model* inside DocFX. For the above example, the *Overwrite Model* represented in YAML format is:

```yaml
uid: microsoft.com/docfx/Contacts
some_property: value
conceptual: <p><b>Content</b> in Markdown</p>
```

### Anchor `*content`

`*content` is the keyword invented and used specifically in *Overwrite File*s to represent the Markdown content following YAML header. We leverage [Anchors](http://www.yaml.org/spec/1.2/spec.html#id2765878) syntax in YAML specification for `*content`.

The value for `*content` is always transformed from Markdown content to HTML. When `*content` is not used, the Markdown content below YAML header will be set to `conceptual` property; When `*content` is used, the Markdown content below YAML header will no longer be set to `conceptual` property. With `*content`, we can easily add Markdown content to any properties. 

```md
---
uid: microsoft.com/docfx/Contacts
footer: *content
---
Footer for `microsoft.com/docfx/Contacts`
```

In the above example, the value for `*content` is `<p>Footer for <code>microsoft.com/docfx/Contacts</code></p>`, and the *Overwrite Model* represented in YAML format is:

```yaml
uid: microsoft.com/docfx/Contacts
footer: <p>Footer for <code>microsoft.com/docfx/Contacts</code></p>
```

`uid` for an *Overwrite Model* stands for the Unique IDentifier of the *Model* it will overwrite. So it is allowed to have multiple *Overwrite Section*s with YAML Header containing the same `uid`. For one *Overwrite File*, the latter *Overwrite Section* overwrites the former one with the same `uid`. For different *Overwrite File*s, the order of overwrite is **Undetermined**. So it is suggested to have *Overwrite Sections* with the same `uid` in the same *Overwrite File*.

> [!NOTE]
>
> Multiple *Overwrite Section*s in one file doesn't work in markdig markdown engine. You should remove `"markdownEngineName": "markdig",` from `docfx.json` to support this feature.

When processing *Conceptual File*s and *Metadata File*s, *Overwrite Model*s with the same `uid` are applied to the processed *Model*s. Different *Model*s have different overwrite principles, [Overwrite principles](#overwrite-principles) section describes the them in detail.

Apply *Overwrite File*s
-----------------------
Inside `docfx.json`, [`overwrite`](../tutorial/docfx.exe_user_manual.md#32-properties-for-build) is used to specify the *Overwrite File*s.

Overwrite principles
-----------------
As a general principle, `uid` is always the key that an *Overwrite Model* find the *Model* it is going to overwrite. So a *Model* with no `uid` defined will never get overwritten.

Different types of files produce different *Model*s. The quickest way to get an idea of what the *Model* looks like is to run:
```
docfx build --exportRawModel
```
`--exportRawModel` exports *Model* in JSON format with `.raw.json` extension.

The basic principle of *Overwrite Model* is:
1. It keeps the same data structure as the *Model* it is going to overwrite
2. If the property is defined in *Model*, please refer [Data model inside DocFX](#data-model-inside-docfx) for the specific overwrite behavior for a specific property.
3. If the property is not defined in *Model*, it is added to *Model*

Data model inside DocFX
-----------------------
### Managed reference model


|        Key        |           Type            | Overwrite behavior |
|-------------------|---------------------------|--------------------|
|      **uid**      |            uid            |     Merge key.     |
|    assemblies     |         string[]          |      Ignore.       |
|    attributes     | [Attribute](#attribute)[] |      Ignore.       |
|     children      |           uid[]           |      Ignore.       |
|   documentation   |     [Source](#source)     |       Merge.       |
|      example      |         string[]          |      Replace.      |
|    exceptions     | [Exception](#exception)[] | Merge keyed list.  |
|     fullName      |          string           |      Replace.      |
|  fullName.<lang>  |          string           |      Replace.      |
|        id         |          string           |      Replace.      |
|    implements     |           uid[]           |      Ignore.       |
|    inheritance    |           uid[]           |      Ignore.       |
| inheritedMembers  |           uid[]           |      Ignore.       |
|       isEii       |          boolean          |      Replace.      |
| isExtensionMethod |          boolean          |      Replace.      |
|       langs       |         string[]          |      Replace.      |
| modifiers.<lang>  |         string[]          |      Ignore.       |
|       name        |          string           |      Replace.      |
|    name.<lang>    |          string           |      Replace.      |
|     namespace     |            uid            |      Replace.      |
|    overridden     |            uid            |      Replace.      |
|      parent       |            uid            |      Replace.      |
|     platform      |         string[]          |      Replace.      |
|     *remarks*     |         markdown          |      Replace.      |
|        see        |  [LinkInfo](#linkinfo)[]  | Merge keyed list.  |
|      seealso      |  [LinkInfo](#linkinfo)[]  | Merge keyed list.  |
|      source       |     [Source](#source)     |       Merge.       |
|     *syntax*      |     [Syntax](#syntax)     |       Merge.       |
|     *summary*     |         markdown          |      Replace.      |
|       type        |          string           |      Replace.      |

#### Source

Property                | Type                       | Overwrite behavior
----------------------- | -------------------------- | ------------------
base                    | string                     | Replace.
content                 | string                     | Replace.
endLine                 | integer                    | Replace.
id                      | string                     | Replace.
isExternal              | boolean                    | Replace.
href                    | string                     | Replace.
path                    | string                     | Replace.
remote                  | [GitSource](#gitsource)    | Merge.
startLine               | integer                    | Replace.

#### GitSource

Property                | Type                       | Overwrite behavior
----------------------- | -------------------------- | ------------------
path                    | string                     | Replace.
branch                  | string                     | Replace.
repo                    | url                        | Replace.
commit                  | [Commit](#commit)          | Merge.
key                     | string                     | Replace.

#### Commit

Property                | Type                       | Overwrite behavior
----------------------- | -------------------------- | ------------------
committer               | [User](#user)              | Replace.
author                  | [User](#user)              | Replace.
id                      | string                     | Replace.
message                 | string                     | Replace.

#### User

Property                | Type                       | Overwrite behavior
----------------------- | -------------------------- | ------------------
name                    | string                     | Replace.
email                   | string                     | Replace.
date                    | datetime                   | Replace.

#### Exception

Property                | Type                       | Overwrite behavior
----------------------- | -------------------------- | ------------------
**type**                | uid                        | Merge key.
*description*           | markdown                   | Replace.
commentId               | string                     | Ignore.

#### LinkInfo

Property                | Type                       | Overwrite behavior
----------------------- | -------------------------- | ------------------
**linkId**              | uid or href                | Merge key.
*altText*               | markdown                   | Replace.
commentId               | string                     | Ignore.
linkType                | enum(`CRef` or `HRef`)     | Ignore.

#### Syntax

|    Property    |           Type            | Overwrite behavior |
|----------------|---------------------------|--------------------|
|    content     |          string           |      Replace.      |
| content.<lang> |          string           |      Replace.      |
|   parameters   | [Parameter](#parameter)[] | Merge keyed list.  |
| typeParameters | [Parameter](#parameter)[] | Merge keyed list.  |
|     return     |  [Parameter](#parameter)  |       Merge.       |

#### Parameter

Property                | Type                       | Overwrite behavior
----------------------- | -------------------------- | ------------------
**id**                  | string                     | Merge key.
*description*           | markdown                   | Replace.
attributes              | [Attribute](#attribute)[]  | Ignore.
type                    | uid                        | Replace.

#### Attribute

Property                | Type                              | Overwrite behavior
----------------------- | --------------------------------- | ------------------
arguments               | [Argument](#argument)[]           | Ignore.
ctor                    | uid                               | Ignore.
namedArguments          | [NamedArgument](#namedargument)[] | Ignore.
type                    | uid                               | Ignore.

#### Argument

Property                | Type                      | Overwrite behavior
----------------------- | ------------------------- | ------------------
type                    | uid                       | Ignore.
value                   | object                    | Ignore.

#### NamedArgument

Property                | Type                      | Overwrite behavior
----------------------- | ------------------------- | ------------------
name                    | string                    | Ignore.
type                    | string                    | Ignore.
value                   | object                    | Ignore.


### REST API model

Key | Type | Overwrite behavior
--- | --- | ---
*children* | [REST API item model](#rest-api-item-model) | Overwrite when *uid* of the item model matches
*summary* | string | Overwrite
*description* | string | Overwrite

#### REST API item model

Key | Type | Overwrite behavior
--- | --- | ---
*uid* | string | Key

### Conceptual model

|     Key      |  Type  | Overwrite behavior |
|--------------|--------|--------------------|
|   *title*    | string |     Overwrite      |
|  *rawTitle*  | string |     Overwrite      |
| *conceptual* | string |     Overwrite      |

