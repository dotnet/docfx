# Walkthrough: Customize a post processor to be incremental

In this tutorial, we'll walk through how to enable a post processor to be incremental.

## Implement @Microsoft.DocAsCode.Plugins.ISupportIncrementalPostProcessor for the post processor

```csharp
public class AppendIntegerPostProcessor : ISupportIncrementalPostProcessor
{
    // to-do: implements IPostProcessor

    public IPostProcessorHost PostProcessorHost { get; set; }

    public string GetIncrementalContextHash()
    {
        // to-do: incremental context hash. If it changes, incremental post processing isn't triggered.
    }
}
```

## Optional: Load and save customized context information from cache

@Microsoft.DocAsCode.Plugins.IPostProcessorHost is the host to provide incremental post processing information as following.

Property                     | Type                    | Description
---------------------        | ---------------------   | ---------------------
SourceFileInfos              | List of @Microsoft.DocAsCode.Plugins.SourceFileInfo | Information of source files
ShouldTraceIncrementalInfo   | bool                  | Whether the post processor should trace incremental information
IsIncremental                | bool                  | Whether the post processor can be incremental

@Microsoft.DocAsCode.Plugins.IPostProcessorHost can also load and save customized context information per post processor in incremental cache.

Method                     | Return Type         | Description
---------------------      | ---------------     | ---------------------
LoadContextInfo()          | Stream              | Load context information from last post processing
SaveContextInfo()          | Stream              | Save context information to current post processing

Here's the sample:
```csharp
public class AppendIntegerPostProcessor : ISupportIncrementalPostProcessor
{
    public IPostProcessorHost PostProcessorHost { get; set; }

    public string GetIncrementalContextHash() { return string.Empty; }

    public Manifest Process(Manifest manifest, string outputFolder)
    {
        string contextInfo = string.Empty;
        var stream = PostProcessorHost.LoadContextInfo();
        if (stream != null)
        {
            using (var sr = new StreamReader(stream))
            {
                contextInfo = sr.ReadToEnd();
            }
        }

        using (var saveStream = PostProcessorHost.SaveContextInfo())
        using (var sw = new StreamWriter(saveStream))
        {
            sw.Write(contextInfo + "-updated");
        }

        return manifest;
    }
}
```