// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;

namespace Docfx.Build.Engine;

public static class TemplateProcessorUtility
{
    public static IDictionary<string, string> LoadTokens(ResourceFileReader resource)
    {
        if (resource is CompositeResourceReader compositeResourceReader)
        {
            return Merge(compositeResourceReader.Select(LoadTokensCore));
        }

        return LoadTokensCore(resource);

        static Dictionary<string, string> LoadTokensCore(ResourceFileReader resource)
        {
            var tokenJson = resource.GetResource("token.json");
            if (string.IsNullOrEmpty(tokenJson))
            {
                // also load `global.json` for backward compatibility
                // TODO: remove this
                tokenJson = resource.GetResource("global.json");
                if (string.IsNullOrEmpty(tokenJson))
                {
                    return null;
                }
            }

            return JsonUtility.FromJsonString<Dictionary<string, string>>(tokenJson);
        }

        static Dictionary<string, string> Merge(IEnumerable<Dictionary<string, string>> items)
        {
            var result = new Dictionary<string, string>();
            foreach (var item in items)
            {
                if (item != null)
                {
                    foreach (var (key, value) in item)
                    {
                        result[key] = value;
                    }
                }
            }
            return result;
        }
    }
}
