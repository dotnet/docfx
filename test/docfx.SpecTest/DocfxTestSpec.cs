// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Docs.Build;

public class DocfxTestSpec
{
    public string OS { get; set; }

    public string Cwd { get; set; }

    public bool NoDryRun { get; set; }

    public bool NoSingleFile { get; set; }

    public bool DryRunOnly { get; set; }

    public bool NoDrySync { get; set; } = true;

    public bool NoRestore { get; set; }

    public bool NoInputCheck { get; set; }

    public bool Temp { get; set; }

    public bool UsePhysicalInput { get; set; }

    public bool NoCache { get; set; }

    public bool UseDocsGitHubToken { get; set; }

    public string Locale { get; set; }

    public string BuildEnvironment { get; set; } = "PPE";

    [JsonConverter(typeof(OneOrManyConverter))]
    public string[] BuildFiles { get; set; } = Array.Empty<string>();

    public string[] Environments { get; set; } = Array.Empty<string>();

    public Dictionary<string, TestGitCommit[]> Repos { get; set; } = new Dictionary<string, TestGitCommit[]>();

    public Dictionary<string, string> Inputs { get; set; } = new Dictionary<string, string>();

    public Dictionary<string, string> Cache { get; set; } = new Dictionary<string, string>();

    public Dictionary<string, string> State { get; set; } = new Dictionary<string, string>();

    public Dictionary<string, string> Outputs { get; set; } = new Dictionary<string, string>();

    public List<LanguageServerTestCommand> LanguageServer { get; set; } = new List<LanguageServerTestCommand>();

    public Dictionary<string, string> Http { get; set; } = new Dictionary<string, string>();
}
