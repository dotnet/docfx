// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tools.YamlSplitter.Models
{
    using CommandLine;

    public class CommonOptions
    {
        [Option("inputYamlFolder", Required = true, HelpText = "Folder that contains input Yaml files to be processed.")]
        public string InputYamlFolder { get; set; }

        [Option("outputYamlFolder", Required = false, HelpText = "Generated skeleton Yaml will be written to this folder.")]
        public string OutputYamlFolder { get; set; }

        [Option("mdFolder", Required = false, HelpText = "Folder that contains fragment Markdown files to be processed. If not specified, will use the Yaml path.")]
        public string MDFolder { get; set; }

        [Option("schemaFolder", Required = true, HelpText = "Folder that contains all schema.json files")]
        public string SchemaFolder { get; set; }
    }

    [Verb("init", HelpText = "Initialize fragment files with existing overwrite .md files")]
    public class InitOptions : CommonOptions
    {
    }

    [Verb("update", HelpText = "Update fragment files with latest Yamls")]
    public class UpdateOptions : CommonOptions
    {

    }
}
