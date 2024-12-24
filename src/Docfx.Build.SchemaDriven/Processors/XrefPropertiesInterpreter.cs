// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.Plugins;

namespace Docfx.Build.SchemaDriven.Processors;

public class XrefPropertiesInterpreter : IInterpreter
{
    /// <summary>
    /// Report xrefSpec when
    /// 1. ContentType = uid is defined => xref spec to be exported to xrefspec.yml
    /// Or 2. XrefResolver is defined => external xref spec
    /// </summary>
    public bool CanInterpret(BaseSchema schema)
    {
        if (schema == null)
        {
            return false;
        }

        if (schema.XrefProperties != null)
        {
            return true;
        }

        if (schema.Properties == null)
        {
            return false;
        }

        if (schema.Properties.TryGetValue("uid", out var baseSchema))
        {
            return baseSchema.ContentType == ContentType.Uid;
        }

        return false;
    }

    public object Interpret(BaseSchema schema, object value, IProcessContext context, string path)
    {
        if (value == null || !CanInterpret(schema))
        {
            return value;
        }

        if (JsonPointer.GetChild(value, "uid") is not string uid)
        {
            // schema validation threw error when uid is required, so here when uid is null, it must be optional, which is allowed
            return value;
        }

        if (string.IsNullOrEmpty(uid))
        {
            Logger.LogWarning($"Invalid xrefProperties for /{path}: empty uid is not allowed.");
            return value;
        }

        var xrefSpec = new XRefSpec
        {
            Uid = uid
        };

        var parts = schema.XrefProperties ?? ["name", "fullName"];
        var root = context.GetModel<object>();
        foreach (var part in parts.Distinct())
        {
            var jsonPointer = new JsonPointer(path + "/" + part);
            var property = jsonPointer.GetValue(root);
            if (property != null)
            {
                xrefSpec[part] = property;
            }
        }

        if (IsInternalXrefSpec(schema))
        {
            context.Uids.Add(new UidDefinition(uid, context.OriginalFileAndType.FullPath, path: path + "/uid"));
            xrefSpec.Href = ((RelativePath)context.OriginalFileAndType.File).GetPathFromWorkingFolder().UrlEncode().ToString();
            context.XRefSpecs.Add(xrefSpec);
        }
        else
        {
            context.ExternalXRefSpecs.Add(xrefSpec);
        }
        return value;
    }

    private static bool IsInternalXrefSpec(BaseSchema schema)
    {
        if (schema.Properties == null)
        {
            return false;
        }

        if (schema.Properties.TryGetValue("uid", out var innerSchema) != true)
        {
            return false;
        }

        return innerSchema.ContentType == ContentType.Uid;
    }
}
