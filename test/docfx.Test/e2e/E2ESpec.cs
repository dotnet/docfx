// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    public class E2ESpec
    {
        public readonly string OS;

        public readonly string Repo;

        public readonly string[] Commands = new[] { "restore", "build" };

        public readonly Dictionary<string, string> Inputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public readonly Dictionary<string, string> Outputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public readonly Dictionary<string, string> Restores = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
