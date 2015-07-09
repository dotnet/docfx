Doc-as-Code: Map file Format Specification
==========================================

0. Introduction
---------------
There are 2 kinds of *map files* in *doc-as-code* project. One is *.yml.map file*, and the other is *.md.map file*.

*.yml.map file* is an intermediate file format defined by *doc-as-code* project, to maintain a mapping between *.yml metadata file*s and *related files*. *Related files* **MAY** contain *markdown file*s, *code snippet file*s and *.yml metadata file*s. 

*.md.map file* is an intermediate file format to maintain a mapping between *markdown file*s and *related files*. *Related files* **MAY** contain *markdown file*s, *code snippet file*s and *.yml metadata file*s. 

*Markdown file* is file written in markdown format. It can use a specific syntax as defined in [metadata_format_spec.md]() that **links** and **overrides** *items* with the same Yaml metadata syntax wrapped by triple-dash lines (`---`), an example as follows:
	```markdown
	---
	uid: System.String
	summary: String class
	---
	Here followes the description for System.String
	```
*Code snippet file* is file with sample code inside it.
*.yml metadata file* is metadata file in YAML format as generated from source code by *doc-as-code* project.

1. *.yml.map* File Format
--------------
*<id>.yml.map* file is **ALWAYS** the map file for *<id>.yml*. The format of *<id>.yml* is defined in [metadata_format_spec.md](). The map file is a key-value pair collection, where *key* is the *uid* from *<id>.yml*, and *value* is an object, the properties of *value* are described in detail in following sections.

### 1.1 Basic Properties for *Value*
The following table describes some basic properties for *value*.

Property   | Description                                      
-----------|-------------------------------------------------
id         | **REQUIRED**. The *unique identifier* as the same as *key*.
path       | **REQUIRED**. The path for the **markdown** file as relative to the **ROOT** code base directory
href       | **REQUIRED**. The relative path for the **markdown** file to **current** *.yml.map file*.
startLine  | **REQUIRED**. The start line for the description to current *id*.
endLine    | **OPTIONAL**. The end line for the description to current *id*. Can be omitted if is the end of the markdown file.
remote     | **OPTIONAL**. The remote source repository info for the markdown file. Can be omitted if is the same as defined in *<id>.yml*.
references | **OPTIONAL**. An array of current item's references. Can be omitted if there is no reference. The properties for reference is described in following section.

#### 1.1.1 Reference Properties
References keeps a similar property list as the basic properties with *references* property excluded.
In general there are 2 types of references in a *.yml.map file*.

##### 1. *Code snippet* reference 
*Code snippet file* is referenced with syntax `{{filepath[startline-endline]}}`, in which `startline` and `endline` can be omitted. For example, if writer wants to include a code snippet file Class1.cs, with line from 10 to 20, to markdown file A.md, then the syntax should be: `{{Class1.cs[10-20]}}`. Correspondingly an item should be added to *references* part as follows:

```yaml
references:
    - ....
	- id: {{CLass1.cs[10-20]}}
	  path: Class1.cs
      startLine: 10
      endLine: 20
    - ....
```

##### 2. *Metadata* reference 
*Metadata* is referenced with syntax `@{uid}`, in which `uid` is the *uid* of an *item*. for example, if writer wants to reference to Namespace1.Class1.Method1 defined in Class1.yml, the syntax should be: `@{Namespace1.Class1.Method1}`. Correspondingly an item should be added to *references* part as follows:

```yaml
references:
    - ....
	- id: @{Namespace1.Class1.Method1}
	  path: Class1.yml
    - ....
```

### 1.2 Additional Properties for *Value* in *.yml.map*
Beside basic properties, there are some additional properties designed specifically for *.yml.map* file, as listed below: 
Property   | Description                                      
-----------|-------------------------------------------------
override   | **OPTIONAL**. If the *markdown file* overrides metadata properties inside *YAML HEADER*, the overriden properties will be listed in this property, keeping the same layout as inside *YAML HEADER*.

#### 1.2.1 Property *override*
For example, the *markdown file* overrides *remarks* property in *YAML HEADER* using:
	```markdown
	---
	uid: System.String
	remarks: String class
	---
	Here followes the description for System.String
	```
The *.yml.map* file will appear like:

```yaml
override:
	remarks: String class
```

*NOTE* that the following *properties* **SHALL NOT** be overridden: id, alias, children, parent. If these mentioned *properties* exist in *YAML HEADER*, the value will be simply **IGNORED**.

2. *.md.map* File Format
--------------
*<name>.md.map* file is **ALWAYS** the map file for *<name>.md*. The map file is a key-value pair collection, where the key is *<name>.md*, and value is similar as what described in *1.1 Basic Properties for Value*, exceptions are listed below. Actually *.md.map* file always contains at most 1 key-value pair. We keep it a key-value pair collection pattern to be consistent with *.yml.map* file.

Property   | Description                                      
-----------|-------------------------------------------------
id         | **REQUIRED**. The same as *key*.
path       | **REQUIRED**. The path for the **markdown** file as relative to the **ROOT** code base directory
href       | **OPTIONAL**. The relative path to **current** *.md.map file*. It can usually be omitted when it is current *markdown file* path. There is chance when the *markdown file* get **preprocessed** and in such case, this property should be specified.
startLine  | **OPTIONAL**. The start line for the *markdown file*. Can be omitted if is the start of the markdown file.
endLine    | **OPTIONAL**. The end line for the description to current *id*. Can be omitted if is the end of the markdown file.