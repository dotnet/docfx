// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference.BuildOutputs
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    internal static class JsonExtensionDataHandlersCreator
    {
        public static JsonExtensionDataHandler CreatePrefixHandler(SortedList<string, List<string>> modifiers, string prefix)
        {
            if (modifiers == null)
            {
                throw new ArgumentNullException(nameof(modifiers));
            }

            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            return new JsonExtensionDataHandler(
                () =>
                    modifiers.Select(s => new KeyValuePair<string, object>(prefix + s.Key, s.Value))
                , (k, v) =>
                {
                    if (k.StartsWith(prefix))
                    {
                        var modifierKey = k.Substring(prefix.Length);
                        var val = v as IEnumerable;
                        if (val == null)
                        {
                            throw new InvalidCastException($"modifiers must be List<string>, type {v.GetType()} is not supported!");
                        }

                        var list = new List<string>();
                        foreach (var item in val)
                        {
                            list.Add(item.ToString());
                        }
                        modifiers.Add(modifierKey, list);
                        return true;
                    }
                    return false;
                });
        }

        public static JsonExtensionDataHandler CreateDefaultHandler(Dictionary<string, object> mta)
        {
            if (mta == null)
            {
                throw new ArgumentNullException(nameof(mta));
            }

            return new JsonExtensionDataHandler(
                () => mta,
                (k, v) =>
                {
                    mta.Add(k, v);
                    return true;
                });
        }
    }
}
