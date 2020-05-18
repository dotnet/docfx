// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace Microsoft.Docs.Build
{
    class Options
    {
        [Value(0, Required = true)]
        public string Repository { get; set; }

        [Option("branch")]
        public string Branch { get; set; } = "master";

        [Option("build-type")]
        public string BuildType { get; set; } = "Commit";

        [Option("pull-request-url")]
        public string PullRequestUrl { get; set; } = string.Empty;

        [Option("pull-request-branch")]
        public string PullRequestBranch { get; set; } = string.Empty;

        [Option("locale")]
        public string Locale { get; set; } = "en-us";

        [Option("need-publish")]
        public bool NeedPublish { get; set; }

        [Option("docset-name")]
        public string DocsetName { get; set; }

        [Option("product-name")]
        public string ProductName { get; set; }

        [Option("timeout")]
        public int? Timeout { get; set; }
    }
}
