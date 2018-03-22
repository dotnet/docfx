// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tools.YamlSplitter
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Build.OverwriteDocuments;
    using Microsoft.DocAsCode.Build.SchemaDriven;

    using Newtonsoft.Json.Schema;
    using YamlDotNet.RepresentationModel;

    public class UpdateFragmentsHandler : ISchemaFragmentsHandler
    {
        public void HandleUid(string uidKey, YamlMappingNode node, Dictionary<string, MarkdownFragment> fragments, BaseSchema schema, string oPathPrefix, string uid)
        {
            fragments.AddOrUpdateFragmentEntity(node.Children[uidKey].ToString());
        }

        public void HandleProperty(string propertyKey, YamlMappingNode node, Dictionary<string, MarkdownFragment> fragments, BaseSchema schema, string oPathPrefix, string uid)
        {
            var propSchema = schema.Properties[propertyKey];
            // TODO: consume "editable"
            if (propSchema.Type == JSchemaType.String && propSchema.ContentType == ContentType.Markdown)
            {
                var val = "";
                if (node.Children.ContainsKey(propertyKey))
                {
                    val = node.Children[propertyKey].ToString();
                    node.Children.Remove(propertyKey);
                }

                fragments[uid].AddOrUpdateFragmentProperty(oPathPrefix + propertyKey, val);
            }
        }
    }
}
