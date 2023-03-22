// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Json.Schema;

namespace Microsoft.DocAsCode.Build.SchemaDriven;

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
