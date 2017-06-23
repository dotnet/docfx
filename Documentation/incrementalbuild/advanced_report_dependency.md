🔧 Advanced: Report Dependency
=================================

`DocFX Incremental Build Framework` provides the flexiblity to register customized dependency type and report dependency. In this topic, we will go through how to do that.

*Register Dependency Type*
---------------------------

### Dependency type

Property                 | Type                                 | Description
-----------------------  | ------------------------------------ | ------------------------------------------------------------------------------------------------------------------------
**Name**                 | string                               | name of the dependency type(it should be unique)
Phase                    | enum(`Compile` or `Link`)            | the build phase that this type of dependency could have an effect on.
Transitivity             | enum(`None` or `SameType` or `All`)  | whether the dependency is transitive, transitive upon the dependencies with same type, or transitive upon any dependency.


#### Reserved dependency types

Below table lists all reserved dependency types. When creating a customized dependency type, name shouldn't conflict with the reserved ones.

**Name**                 | Phase                 | Transitivity             | Description
-----------------------  | ----------------------| -------------------------| -----------------------------------------------------------------------
include                  | Compile               | All                      | file inclusion and code snippet
uid                      | Link                  | None                     | cross reference
file                     | Link                  | None                     | file link
overwrite                | Link                  | All                      | overwrite files
bookmark                 | Link                  | None                     | file link with bookmark
metadata                 | Link                  | None                     | metadata related dependency
reference                | Link                  | None                     | managed reference document's references

#### Register a customized dependency type

Plugins are flexible to register customized dependency types by implementing @Microsoft.DocAsCode.Plugins.ISupportIncrementalBuildStep interface's method `GetDependencyTypesToRegister`. The method would be called by the framework at the very start of the whole build.

Sample code:

```csharp
    public IEnumerable<DependencyType> GetDependencyTypesToRegister() => new[]
    {
        new DependencyType()
        {
            Name = "ref",
            Phase = BuildPhase.Link,
            Transitivity = DependencyTransitivity.None,
        }
    };
```

*Dependency item*
-------------------

### DependencyItem model

Property                 | Type                                                   | Description
-----------------------  | -------------------------------------------------------| ----------------------------
From                     | [DependencyItemSourceInfo](#dependencyitemsourceinfo-model)  | the depending one
To                       | [DependencyItemSourceInfo](#dependencyitemsourceinfo-model)  | the dependent one
Type                     | string                                                 | the dependency type name

### DependencyItemSourceInfo model

Property                 | Type            | Description
-----------------------  | ----------------| -----------------------------------------------------------
sourceType               | string          | the type of the value. `file` and `uid` are reserved types  
value                    | string          | value

### How to report

`IHostService` provides the interface to report directed/reversed dependency items.

If you want to report the dependency between an file and another file, you can use below method:

directed dependency: `void ReportDependencyTo(FileModel currentFileModel, string to, string type)`

reversed dependency: `void ReportDependencyFrom(FileModel currentFileModel, string from, string type)`

For example, i'd like to report a dependency: file `~/test.md`(filemodel is `a`) depends on file `~/../include/token/md`, dependency type is `include`,
i could call the method `ReportDependencyTo(a, "~/../include/token.md", "include")`.

Plugins are only allowed to report a dependency during `Compile` phase. However, some plugins might don't have enough info to resolve some dependency to file until the whole phase completes. `DocFX` provides the flexiblity that plugins could report dependency between items that are not files and resolve them later.

directed dependency: `void ReportDependencyTo(FileModel currentFileModel, string to, string toType, string type)`

reversed dependency: `void ReportDependencyFrom(FileModel currentFileModel, string from, string fromType, string type)`

report reference:    `void ReportReference(FileModel currentFileModel, string reference, string referenceType)`

A common usage is to report dependency between file and uid.

For example, i'd like to report a dependency: file `~/test.md`(filemodel is `a`) depends on sentenceId @Testid(filemodel is `b`), dependency type is `link`,
i could call the method `ReportDependencyTo(a, "Testid", "sentenceId", "link")` to report the dependency and `ReportReference(b, "Testid", "sentenceId")` to report the mapping between sentenceId and file. This way, the framework would do the resolution work and resolve it to file-file dependency.