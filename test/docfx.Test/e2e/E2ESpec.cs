// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    public class E2ESpec
    {
        public readonly string OS;

        public readonly string Repo;

        public readonly string Cwd;

        public readonly bool Watch;

        public string[] Commands = new[] { "restore", "build" };

        public readonly string[] Environments = Array.Empty<string>();

        public readonly string[] SkippableOutputs =
        {
            "xrefmap.json", ".publish.json", ".dependencymap.json",
            // legacy
            ".manifest.json", ".dependency-map.json", "filemap.json", "op_aggregated_file_map_info.json",
            "server-side-dependent-list.txt", "full-dependent-list.txt"
        };

        public readonly string[] SkippableInputs =
        {
            "user_profile.json",
            "github-user.json",
            ".lock.json",
        };

        public readonly Dictionary<string, E2ECommit[]> Repos = new Dictionary<string, E2ECommit[]>();

        public readonly Dictionary<string, string> Inputs = new Dictionary<string, string>();

        public readonly Dictionary<string, string> Cache = new Dictionary<string, string>();

        public readonly Dictionary<string, string> Outputs = new Dictionary<string, string>();

        public readonly Dictionary<string, JToken> Http = new Dictionary<string, JToken>();

        public class E2ECommit
        {
            public readonly string Author = "docfx";
            public readonly string Email = "docfx@microsoft.com";
            public readonly DateTime Time = new DateTime(2018, 10, 30, 0, 0, 0, DateTimeKind.Utc);
            public readonly Dictionary<string, string> Files;
        }
    }
}
