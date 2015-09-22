// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using CommandLine;
    using System.Collections.Generic;

    /// <summary>
    /// TODO: support input Conceptual files in Website sub commands, e.g. -c "**/*.md" "**/*.png"?
    /// </summary>
    class BuildCommandOptions
    {
        [Option('o', "output")]
        public string OutputFolder { get; set; }

        [ValueList(typeof(List<string>))]
        public List<string> Content { get; set; }

        [Option("resource", HelpText = "Specifies resources used by content files.")]
        public List<string> Resource { get; set; }

        [Option("overwrite", HelpText = "Specifies overwrite files used by content files.")]
        public List<string> Overwrite { get; set; }

        [Option("externalReference", HelpText = "Specifies external reference files used by content files.")]
        public List<string> ExternalReference { get; set; }

        [Option('t', "template", HelpText = "Specifies the template name to apply to. If not specified, output YAML file will not be transformed.")]
        public string Template { get; set; }

        [Option("templateFolder", HelpText = "If specified, this folder will be searched first to get the matching template.")]
        public string TemplateFolder { get; set; }

        [Option("theme", HelpText = "Specifies which theme to use. By default 'default' theme is offered.")]
        public string TemplateTheme { get; set; }

        [Option("themeFolder", HelpText = "If specified, this folder will be searched first to get the matching theme.")]
        public string TemplateThemeFolder { get; set; }
    }
}
