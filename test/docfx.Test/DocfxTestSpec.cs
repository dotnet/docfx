// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    public class DocfxTestSpec
    {
        public string OS { get; private set; }

        public string Cwd { get; private set; }

        public bool Restore { get; private set; } = true;

        public bool Build { get; private set; } = true;

        public bool NoDryRun { get; private set; }

        public bool Watch { get; private set; }

        public bool Legacy { get; private set; }

        public bool Temp { get; private set; }

        public string Locale { get; private set; }

        public string[] Environments { get; private set; } = Array.Empty<string>();

        public Dictionary<string, TestGitCommit[]> Repos { get; private set; } = new Dictionary<string, TestGitCommit[]>();

        public Dictionary<string, string> Inputs { get; private set; } = new Dictionary<string, string>();

        public Dictionary<string, string> Cache { get; private set; } = new Dictionary<string, string>();

        public Dictionary<string, string> State { get; private set; } = new Dictionary<string, string>();

        public Dictionary<string, string> Outputs { get; private set; } = new Dictionary<string, string>();

        public Dictionary<string, JToken> Http { get; private set; } = new Dictionary<string, JToken>();
    }
}
