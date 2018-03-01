// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;

    public class EnvironmentScope : IDisposable
    {
        private readonly Dictionary<string, string> _originalEnvValues = new Dictionary<string, string>();
        public EnvironmentScope(Dictionary<string, string> envValues)
        {
            foreach (var pair in envValues)
            {
                // incase envValues contains duplicate keys
                if (!_originalEnvValues.TryGetValue(pair.Key, out var val))
                {
                    _originalEnvValues[pair.Key] = Environment.GetEnvironmentVariable(pair.Key);
                    Environment.SetEnvironmentVariable(pair.Key, pair.Value);
                }
            }
        }

        public void Dispose()
        {
            // restore
            foreach(var pair in _originalEnvValues)
            {
                Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }
        }
    }
}
