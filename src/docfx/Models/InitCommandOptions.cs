// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace Microsoft.DocAsCode;

[Verb("init", HelpText = "Generate an initial docfx.json following the instructions")]
internal class InitCommandOptions
{
    [Option('q', "quiet", HelpText = "Quietly generate the default docfx.json")]
    public bool Quiet { get; set; }

    [Option("overwrite", HelpText = "Specify if the current file will be overwritten if it exists")]
    public bool Overwrite { get; set; }

    [Option('o', "output", HelpText = "Specify the output folder of the config file. If not specified, the config file will be saved to a new folder docfx_project")]
    public string OutputFolder { get; set; }

    [Option('f', "file", HelpText = "Generate config file docfx.json only, no project folder will be generated")]
    public bool OnlyConfigFile { get; set; }
}
