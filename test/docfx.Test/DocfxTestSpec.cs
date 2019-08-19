// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    public class DocfxTestSpec
    {
        public string OS { get; set; }

        public string Cwd { get; set; }

        public bool Restore { get; set; } = true;

        public bool Build { get; set; } = true;

        public bool Watch { get; set; }

        public bool Legacy { get; set; }

        public string Locale { get; set; }

        public string[] Environments { get; set; } = Array.Empty<string>();

        public Dictionary<string, TestGitCommit[]> Repos { get; set; } = new Dictionary<string, TestGitCommit[]>();

        public Dictionary<string, string> Inputs { get; set; } = new Dictionary<string, string>();

        public Dictionary<string, string> Cache { get; set; } = new Dictionary<string, string>();

        public Dictionary<string, string> Outputs { get; set; } = new Dictionary<string, string>();

        public Dictionary<string, JToken> Http { get; set; } = new Dictionary<string, JToken>();
    }
}
