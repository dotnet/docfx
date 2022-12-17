Doc-as-Code: Metadata Format Specification
==========================================

## 0. Introduction

### 0.1 Goals and Non-goals

1. The goal of this document is to define a general format to describe language metadata for programming languages.
2. The language metadata is designed to be language agnostic and support multiple programming language in a single metadata file.
3. The main user scenario for language metadata is to generate reference documentation, so this document will discuss how to optimize metadata format for documentation rendering.
4. This document does **NOT** discuss details of metadata format implementation of a specific programming language.

### 0.2 Terminology

The key words **MUST**, **MUST NOT**, **REQUIRED**, **SHALL**, **SHALL NOT**, **SHOULD**, **SHOULD NOT**, **RECOMMENDED**,  **MAY**, and **OPTIONAL** in this document are to be interpreted as described in [RFC 2119][1].

Words in *italic* indicate they are terms previously defined in this document.

## 1. Items and Identifiers

### 1.1 Items

*Item* is the basic unit of metadata format. From a documentation perspective, each *item* represents a "section" in the reference documentation. This "section" is the minimum unit that you can cross reference to, or customize in layout and content.

> When implementing the metadata format for your own language, you can decide which elements are *items*. For example, usually namespaces, classes, and methods are *items*. However, you can also make smaller elements such as parameters be items if you want them to be referenceable and customizable.

*Items* can be hierarchical. One *item* can have other *items* as *children*. For example, in C#, namespaces and classes can have classes and/or methods as *children*.

### 1.2 Identifiers

Each *item* has an identifier (ID) which is unique under its parent.

As we're targeting to support multiple languages, there are no restrictions as to which characters are not allowed in identifiers. However, to make identifiers easier to recognize and resolve in Markdown, it's not **RECOMMENDED** to have whitespaces in identifiers. Markdown processor **MAY** implement some algorithm to tolerate whitespaces in handwritten Markdown. (Leading and trailing spaces **MUST** be removed from identifier.)

Identifier **MUST** be treated as case-sensitive when comparing equality.

Each *item* has a unique identifier (UID) which is globally unique. A *UID* is defined as follows:
1. If an *item* does not have a *parent*, its *UID* is its *ID*.
2. Otherwise, its *UID* is the combination of the *UID* of its *parent*, a separator and the *ID* of the *item* itself.

Valid separators are `.`, `:`, `/` and `\`.

For example, for a class `String` under namespace `System`, its ID is `String` and UID is `System.String`.

> Given the above definition, an *item*'s UID **MUST** starts with the *UID* of its parent (and any of its ancestors) and ends with the *ID* of itself. This is useful to quickly determine whether an *item* is under another *item*.

### 1.3 Alias

*Identifier* could be very long, which makes it difficult to write by hand in Markdown. For example, it's easy to create a long *ID* in C# like this:

```markdown
Format(System.IFormatProvider,System.String,System.Object,System.Object)
```

We can create short *alias* for *items* so that they can be referenced easily.

*Alias* is same as *ID*, except:

1. It doesn't have to be unique.
2. One *item* can have multiple *aliases*.

> It's not **RECOMMENDED** to create an *alias* that has nothing to do with an *item's* *ID*. Usually an *item*'s *alias* is part of its *ID* so it's easy to recognize and memorize.  
> For example, for the case above, we usually create an alias `Format()`.

We can easily get a "global" alias for an *item* by replacing the *ID* part of its *UID* with its alias.

## 2. File Structure

### 2.1 File Format

You can use any file format that can represent structural data to store metadata. However, we recommend using [YAML][2] or [JSON][3]. In this document, we use YAML in examples, but all YAML can be converted to JSON easily.

### 2.2 File Layout

A metadata file consists of two parts: An "item" section and a "reference" section. Each section is a list of objects and each object is a key-value pair (hereafter referred to as "property") list that represents an *item*.

### 2.3 Item Section

Though *items* can be hierarchical, they are flat in an *item* section. Instead, each *item* has a "children" *property* indicating its *children* and a "parent" *property* indicating its parent.

An *item* object has some basic properties:

Property   | Description                                      
-----------|-------------------------------------------------
uid        | **REQUIRED**. The *unique identifier* of the *item*.
children   | **OPTIONAL**. A list of *UIDs* of the *item*'s children. Can be omitted if there are no *children*.
parent     | **OPTIONAL**. The *UID* of the *item*'s parent. If omitted, metadata parser will try to figure out its *parent* from the *children* information of other *items* within the same file.

Here is an example of a YAML format metadata file for C# Object class:

```yaml
items:
- uid: System.Object
  parent: System
  children:
  - System.Object.Object()
  - System.Object.Equals(System.Object)
  - System.Object.Equals(System.Object,System.Object)
  - System.Object.Finalize()
  - System.Object.GetHashCode()
  - System.Object.GetType()
  - System.Object.MemberwiseClone()
  - System.Object.ReferenceEquals()
  - System.Object.ToString()
- uid: System.Object.Object()
  parent: System.Object
- uid: System.Object.Equals(System.Object)
  parent: System.Object
- uid: System.Object.Equals(System.Object,System.Object)
  parent: System.Object
- uid: System.Object.Finalize()
  parent: System.Object
- uid: System.Object.GetHashCode()
  parent: System.Object
- uid: System.Object.GetType()
  parent: System.Object
- uid: System.Object.MemberwiseClone()
  parent: System.Object
- uid: System.Object.ReferenceEquals()
  parent: System.Object
- uid: System.Object.ToString()
  parent: System.Object
references:
...
```

> *Items* **SHOULD** be organized based upon how they will display in documentation. For example, if you want all members of a class be displayed in a single page, put all members in a single metadata file.

### 2.3 Item Object
In additional to the *properties* listed in last section, *item object* also has some **OPTIONAL** *properties*:

Property | Description
---------|-----------------------------------
id       | The *identifier* of the *item*.
alias    | A list of *aliases* of the *item*.
name     | The display name of the *item*.
fullName | The full display name of the *item*. In programming languages, it's usually the full qualified name.
type     | The type of the *item*, such as class, method, etc.
url      | If it's a relative URL, then it's another metadata file that defines the *item*. If it's an absolute URL, it means the *item* is coming from an external library, and the URL is the documentation page of this *item*. If omitted, the URL is the location of the current file.
source   | The source code information of the *item*. It's an object that contains following *properties*:<br>1. repo: the remote Git repository of the source code.<br>2. branch: the branch of the source code.<br>3. revision: the Git revision of the source code.<br>4. path: the path to the source code file where the *item* is defined.<br>5. startLine: the start line of the *item* definition.<br>6. endLine: the end line of the *item* definition.

Here is an example of a C# Dictionary class:

```yaml
- uid: System.Collections.Generic.Dictionary`2
  id: Dictionary`2
  alias:
  - Dictionary
  parent: System.Collections.Generic
  name: Dictionary<TKey, TValue>
  fullName: System.Collections.Generic.Dictionary<TKey, TValue>
  type: class
  url: System.Collections.Generic.Dictionary`2.yml
  source:
    repo: https://github.com/dotnet/netfx.git
    branch: master
    revision: 5ed47001acfb284a301260271f7d36d2bb014432
    path: src/system/collections/generic/dictionary.cs
    startLine: 1
    endLine: 100
```

### 2.4 Custom Properties

Besides the predefined *properties*, *item* can have its own *properties*. One restriction is *property* name **MUST NOT** contains dots, as dot in *property* name will have special meaning (described in later section).

### 2.5 Reference Section

The reference section also contains a list of *items*. These *items* serve as the references to *items* in the *item section* and won't show up in documentation. Also, a reference *item* doesn't need to have full *properties*, it just contains necessary information needed by its referrer (for example, name or URL).

In metadata file, all *items* **MUST** be referenced by *UID*.

> It's **RECOMMENDED** to include all referenced *items* in reference section. This makes the file self-contained and easy to render at runtime.
>
> Many programming languages have the concept of "template instantiation". For example, in C#, you can create a new type `List<int>` from `List<T>` with argument `int`. You can create a reference for "template instances". For example, for a class inherited from `List<int>`:

```yaml
items:
- uid: NumberList
  inherits:
  - System.Collections.Generic.List<System.Int32>
references:
- uid: System.Collections.Generic.List`1<System.Int32>
  link: @"System.Collections.Generic.List`1"<@"System.Int32">
- uid: System.Collections.Generic.List`1
  name: List
  url: system.collections.generic.list`1.yml
- uid: System.Int32
  name: int
  url: system.int32.yml
```

### 2.6 Multiple Language Support

An *item* may need to support multiple languages. For example, in .NET, a class can be used in C#, VB, managed C++ and F#. Different languages may have differences in *properties*. For example, a list of string is displayed as `List<string>` in C#, while `List(Of string)` in VB.

To support this scenario, we introduce a concept of language context to allow defining different *property* values in different languages.

If a *property* name is in the form of `property_name.language_name`, it defines the value of `property_name` under `language_name`. For example:

```yaml
- uid: System.Collections.Generic.Dictionary`2
  name.csharp: Dictionary<TKey, TValue>
  name.vb: Dictionary(Of TKey, TValue)
```

This means the name of dictionary is `Dictionary<TKey, TValue>` in C# and `Dictionary(Of TKey, TValue)` in VB.

The following *properties* **SHALL NOT** be overridden in language context: uid, id, alias, children, and parent.

## 3. Work with Metadata in Markdown 

### 3.1 YAML Metadata Section

In a Markdown file, you can also define *items* using the same metadata syntax. The metadata definition **MUST** be in YAML format and enclosed by triple-dash lines (`---`).
Here is an example:

```markdown
---
uid: System.String
summary: String class
---

This is a **string** class.
```

You can have multiple YAML sections inside a single Markdown file, but in a single YAML section, there **MUST** be only one *item*.

The YAML metadata section does not have to contain all *properties*. The only *property* that **MUST** appear is "uid", which is used to match the same *item* in metadata file.

The most common scenario for using YAML section is to specify which *item* the markdown doc belongs to. But you can also overwrite *item* *property* by defining one with the same name in YAML section. In the above example, the *property* "summary" will overwrite the same one in metadata.

As with language context, the following *properties* **SHALL NOT** be overridden: uid, id, alias, children, and parent.

You **SHALL NOT** define new *item* in Markdown.

### 3.2 Reference Items in Markdown

To cross reference an *item*, you can use URI with `xref` scheme. You can either use [standard link](https://daringfireball.net/projects/markdown/syntax#link) or [automatic link](https://daringfireball.net/projects/markdown/syntax#autolink) with the above URI.
For example, to cross reference `System.String`:
```markdown
[System.String](xref:System.String)

<xref:System.String>
```

> Since *item* reference is a URI, special characters (like `#`, `?`) **MUST** be [encoded](https://tools.ietf.org/html/rfc3986#section-2.1).

We also introduce a shorthand markdown syntax to cross reference easily:

If a string starts with `@`, and followed by a string enclosed by quotes `'` or double quotes `"`, it will be treated as an *item* reference. The string inside `""` or `''` is the *UID* of the *item*. Here is one example:

```markdown
@"System.String"
```

> Markdown processor **MAY** implement some algorithm to allow omit curly braces if *ID* is simple enough. For example, For reference like `@"int"`, we may also want to allow `@int`.

When rendering references in Markdown, they will expand into a link with the *item*'s name as link title. You can also customize the link title using the standard syntax of Markdown:

```markdown
[Dictionary](xref:System.Collections.Generic.Dictionary`2)<[String](xref:System.String), [String](xref:System.String)>
```

Will be rendered to:
[Dictionary](xref:System.Collections.Generic.Dictionary`2)<[String](xref:System.String), [String](xref:System.String)>

Besides *UID*, we also allow referencing items using *ID* and *alias*, in the Markdown processor, the below algorithm **SHOULD** be implemented to resolve references.

Check whether the reference matches:

1. Any *identifier* of current *item*'s children.
2. Any *alias* of current *item*'s children.
3. Any *identifier* of current *item*'s silbings.
4. Any *alias* of current *item*'s silbings.
5. A *UID*.
6. A *global alias*.

[1]: https://www.ietf.org/rfc/rfc2119.txt
[2]: http://www.yaml.org/
[3]: http://www.json.org/
