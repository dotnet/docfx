// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Json.Schema;

namespace Docfx.Build.SchemaDriven;

public class BaseSchema
{
    [JsonPropertyName("$ref")]
    public string Ref { get; set; }

    public Dictionary<string, BaseSchema> Definitions { get; set; }

    public string Title { get; set; }

    public SchemaValueType? Type { get; set; }

    public Dictionary<string, BaseSchema> Properties { get; set; }

    public BaseSchema Items { get; set; }

    public ReferenceType Reference { get; set; }

    public ContentType ContentType { get; set; }

    public List<string> Tags { get; set; }

    public MergeType MergeType { get; set; }

    public List<string> XrefProperties { get; set; }
}
