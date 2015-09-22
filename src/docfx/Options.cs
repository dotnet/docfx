// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using CommandLine;
    using Microsoft.DocAsCode.EntityModel;
    using System.Collections.Generic;

    class Options : WebsiteSubOptions
    {
        public SubCommandType? CurrentSubCommand { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [VerbOption("help", HelpText = "Read the detailed help documentation")]
        public HelpSubOptions HelpVerb { get; set; } = new HelpSubOptions();

        [VerbOption("init", HelpText = "Init docfx.json with recommended settings")]
        public InitSubOptions InitVerb { get; set; } = new InitSubOptions();

        [VerbOption("metadata", HelpText = "Generate API YAML metadata")]
        public MetadataSubOptions MetadataVerb { get; set; } = new MetadataSubOptions();

        [VerbOption("website", HelpText = "Generate website as documenation")]
        public WebsiteSubOptions WebsiteVerb { get; set; } = new WebsiteSubOptions();

        [VerbOption("export", HelpText = "Generate API yaml from project for external reference")]
        public ExportSubOptions ExportVerb { get; set; } = new ExportSubOptions();

        [VerbOption("pack", HelpText = "Pack existing API YAML for external reference")]
        public PackSubOptions PackVerb { get; set; } = new PackSubOptions();

        public WebsiteSubOptions GetTopLevelOptions()
        {
            return new WebsiteSubOptions
            {
                OutputFolder = OutputFolder,
                Projects = Projects,
                ForceRebuild = ForceRebuild,
                Template = Template,
                TemplateThemeFolder = TemplateThemeFolder,
            };
        }
    }

    class TopLevelOptions
    {
        [Option('o', "output")]
        public string OutputFolder { get; set; }

        [Option("raw", HelpText = "Preserve the existing xml comment tags inside 'summary' triple slash comments")]
        public bool PreserveRawInlineComments { get; set; }
        
        [ValueList(typeof(List<string>))]
        public List<string> Projects { get; set; }

        public TopLevelOptions(TopLevelOptions options)
        {
            this.OutputFolder = options.OutputFolder;
            this.Projects = options.Projects;
        }

        public TopLevelOptions() { }
    }

    /// <summary>
    /// TODO: support input Conceptual files in Website sub commands, e.g. -c "**/*.md" "**/*.png"?
    /// </summary>
    class WebsiteSubOptions : MetadataSubOptions
    {
        [Option('t', "template", HelpText = "Specifies the template name to apply to. If not specified, output YAML file will not be transformed.")]
        public string Template { get; set; }

        [Option("templateFolder", HelpText = "If specified, this folder will be searched first to get the matching template.")]
        public string TemplateFolder { get; set; }

        [Option("theme", HelpText = "Specifies which theme to use. By default 'default' theme is offered.")]
        public string TemplateTheme { get; set; }

        [Option("themeFolder", HelpText = "If specified, this folder will be searched first to get the matching theme.")]
        public string TemplateThemeFolder { get; set; }

        public WebsiteSubOptions(WebsiteSubOptions options) : base(options)
        {
            this.ForceRebuild = options.ForceRebuild;
            this.Template = options.Template;
            this.TemplateFolder = options.TemplateFolder;
            this.TemplateTheme = options.TemplateTheme;
            this.TemplateThemeFolder = options.TemplateThemeFolder;
        }

        public WebsiteSubOptions(MetadataSubOptions options) : base(options)
        {
            this.ForceRebuild = options.ForceRebuild;
        }

        public WebsiteSubOptions(TopLevelOptions options) : base(options) { }
        public WebsiteSubOptions() : base() { }
    }

    class MetadataSubOptions : TopLevelOptions
    {
        [Option('f', "force", HelpText = "Force re-generate all the metadata")]
        public bool ForceRebuild { get; set; }
        public MetadataSubOptions(TopLevelOptions options) : base(options) { }
        public MetadataSubOptions() : base() { }
    }

    class InitSubOptions
    {
        [Option('q', "quiet", HelpText = "Quietly generate the default docfx.json")]
        public bool Quiet { get; set; }

        [Option('n', "name", HelpText = "Specify the name of the config file generated", DefaultValue = "docfx.json")]
        public string Name { get; set; }

        [Option('o', "output", HelpText = "Specify the output folder of the config file. If not specified, the config file will be saved to current folder")]
        public string OutputFolder { get; set; }
    }

    class ExportSubOptions : TopLevelOptions
    {
        [Option('u', "url", HelpText = "The base url of yaml file.", Required = true)]
        public string BaseUrl { get; set; }

        [Option('n', "name", HelpText = "The name of package.")]
        public string Name { get; set; }

        [Option('a', "append", HelpText = "Append the package.")]
        public bool AppendMode { get; set; }
    }

    class PackSubOptions : TopLevelOptions
    {
        [Option('u', "url", HelpText = "The base url of yaml file.", Required = true)]
        public string BaseUrl { get; set; }

        [Option('s', "source", HelpText = "The base folder for yaml files.", Required = true)]
        public string Source { get; set; }

        [Option('g', "glob", HelpText = "The glob partten for yaml files.", Required = true)]
        public string Glob { get; set; }

        [Option('n', "name", HelpText = "The name of package.")]
        public string Name { get; set; }

        [Option('a', "append", HelpText = "Append the package.")]
        public bool AppendMode { get; set; }

        [Option('f', "flat", HelpText = "Flat href path.")]
        public bool FlatMode { get; set; }
    }

    class HelpSubOptions
    {
        [ValueOption(0)]
        public string Command { get; set; }
    }
}
