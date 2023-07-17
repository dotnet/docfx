// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Build.OverwriteDocuments;

using YamlDotNet.RepresentationModel;

namespace Docfx.Build.SchemaDriven;

public interface ISchemaFragmentsHandler
{
    void HandleUid(string uidKey, YamlMappingNode node, Dictionary<string, MarkdownFragment> fragments, BaseSchema schema, string oPathPrefix, string uid);

    void HandleProperty(string propertyKey, YamlMappingNode node, Dictionary<string, MarkdownFragment> fragments, BaseSchema schema, string oPathPrefix, string uid);
}
