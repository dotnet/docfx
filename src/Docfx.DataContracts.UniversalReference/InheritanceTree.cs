﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.Common.EntityMergers;
using Docfx.DataContracts.Common;
using Docfx.YamlSerialization;

using YamlDotNet.Serialization;

namespace Docfx.DataContracts.UniversalReference;

public class InheritanceTree
{
    [YamlMember(Alias = Constants.PropertyName.Type)]
    [JsonPropertyName(Constants.PropertyName.Type)]
    [UniqueIdentityReference]
    public string Type { get; set; }

    /// <summary>
    /// item's inheritance
    /// multiple inheritance is allowed in languages like Python
    /// </summary>
    [YamlMember(Alias = Constants.PropertyName.Inheritance)]
    [MergeOption(MergeOption.Ignore)]
    [JsonPropertyName(Constants.PropertyName.Inheritance)]
    public List<InheritanceTree> Inheritance { get; set; }

    [ExtensibleMember]
    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
