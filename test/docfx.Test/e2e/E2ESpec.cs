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

        public readonly string[] Environments = Array.Empty<string>();

        public readonly string[] Commands = new[] { "restore", "build" };

        public readonly Dictionary<string, E2ECommit[]> Repos = new Dictionary<string, E2ECommit[]>();

        public readonly Dictionary<string, string> Inputs = new Dictionary<string, string>();

        public readonly Dictionary<string, string> Outputs = new Dictionary<string, string>();

        public readonly Dictionary<string, string> Restores = new Dictionary<string, string>();

        public class E2ECommit
        {
            public readonly string Author = "docfx";
            public readonly string Email = "docfx@microsoft.com";
            public readonly DateTime Time = new DateTime(2018, 10, 30);
            public readonly Dictionary<string, string> Files;
        }
    }
}
