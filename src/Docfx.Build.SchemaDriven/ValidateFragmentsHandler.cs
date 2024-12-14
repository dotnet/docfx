// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Build.OverwriteDocuments;
using Docfx.Common;

using YamlDotNet.RepresentationModel;

namespace Docfx.Build.SchemaDriven;

public class ValidateFragmentsHandler : ISchemaFragmentsHandler
{
    private readonly Dictionary<string, bool> _isMissingUidsLogged = [];

    public void HandleUid(string uidKey, YamlMappingNode node, Dictionary<string, MarkdownFragment> fragments, BaseSchema schema, string oPathPrefix, string uid)
    {
        if (!fragments.ContainsKey(uid))
        {
            _isMissingUidsLogged.TryAdd(uid, false);
        }
    }

    public void HandleProperty(string propertyKey, YamlMappingNode node, Dictionary<string, MarkdownFragment> fragments, BaseSchema schema, string oPathPrefix, string uid)
    {
        var propSchema = schema.Properties[propertyKey];
        if (!propSchema.IsRequiredInFragments())
        {
            return;
        }
        if (_isMissingUidsLogged.TryGetValue(uid, out var isLogged))
        {
            if (!isLogged)
            {
                Logger.LogWarning($"Missing UID {uid} in markdown fragments. This may be caused by YAML update. Please ensure your markdown fragments are up to date.", code: WarningCodes.Overwrite.InvalidMarkdownFragments);
                _isMissingUidsLogged[uid] = true;
            }
            return;
        }
        var opath = oPathPrefix + propertyKey;
        var fragment = fragments[uid];
        if (!fragment.Properties.ContainsKey(opath))
        {
            if (string.IsNullOrEmpty(oPathPrefix) && fragment.Metadata?.ContainsKey(opath) == true)
            {
                return;
            }
            // TODO: also check whether it exists in inner objects
            Logger.LogWarning($"Missing property '{opath}' for UID '{uid}' in markdown fragments. This may be caused by YAML update or schema update. Please ensure your markdown fragments are up to date.", code: WarningCodes.Overwrite.InvalidMarkdownFragments);
        }
    }
}
