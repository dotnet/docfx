// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal static class MaskUtility
{
    public static string HideSecret(string args, string? secret)
        => secret is null
            ? args
            : args.Replace(secret, MaskString(secret));

    public static JToken HideSecret(JToken token)
    {
        switch (token)
        {
            case JValue val:
                if (val.Value is string secret)
                {
                    return new JValue(MaskString(secret));
                }
                return val;
            case JArray arr:
                return new JArray(arr.Select(arrElement => HideSecret(arrElement)));
            case JObject obj:
                var result = new JObject();
                foreach (var (key, value) in obj)
                {
                    result[key] = value is null ? value : HideSecret(value);
                }
                return result;
        }
        return token;
    }

    private static string MaskString(string str)
        => str.Length > 10 ? str[0..2] + "***" + str[^2..] : "***";
}
