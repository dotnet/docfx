// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace Microsoft.Docs.Build;

internal class Options
{
    [Value(0, Required = true)]
    public string Repository { get; set; } = "";

    [Option("warm-up")]
    public bool WarmUp { get; set; } = false;

    [Option("branch")]
    public string Branch { get; set; } = "live";

    [Option("locale")]
    public string Locale { get; set; } = "en-us";

    [Option("timeout")]
    public int? Timeout { get; set; }

    [Option("output-type")]
    public string OutputType { get; set; } = "pagejson";

    [Option("template")]
    public string? Template { get; set; }

    [Option("dry-run")]
    public bool DryRun { get; set; }

    [Option("no-dry-sync")]
    public bool NoDrySync { get; set; }

    [Option("profile")]
    public bool Profile { get; set; }

    [Option("regression-rules")]
    public bool RegressionRules { get; set; }

    [Option("error-level")]
    public ErrorLevel ErrorLevel { get; set; }
}
