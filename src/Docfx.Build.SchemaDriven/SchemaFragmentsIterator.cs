// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Build.OverwriteDocuments;
using Docfx.Common;
using Json.Schema;
using YamlDotNet.RepresentationModel;

namespace Docfx.Build.SchemaDriven;

public class SchemaFragmentsIterator
{
    private readonly ISchemaFragmentsHandler _handler;

    public SchemaFragmentsIterator(ISchemaFragmentsHandler handler)
    {
        _handler = handler;
    }

    public void Traverse(
        YamlNode node,
        Dictionary<string, MarkdownFragment> fragments,
        BaseSchema schema)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(fragments);
        ArgumentNullException.ThrowIfNull(schema);

        TraverseCore(node, fragments, schema, string.Empty, string.Empty);
    }

    private void TraverseCore(
        YamlNode node,
        Dictionary<string, MarkdownFragment> fragments,
        BaseSchema schema,
        string parentOPath,
        string uid)
    {
        var oPathPrefix = string.IsNullOrEmpty(parentOPath) ? "" : (parentOPath + "/");
        if (node is YamlMappingNode map)
        {
            var uidKey = schema.Properties.Keys.FirstOrDefault(k => schema.Properties[k].ContentType == ContentType.Uid);
            if (!string.IsNullOrEmpty(uidKey))
            {
                // If a new UID is found, OPath resets.
                uid = map.Children[uidKey].ToString();
                oPathPrefix = string.Empty;
                _handler.HandleUid(uidKey, (YamlMappingNode)node, fragments, schema, parentOPath, uid);
            }
            else if (string.IsNullOrEmpty(uid))
            {
                Logger.LogError("Cannot find Uid");
                return;
            }

            var keys = schema.Properties.Keys;
            foreach (var key in keys)
            {
                var propSchema = schema.Properties[key];
                if (propSchema.Type == SchemaValueType.Object || propSchema.Type == SchemaValueType.Array)
                {
                    if (map.Children.TryGetValue(key, out YamlNode child))
                    {
                        TraverseCore(child, fragments, propSchema, oPathPrefix + key, uid);
                    }
                }
                else
                {
                    _handler.HandleProperty(key, (YamlMappingNode)node, fragments, schema, oPathPrefix, uid);
                }
            }
        }
        else if (node is YamlSequenceNode seq)
        {
            if (string.IsNullOrEmpty(uid))
            {
                Logger.LogError("Cannot find Uid");
                return;
            }
            if (schema.Items != null && schema.Items.Properties.Any(s => s.Value.ContentType == ContentType.Markdown || s.Value.Type == SchemaValueType.Object || s.Value.Type == SchemaValueType.Array))
            {
                var mergeKey = schema.Items.Properties.Keys.FirstOrDefault(k => schema.Items.Properties[k].MergeType == MergeType.Key);
                if (mergeKey == null)
                {
                    return;
                }
                foreach (var item in seq)
                {
                    if (item is YamlMappingNode mapNode)
                    {
                        if (mapNode.Children.TryGetValue(mergeKey, out YamlNode child))
                        {
                            var opath = $"{parentOPath}[{mergeKey}=\"{child}\"]";
                            TraverseCore(item, fragments, schema.Items, opath, uid);
                        }
                        else
                        {
                            Logger.LogError($"Cannot find merge key {mergeKey} in {mapNode}");
                        }
                    }
                }
            }
        }
    }
}
