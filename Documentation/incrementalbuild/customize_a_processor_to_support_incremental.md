Walkthrough: Customize a processor to support incremental build
================================================================

During this tutorial, we'll walk through the steps to enable a processor to be incremental.

Step1. Implement @Microsoft.DocAsCode.Plugins.ISupportIncrementalDocumentProcessor interface for the processor
------------------------------------------------------------------------------------

```csharp
public class RtfDocumentProcessor : ISupportIncrementalDocumentProcessor
{
    // to-do: implements IDocumentProcessor

    public virtual string GetIncrementalContextHash()
    {
        // to-do: context related hash. if it changes, incremental build isn't triggered.
    }

    public virtual void SaveIntermediateModel(FileModel model, Stream stream)
    {
        // to-do: the logic to store filemodel 
    }

    public virtual FileModel LoadIntermediateModel(Stream stream)
    {
        // to-do: the logic to load filemodel
    }
}
```

Step2. Implement @Microsoft.DocAsCode.Plugins.ISupportIncrementalBuildStep interface for all the plugins plugged in the processor
------------------------------------------------------------------------------------------------------
Plugins are flexible to register customized dependency types by implementing the interface's method @Microsoft.DocAsCode.Plugins.ISupportIncrementalBuildStep.GetDependencyTypesToRegister.

Plugins are also flexible to report dependencies by invoking the methods provided by @Microsoft.DocAsCode.Plugins.IHostService.

```csharp
public class RtfBuildStep : ISupportIncrementalBuildStep
{
    // to-do: implements IDocumentBuildStep

    public bool CanIncrementalBuild(FileAndType fileAndType) => true;

    public string GetIncrementalContextHash() => null;

    public IEnumerable<DependencyType> GetDependencyTypesToRegister() => new[]
    {
        new DependencyType()
        {
            Name = "ref",
            Phase = BuildPhase.Link,
            Transitivity = DependencyTransitivity.None,
        }
    };

    public override void Build(FileModel model, IHostService host)
    {
        //.....
        host.ReportDependencyTo(model, "uid", DependencyItemSourceType.Uid, "ref");
    }
}
```

The above sample registered a dependency type named `ref`, this type of dependency applies during `Link` phase and it isn't transitive. `DocFX` has some reserved dependency types, you can refer to [Reserved Dependency Types](advanced_report_dependency.md#reserved-dependency-types) for more details.

In `Build` step, this plugin reports dependencies of type `ref` by invoking @Microsoft.DocAsCode.Plugins.IHostService 's `ReportDependencyTo` method. @Microsoft.DocAsCode.Plugins.IHostService also provides `ReportDependencyFrom` method you can report reverse dependency.

For more details about how to register your own dependency types and report , you can refer to [Advanced: register and report dependency](advanced_report_dependency.md).


Step3. [Optional]Implement @Microsoft.DocAsCode.Plugins.ICanTraceContextInfoBuildStep interface for plugins that need to access context info
-----------------------------------------------------------------------------------------------------------------------
When building articles, some plugins might need the info of unloaded articles. Incremental Build Framework provides the interface @Microsoft.DocAsCode.Plugins.ICanTraceContextInfoBuildStep, which is the superset of @Microsoft.DocAsCode.Plugins.ISupportIncrementalBuildStep and also contains methods to save/load context info.

```csharp
public class RtfBuildStep : ICanTraceContextInfoBuildStep
{
    // to-do: implements ISupportIncrementalBuildStep

    public void LoadContext(Stream stream)
    {
        // to-do: the logic to load last context info
    }

    public void SaveContext(Stream stream)
    {
        // to-do: the logic to save current context info
    }
}
```


Now you're done! Your processor can run incrementally!