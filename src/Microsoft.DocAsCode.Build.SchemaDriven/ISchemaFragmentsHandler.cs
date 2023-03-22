// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Build.OverwriteDocuments;

using YamlDotNet.RepresentationModel;

namespace Microsoft.DocAsCode.Build.SchemaDriven;

public interface ISchemaFragmentsHandler
{
    void HandleUid(string uidKey, YamlMappingNode node, Dictionary<string, MarkdownFragment> fragments, BaseSchema schema, string oPathPrefix, string uid);

    void HandleProperty(string propertyKey, YamlMappingNode node, Dictionary<string, MarkdownFragment> fragments, BaseSchema schema, string oPathPrefix, string uid);
}
