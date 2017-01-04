// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference.BuildOutputs
{
    using System;
    using System.Collections.Generic;

    public class JsonExtensionDataHandler
    {
        public Func<IEnumerable<KeyValuePair<string, object>>> Initializer { get; }

        public Func<string, object, bool> Handler { get; }

        public JsonExtensionDataHandler(Func<IEnumerable<KeyValuePair<string, object>>> initializer, Func<string, object, bool> handler)
        {
            Initializer = initializer;
            Handler = handler;
        }
    }
}
