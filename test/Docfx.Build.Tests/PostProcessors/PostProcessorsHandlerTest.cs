// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.Plugins;
using Docfx.Tests.Common;
using Xunit;

namespace Docfx.Build.Engine.Tests;

[Collection("docfx STA")]
public class PostProcessorsHandlerTest : TestBase
{
    private const string MetaAppendContent = "-meta";
    private static readonly PostProcessorsHandler PostProcessorsHandler = new();
    private static readonly int MaxParallelism = Environment.ProcessorCount;

    [Fact]
    public void TestBasicScenario()
    {
        try
        {
            var manifest = JsonUtility.Deserialize<Manifest>(Path.GetFullPath("PostProcessors/Data/manifest_basic.json"));
            var outputFolder = GetRandomFolder();
            PrepareOutput(outputFolder, "index");
            SetDefaultFAL(manifest, outputFolder);
            PostProcessorsHandler.Handle(GetPostProcessors(typeof(AppendStringPostProcessor)), manifest, outputFolder);
            VerifyOutput(outputFolder, AppendStringPostProcessor.AppendString, "index");
        }
        finally
        {
            EnvironmentContext.Clean();
        }
    }

    private static void PrepareOutput(string outputFolder, params string[] fileNames)
    {
        foreach (var fileName in fileNames)
        {
            CreateFile($"{fileName}.html", $"{fileName}", outputFolder);
            CreateFile($"{fileName}.mta.json", $"{fileName}{MetaAppendContent}", outputFolder);
        }
    }

    private static void VerifyOutput(string outputFolder, string appendContent, params string[] fileNames)
    {
        foreach (var fileName in fileNames)
        {
            Assert.Equal($"{fileName}{appendContent}", EnvironmentContext.FileAbstractLayer.ReadAllText($"{fileName}.html"));
            Assert.Equal($"{fileName}{MetaAppendContent}", EnvironmentContext.FileAbstractLayer.ReadAllText($"{fileName}.mta.json"));
        }
    }

    private static List<PostProcessor> GetPostProcessors(params Type[] types)
    {
        var result = new List<PostProcessor>();
        foreach (var type in types)
        {
            var instance = Activator.CreateInstance(type);
            if (instance is not IPostProcessor postProcessor)
            {
                throw new InvalidOperationException($"{type} should implement {nameof(IPostProcessor)}.");
            }

            result.Add(new PostProcessor
            {
                ContractName = type.Name,
                Processor = postProcessor
            });
        }
        return result;
    }

    private static void SetDefaultFAL(Manifest manifest, string outputFolder)
    {
        EnvironmentContext.FileAbstractLayerImpl =
            FileAbstractLayerBuilder.Default
            .ReadFromManifest(manifest, outputFolder)
            .WriteToManifest(manifest, outputFolder)
            .Create();
    }
}
