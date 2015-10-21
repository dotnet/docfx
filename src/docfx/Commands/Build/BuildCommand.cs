// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.EntityModel.Builders;
    using Newtonsoft.Json.Linq;
    using System.Linq;
    using System.IO;
    using System;
    using System.Collections.Generic;
    using Utility;
    using System.Collections.Immutable;
    using Plugins;
    using Owin.Hosting;
    using Owin.StaticFiles;
    using Owin.FileSystems;
    using global::Owin;

    class BuildCommand : ICommand
    {
        private DocumentBuilder _builder = new DocumentBuilder();
        private CommandContext _context;
        private string _helpMessage = null;
        public BuildJsonConfig Config { get; }

        public BuildCommand(CommandContext context) : this(new BuildJsonConfig(), context)
        {
        }

        public BuildCommand(JToken value, CommandContext context) : this(CommandFactory.ConvertJTokenTo<BuildJsonConfig>(value), context)
        {
        }

        public BuildCommand(BuildJsonConfig config, CommandContext context)
        {
            _context = context;
            Config = config;
        }

        public BuildCommand(Options options, CommandContext context)
        {
            var buildCommandOptions = options.BuildCommand;
            if (buildCommandOptions.IsHelp)
            {
                _helpMessage = HelpTextGenerator.GetHelpMessage(options, "build");
            }
            else
            {
                Config = GetConfigFromOptions(buildCommandOptions);
                _context = context;
            }
        }

        public ParseResult Exec(RunningContext context)
        {
            if (_helpMessage != null)
            {
                Console.WriteLine(_helpMessage);
                return ParseResult.SuccessResult;
            }
            if (_context?.BaseDirectory != null)
                Config.BaseDirectory = _context?.BaseDirectory;

            return InternalExec(Config, context);
        }

        private ParseResult InternalExec(BuildJsonConfig config, RunningContext context)
        {
            try
            {
                var parameters = ConfigToParameter(config);
                if (parameters.Files.Count == 0) return new ParseResult(ResultLevel.Warning, "No files found, nothing is to be generated");
                _builder.Build(parameters);

                var documentContext = DocumentBuildContext.DeserializeFrom(parameters.OutputBaseDir);
                var assembly = typeof(Program).Assembly;

                // If RootOutput folder is specified from command line, use it instead of the base directory
                var outputFolder = Path.Combine(_context?.RootOutputFolder ?? config.BaseDirectory ?? string.Empty, config.Destination ?? string.Empty);
                var templateFolder = string.IsNullOrEmpty(config.TemplateFolder) ? null : Path.Combine(config.BaseDirectory ?? string.Empty, config.TemplateFolder);
                var themeFolder = string.IsNullOrEmpty(config.TemplateThemeFolder) ? null : Path.Combine(config.BaseDirectory ?? string.Empty, config.TemplateThemeFolder);
                using (var manager = new TemplateManager(assembly, "Template", templateFolder, config.Template, themeFolder, config.TemplateTheme))
                {
                    manager.ProcessTemplateAndTheme(documentContext, outputFolder, true);
                }

                // TODO: SEARCH DATA

                if (config.Serve)
                {
                   Serve(outputFolder, config.Port);
                }

                return ParseResult.SuccessResult;
            }
            catch (Exception e)
            {
                return new ParseResult(ResultLevel.Error, e.Message);
            }
        }

        private static void Serve(string folder, string port)
        {
            folder = Path.GetFullPath(folder);
            port = string.IsNullOrWhiteSpace(port) ? "8080" : port;
            var url = $"http://localhost:{port}";
            var fileServerOptions = new FileServerOptions
            {
                EnableDirectoryBrowsing = true,
                FileSystem = new PhysicalFileSystem(folder),
            };

            if (!File.Exists(Path.Combine(folder, "index.html")) && File.Exists(Path.Combine(folder, "toc.html")))
            {
                File.Copy(Path.Combine(folder, "toc.html"), Path.Combine(folder, "index.html"));
            }

            WebApp.Start(url, builder => builder.UseFileServer(fileServerOptions));
            Console.WriteLine($"Listening at {url}");
            Console.ReadLine();
        }

        private static DocumentBuildParameters ConfigToParameter(BuildJsonConfig config)
        {
            var parameters = new DocumentBuildParameters();
            var baseDirectory = config.BaseDirectory ?? Environment.CurrentDirectory;

            parameters.OutputBaseDir = Path.GetFullPath(Path.Combine("obj", Path.GetRandomFileName()));
            parameters.Metadata = (config.GlobalMetadata ?? new Dictionary<string, object>()).ToImmutableDictionary();
            parameters.ExternalReferencePackages = GetFilesFromFileMapping(GlobUtility.ExpandFileMapping(baseDirectory, config.ExternalReference)).ToImmutableArray();
            parameters.Files = GetFileCollectionFromFileMapping(baseDirectory,
               Tuple.Create(DocumentType.Article, GlobUtility.ExpandFileMapping(baseDirectory, config.Content)),
               Tuple.Create(DocumentType.Override, GlobUtility.ExpandFileMapping(baseDirectory, config.Overwrite)),
               Tuple.Create(DocumentType.Resource, GlobUtility.ExpandFileMapping(baseDirectory, config.Resource)));
            return parameters;
        }

        private static IEnumerable<string> GetFilesFromFileMapping(FileMapping mapping)
        {
            if (mapping == null) yield break;
            foreach(var file in mapping.Items)
            {
                foreach(var item in file.Files)
                {
                    yield return Path.Combine(file.CurrentWorkingDirectory ?? Environment.CurrentDirectory, item);
                }
            }
        }

        private static FileCollection GetFileCollectionFromFileMapping(string baseDirectory, params Tuple<DocumentType, FileMapping>[] files)
        {
            var fileCollection = new FileCollection(baseDirectory);
            foreach(var file in files)
            {
                if (file.Item2 != null)
                {
                    foreach (var mapping in file.Item2.Items)
                    {
                        fileCollection.Add(file.Item1, mapping.CurrentWorkingDirectory, mapping.Files);
                    }
                }
            }

            return fileCollection;
        }

        private static BuildJsonConfig GetConfigFromOptions(BuildCommandOptions options)
        {
            string configFile = options.ConfigFile;
            if (string.IsNullOrEmpty(configFile) && options.Content == null && options.Resource == null)
            {
                if (!File.Exists(Constants.ConfigFileName))
                {
                    throw new ArgumentException("Either provide config file or specify content files to start building documentation.");
                }
                else
                {
                    Logger.Log(LogLevel.Info, $"Config file {Constants.ConfigFileName} found, start building...");
                }
            }
                
            BuildJsonConfig config;
            if (!string.IsNullOrEmpty(configFile))
            {
                var command = (BuildCommand)CommandFactory.ReadConfig(configFile, null).Commands.FirstOrDefault(s => s is BuildCommand);
                if (command == null) throw new ApplicationException($"Unable to find {SubCommandType.Build} subcommand config in file '{Constants.ConfigFileName}'.");
                config = command.Config;
                config.BaseDirectory = Path.GetDirectoryName(configFile);
            }
            else
            {
                config = new BuildJsonConfig();
            }

            string optionsBaseDirectory = Environment.CurrentDirectory;
            // Override config file with options from command line
            if (!string.IsNullOrEmpty(options.Template)) config.Template = options.Template;
            if (!string.IsNullOrEmpty(options.TemplateFolder)) config.TemplateFolder = Path.GetFullPath(options.TemplateFolder);

            if (!string.IsNullOrEmpty(options.TemplateTheme)) config.TemplateTheme = options.TemplateTheme;
            if (!string.IsNullOrEmpty(options.TemplateThemeFolder)) config.TemplateThemeFolder = Path.GetFullPath(options.TemplateThemeFolder);
            if (!string.IsNullOrEmpty(options.OutputFolder)) config.Destination = Path.GetFullPath(Path.Combine(options.OutputFolder, config.Destination ?? string.Empty));
            if (options.Content != null)
            {
                if (config.Content == null)
                    config.Content = new FileMapping(new FileMappingItem());
                config.Content.Add(new FileMappingItem() { Files = new FileItems(options.Content), CurrentWorkingDirectory = optionsBaseDirectory });
            }
            if (options.Resource != null)
            {
                if (config.Resource == null)
                    config.Resource = new FileMapping(new FileMappingItem());
                config.Resource.Add(new FileMappingItem() { Files = new FileItems(options.Resource), CurrentWorkingDirectory = optionsBaseDirectory });
            }
            if (options.Overwrite != null)
            {
                if (config.Overwrite == null)
                    config.Overwrite = new FileMapping(new FileMappingItem());
                config.Overwrite.Add(new FileMappingItem() { Files = new FileItems(options.Overwrite), CurrentWorkingDirectory = optionsBaseDirectory });
            }
            if (options.ExternalReference != null)
            {
                if (config.ExternalReference == null)
                    config.ExternalReference = new FileMapping(new FileMappingItem());
                config.ExternalReference.Add(new FileMappingItem() { Files = new FileItems(options.ExternalReference), CurrentWorkingDirectory = optionsBaseDirectory });
            }
            if (options.Serve) config.Serve = options.Serve;
            if (options.Port.HasValue) config.Port = options.Port.Value.ToString();
            return config;
        }
    }
}
