// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;
    using Microsoft.DocAsCode.Common;

    public static class TemplateProcessorUtility
    {
        public static IDictionary<string, string> LoadTokens(ResourceFileReader resource)
        {
            if (resource == null)
            {
                return null;
            }

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
    }
}
