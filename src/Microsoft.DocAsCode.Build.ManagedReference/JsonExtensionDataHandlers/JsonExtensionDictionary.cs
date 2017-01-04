// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference.BuildOutputs
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;

    [Serializable]
    public sealed class JsonExtensionDictionary : VirtualDictionary<string, object>
    {
        private JsonExtensionDataHandler[] _converters;
        public JsonExtensionDictionary() : base()
        {
        }

        public JsonExtensionDictionary(params JsonExtensionDataHandler[] converters) : base()
        {
            _converters = converters;
            foreach (var converter in _converters)
            {
                IEnumerable<KeyValuePair<string, object>> converted = converter.Initializer();
                foreach (var pair in converted)
                {
                    this[pair.Key] = pair.Value;
                }
            }
        }

        public override void Add(string key, object value)
        {
            foreach (var converter in _converters)
            {
                var pair = new KeyValuePair<string, object>(key, value);
                if (converter.Handler(key, value))
                {
                    break;
                }
            }
        }
    }
}
