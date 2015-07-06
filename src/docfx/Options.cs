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

        [VerbOption("init", HelpText = "Init xdoc.json with recommended settings")]
        public InitSubOptions InitVerb { get; set; } = new InitSubOptions();

        [VerbOption("metadata", HelpText = "Generate API YAML metadata")]
        public MetadataSubOptions MetadataVerb { get; set; } = new MetadataSubOptions();

        [VerbOption("website", HelpText = "Generate website as documenation")]
        public WebsiteSubOptions WebsiteVerb { get; set; } = new WebsiteSubOptions();

        [VerbOption("export", HelpText = "Generate API YAML for external reference")]
        public ExportSubOptions ExportVerb { get; set; } = new ExportSubOptions();

        public WebsiteSubOptions GetTopLevelOptions()
        {
            return new WebsiteSubOptions
            {
                OutputFolder = OutputFolder,
                Projects = Projects,
                ForceRebuild = ForceRebuild,
                TemplateFolder = TemplateFolder,
                WebsiteTheme = WebsiteTheme,
            };
        }
    }

    class TopLevelOptions
    {
        [Option('o', "output")]
        public string OutputFolder { get; set; }

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
        [Option('t', "template")]
        public string TemplateFolder { get; set; }

        [Option("theme", HelpText = "Specifies which theme to use. By default 'Github.default' and 'IIS.default' are offered")]
        public string WebsiteTheme { get; set; }

        public WebsiteSubOptions(WebsiteSubOptions options) : base(options)
        {
            this.ForceRebuild = options.ForceRebuild;
            this.TemplateFolder = options.TemplateFolder;
            this.WebsiteTheme = options.WebsiteTheme;
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
        [Option('q', "quiet", HelpText = "Quietly generate the default xdoc.json")]
        public bool Quiet { get; set; }

        [Option('n', "name", HelpText = "Specify the name of the config file generated", DefaultValue = "xdoc.json")]
        public string Name { get; set; }

        [Option('o', "output", HelpText = "Specify the output folder of the config file. If not specified, the config file will be saved to current folder")]
        public string OutputFolder { get; set; }
    }

    class ExportSubOptions : TopLevelOptions
    {
        [Option('b', "baseurl", HelpText = "The base url of yaml file.", Required = true)]
        public string BaseUrl { get; set; }

        [Option('n', "name", HelpText = "The name of package.")]
        public string Name { get; set; }

        public ExportSubOptions(ExportSubOptions options) : base(options)
        {
            BaseUrl = options.BaseUrl;
            Name = options.Name;
        }
        public ExportSubOptions() : base() { }
    }

    class HelpSubOptions
    {
        [ValueOption(0)]
        public string Command { get; set; }
    }
}
