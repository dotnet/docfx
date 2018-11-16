// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    public class XrefSpec
    {
        public string Uid { get; set; }

        [JsonIgnore]
        public SortedSet<string> Monikers { get; } = new SortedSet<string>(new StringDescendingComparer());

        public string Href { get; set; }

        [JsonExtensionData]
        public JObject ExtensionData { get; } = new JObject();

        public string GetName() => GetXrefPropertyValue("name");

        public string GetXrefPropertyValue(string property)
            => ExtensionData.TryGetValue<JValue>(property, out var v) && v.Value is string str ? str : null;

        private class StringDescendingComparer : IComparer<string>
        {
            public int Compare(string x, string y)
                => y.CompareTo(x);
        }
    }
}
