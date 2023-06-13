﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Text;

using Microsoft.DocAsCode.Build.Common;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Plugins;

using Newtonsoft.Json;

namespace Microsoft.DocAsCode.Build.Engine.Tests;

[Export(typeof(IDocumentProcessor))]
public class YamlDocumentProcessor : DisposableDocumentProcessor
{
    #region Fields

    private const string YamlDocumentYamlMime = YamlMime.YamlMimePrefix + "YamlDocument";
    private const string DefaultDocumentType = "YamlDocument";

    public override string Name => nameof(YamlDocumentProcessor);

    private readonly ResourcePoolManager<JsonSerializer> _serializerPool;

    #endregion

    #region Constructors

    public YamlDocumentProcessor()
    {
        _serializerPool = new ResourcePoolManager<JsonSerializer>(GetSerializer, 0x10);
    }

    #endregion

    #region IDocumentProcessor Member

    [ImportMany(nameof(YamlDocumentProcessor))]
    public override IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }

    public override ProcessingPriority GetProcessingPriority(FileAndType file)
    {
        if (file.Type != DocumentType.Article)
        {
            return ProcessingPriority.NotSupported;
        }
        if (!".yml".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase) &&
            !".yaml".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase))
        {
            return ProcessingPriority.NotSupported;
        }
        var mime = YamlMime.ReadMime(file.File);
        switch (mime)
        {
            case YamlDocumentYamlMime:
                return ProcessingPriority.Normal;
            default:
                return ProcessingPriority.NotSupported;
        }
    }

    public override FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
    {
        if (file.Type != DocumentType.Article)
        {
            throw new NotSupportedException();
        }

        var content = YamlUtility.Deserialize<YamlDocumentModel>(file.File);
        foreach (var item in metadata.Where(item => !content.Metadata.ContainsKey(item.Key)))
        {
            content.Metadata[item.Key] = item.Value;
        }
        var localPathFromRoot = PathUtility.MakeRelativePath(EnvironmentContext.BaseDirectory, EnvironmentContext.FileAbstractLayer.GetPhysicalPath(file.File));
        return new FileModel(
            file,
            content)
        {
            LocalPathFromRoot = localPathFromRoot,
            DocumentType = content.DocumentType ?? DefaultDocumentType,
        };
    }

    public override SaveResult Save(FileModel model)
    {
        if (model.Type != DocumentType.Article)
        {
            throw new NotSupportedException();
        }
        return new SaveResult
        {
            DocumentType = model.DocumentType,
            FileWithoutExtension = Path.ChangeExtension(model.File, null)
        };
    }

    #endregion

    #region Protected Methods

    protected virtual void SerializeModel(object model, Stream stream)
    {
        using var sw = new StreamWriter(stream, Encoding.UTF8, 0x100, true);
        using var lease = _serializerPool.Rent();
        lease.Resource.Serialize(sw, model);
    }

    protected virtual object DeserializeModel(Stream stream)
    {
        using var sr = new StreamReader(stream, Encoding.UTF8, false, 0x100, true);
        using var jr = new JsonTextReader(sr);
        using var lease = _serializerPool.Rent();
        return lease.Resource.Deserialize(jr);
    }

    protected virtual void SerializeProperties(IDictionary<string, object> properties, Stream stream)
    {
        using var sw = new StreamWriter(stream, Encoding.UTF8, 0x100, true);
        using var lease = _serializerPool.Rent();
        lease.Resource.Serialize(sw, properties);
    }

    protected virtual IDictionary<string, object> DeserializeProperties(Stream stream)
    {
        using var sr = new StreamReader(stream, Encoding.UTF8, false, 0x100, true);
        using var jr = new JsonTextReader(sr);
        using var lease = _serializerPool.Rent();
        return lease.Resource.Deserialize<Dictionary<string, object>>(jr);
    }

    protected virtual JsonSerializer GetSerializer()
    {
        return new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            Converters =
            {
                new Newtonsoft.Json.Converters.StringEnumConverter(),
            },
            TypeNameHandling = TypeNameHandling.All, // lgtm [cs/unsafe-type-name-handling]
        };
    }

    #endregion
}
