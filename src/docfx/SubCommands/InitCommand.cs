// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json;

    internal sealed class InitCommand : ISubCommand
    {
        #region private members

        private const string ConfigName = Constants.ConfigFileName;
        private const string DefaultOutputFolder = "docfx_project";
        private const string DefaultMetadataOutputFolder = "api";
        private static readonly string[] DefaultExcludeFiles = new string[] { "obj/**" };
        private static readonly string[] DefaultSrcExcludeFiles = new string[] { "**/obj/**", "**/bin/**" };
        private readonly InitCommandOptions _options;
        private readonly IEnumerable<IQuestion> _metadataQuestions;

        private readonly IEnumerable<IQuestion> _overallQuestion;

        private readonly IEnumerable<IQuestion> _buildQuestions;

        private readonly IEnumerable<IQuestion> _selectorQuestions;
        #endregion

        public string Name { get; } = nameof(InitCommand);

        public bool AllowReplay => false;

        public InitCommand(InitCommandOptions options)
        {
            _options = options;
            _metadataQuestions = new IQuestion[]
            {
                new MultiAnswerQuestion(
                    "What are the locations of your source code files?",
                    (s, m, c) =>
                    {
                        if (s != null)
                        {
                            var item = new FileMapping(
                                new FileMappingItem(s)
                                {
                                    SourceFolder = options.ApiSourceFolder
                                });
                            m.Metadata.Add(new MetadataJsonItemConfig
                            {
                                 Source = item,
                                 Destination = DefaultMetadataOutputFolder,
                            });
                            m.Build.Content = new FileMapping(new FileMappingItem("api/**.yml", "api/index.md"));
                        }
                    },
                    new string[] { options.ApiSourceGlobPattern ?? "src/**.csproj" })
                    {
                        Descriptions = new string[]
                        {
                            "Supported project files could be .sln, .csproj, .vbproj project files, or assembly files .dll, or .cs, .vb source files",
                            Hints.Glob,
                            Hints.Enter,
                        }
                    },
                new MultiAnswerQuestion(
                    "What are the locations of your markdown files overwriting triple slash comments?",
                    (s, m, _) =>
                    {
                        if (s != null)
                        {
                            var exclude = new FileItems(DefaultExcludeFiles);
                            if(!string.IsNullOrEmpty(m.Build.Destination))
                            {
                                exclude.Add($"{m.Build.Destination}/**");
                            }
                            m.Build.Overwrite = new FileMapping(new FileMappingItem(s) { Exclude = exclude });
                        }
                    },
                    new string[] { "apidoc/**.md" })
                    {
                        Descriptions = new string[]
                        {
                            "You can specify markdown files with a YAML header to overwrite summary, remarks and description for parameters",
                            Hints.Glob,
                            Hints.Enter,
                        }
                    },
            };
            _overallQuestion = new IQuestion[]
             {
                new SingleAnswerQuestion(
                    "Where to save the generated documentation?",
                    (s, m, _) => m.Build.Destination = s,
                    "_site")
                {
                    Descriptions = new string[]
                    {
                        Hints.Enter,
                    }
                },
             };
            _buildQuestions = new IQuestion[]
             {
                // TODO: Check if the input glob pattern matches any files
                // IF no matching: WARN [init]: There is no file matching this pattern.
                new MultiAnswerQuestion(
                    "What are the locations of your conceptual files?",
                    (s, m, _) =>
                    {
                        if (s != null)
                        {
                            if (m.Build.Content == null)
                            {
                                m.Build.Content = new FileMapping();
                            }

                            m.Build.Content.Add(new FileMappingItem(s));
                        }
                    },
                    new string[] { "articles/**.md", "articles/**/toc.yml", "toc.yml", "*.md" })
                {
                    Descriptions = new string[]
                    {
                        "Supported conceptual files could be any text files. Markdown format is also supported.",
                        Hints.Glob,
                        Hints.Enter,
                    }
                },
                new MultiAnswerQuestion(
                    "What are the locations of your resource files?",
                    (s, m, _) =>
                    {
                        if (s != null)
                        {
                            m.Build.Resource = new FileMapping(new FileMappingItem(s));
                        }
                    },
                    new string[] { "images/**" })
                {
                    Descriptions = new string[]
                    {
                        "The resource files which conceptual files are referencing, e.g. images.",
                        Hints.Glob,
                        Hints.Enter,
                    }
                },
                new MultiAnswerQuestion(
                    "Do you want to specify external API references?",
                    (s, m, _) =>
                    {
                        if (s != null)
                        {
                            m.Build.XRefMaps = new ListWithStringFallback(s);
                        }
                    },
                    null)
                {
                    Descriptions = new string[]
                    {
                        "Supported external API references can be in either JSON or YAML format.",
                        Hints.Enter,
                    }
                },
                new MultiAnswerQuestion(
                    "What documentation templates do you want to use?",
                    (s, m, _) => { if (s != null) m.Build.Templates.AddRange(s); },
                    new string[] { "default" })
                {
                    Descriptions = new string[]
                    {
                        "You can define multiple templates in order. The latter one will overwrite the former one if names collide",
                        "Predefined templates in docfx are now: default, statictoc",
                        Hints.Enter,
                    }
                }
             };
            _selectorQuestions = new IQuestion[]
             {
                new YesOrNoQuestion(
                    "Does the website contain API documentation from source code?", (s, m, c) =>
                    {
                        m.Build = new BuildJsonConfig();
                        if (s)
                        {
                            m.Metadata = new MetadataJsonConfig();
                            c.ContainsMetadata = true;
                        }
                        else
                        {
                            c.ContainsMetadata = false;
                        }
                    }),
             };
        }

        public void Exec(SubCommandRunningContext context)
        {
            string outputFolder = null;
            try
            {
                var config = new DefaultConfigModel();
                var questionContext = new QuestionContext
                {
                    Quiet = _options.Quiet
                };
                foreach (var question in _selectorQuestions)
                {
                    question.Process(config, questionContext);
                }

                foreach (var question in _overallQuestion)
                {
                    question.Process(config, questionContext);
                }

                if (questionContext.ContainsMetadata)
                {
                    foreach (var question in _metadataQuestions)
                    {
                        question.Process(config, questionContext);
                    }
                }

                foreach (var question in _buildQuestions)
                {
                    question.Process(config, questionContext);
                }
                config.Build.MarkdownEngineName = "markdig";

                if (_options.OnlyConfigFile)
                {
                    GenerateConfigFile(_options.OutputFolder, config, _options.Quiet, _options.Overwrite);
                }
                else
                {
                    outputFolder = Path.GetFullPath(string.IsNullOrEmpty(_options.OutputFolder) ? DefaultOutputFolder : _options.OutputFolder).ToDisplayPath();
                    GenerateSeedProject(outputFolder, config, _options.Quiet, _options.Overwrite);
                }
            }
            catch (Exception e)
            {
                throw new DocfxInitException($"Error with init docfx project under \"{outputFolder}\" : {e.Message}", e);
            }
        }

        private void GenerateConfigFile(string outputFolder, object config, bool quiet, bool overwrite)
        {
            var path = Path.Combine(outputFolder ?? string.Empty, ConfigName).ToDisplayPath();
            if (File.Exists(path))
            {
                if (!ProcessOverwriteQuestion($"Config file \"{path}\" already exists, do you want to overwrite this file?", quiet, overwrite))
                {
                    return;
                }
            }

            SaveConfigFile(path, config);
            $"Successfully generated default docfx config file to {path}".WriteLineToConsole(ConsoleColor.Green);
        }

        private void GenerateSeedProject(string outputFolder, DefaultConfigModel config, bool quiet, bool overwrite)
        {
            if (Directory.Exists(outputFolder))
            {
                if (!ProcessOverwriteQuestion($"Output folder \"{outputFolder}\" already exists. Do you still want to generate files into this folder? You can use -o command option to specify the folder name", quiet, true))
                {
                    return;
                }
            }
            else
            {
                Directory.CreateDirectory(outputFolder);
            }

            // 1. Create default files
            var srcFolder = Path.Combine(outputFolder, "src");
            var apiFolder = Path.Combine(outputFolder, "api");
            var apidocFolder = Path.Combine(outputFolder, "apidoc");
            var articleFolder = Path.Combine(outputFolder, "articles");
            var imageFolder = Path.Combine(outputFolder, "images");
            var folders = new string[] { srcFolder, apiFolder, apidocFolder, articleFolder, imageFolder };
            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                    $"Created folder {folder.ToDisplayPath()}".WriteLineToConsole(ConsoleColor.Gray);
                }
            }

            // 2. Create default files
            // a. toc.yml
            // b. index.md
            // c. articles/toc.yml
            // d. articles/index.md
            // e. .gitignore
            // f. api/.gitignore
            // TODO: move api/index.md out to some other folder
            var tocYaml = Tuple.Create("toc.yml", @"- name: Articles
  href: articles/
- name: Api Documentation
  href: api/
  homepage: api/index.md
");
            var indexMarkdownFile = Tuple.Create("index.md", @"# This is the **HOMEPAGE**.
Refer to [Markdown](http://daringfireball.net/projects/markdown/) for how to write markdown files.
## Quick Start Notes:
1. Add images to the *images* folder if the file is referencing an image.
");
            var apiTocFile = Tuple.Create("api/toc.yml", @"- name: TO BE REPLACED
  href: index.md
");
            var apiIndexFile = Tuple.Create("api/index.md", @"# PLACEHOLDER
TODO: Add .NET projects to the *src* folder and run `docfx` to generate **REAL** *API Documentation*!
");

            var articleTocFile = Tuple.Create("articles/toc.yml", @"- name: Introduction
  href: intro.md
");
            var articleMarkdownFile = Tuple.Create("articles/intro.md", @"# Add your introductions here!
");
            var gitignore = Tuple.Create(".gitignore", $@"###############
#    folder   #
###############
/**/DROP/
/**/TEMP/
/**/packages/
/**/bin/
/**/obj/
{config.Build.Destination}
");
            var apiGitignore = Tuple.Create("api/.gitignore", $@"###############
#  temp file  #
###############
*.yml
.manifest
");
            var files = new Tuple<string, string>[] { tocYaml, indexMarkdownFile, apiTocFile, apiIndexFile, articleTocFile, articleMarkdownFile, gitignore, apiGitignore };
            foreach (var file in files)
            {
                var filePath = Path.Combine(outputFolder, file.Item1);

                if (overwrite || !File.Exists(filePath))
                {
                    var content = file.Item2;
                    var dir = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    File.WriteAllText(filePath, content);
                    $"Created File {filePath.ToDisplayPath()}".WriteLineToConsole(ConsoleColor.Gray);
                }
            }

            // 2. Create docfx.json
            var path = Path.Combine(outputFolder ?? string.Empty, ConfigName);
            if (overwrite || !File.Exists(path))
            {
                SaveConfigFile(path, config);
                $"Created config file {path.ToDisplayPath()}".WriteLineToConsole(ConsoleColor.Gray);
            }

            $"Successfully generated default docfx project to {outputFolder.ToDisplayPath()}".WriteLineToConsole(ConsoleColor.Green);
            "Please run:".WriteLineToConsole(ConsoleColor.Gray);
            $"\tdocfx \"{path.ToDisplayPath()}\" --serve".WriteLineToConsole(ConsoleColor.White);
            "To generate a default docfx website.".WriteLineToConsole(ConsoleColor.Gray);
        }

        private static void SaveConfigFile(string path, object config)
        {
            JsonUtility.Serialize(path, config, Formatting.Indented);
        }

        private static bool ProcessOverwriteQuestion(string message, bool quiet, bool overwriteResult)
        {
            bool overwrited = true;

            IQuestion overwriteQuestion;
            if (overwriteResult)
            {
                overwriteQuestion = new YesOrNoQuestion(
                message,
                (s, m, c) =>
                {
                    if (!s)
                    {
                        overwrited = false;
                    }
                });
            }
            else
            {
                overwriteQuestion = new NoOrYesQuestion(
                message,
                (s, m, c) =>
                {
                    if (!s)
                    {
                        overwrited = false;
                    }
                });
            }

            overwriteQuestion.Process(null, new QuestionContext { NeedWarning = overwrited, Quiet = quiet });

            return overwrited;
        }

        #region Question classes

        private static class YesOrNoOption
        {
            public const string YesAnswer = "Yes";
            public const string NoAnswer = "No";
        }

        /// <summary>
        /// the default option is Yes
        /// </summary>
        private sealed class YesOrNoQuestion : SingleChoiceQuestion<bool>
        {
            private static readonly string[] YesOrNoAnswer = { YesOrNoOption.YesAnswer, YesOrNoOption.NoAnswer };
            public YesOrNoQuestion(string content, Action<bool, DefaultConfigModel, QuestionContext> setter) : base(content, setter, Converter, YesOrNoAnswer)
            {
            }

            private static bool Converter(string input)
            {
                return input == YesOrNoOption.YesAnswer;
            }
        }

        /// <summary>
        /// the default option is No
        /// </summary>
        private sealed class NoOrYesQuestion : SingleChoiceQuestion<bool>
        {
            private static readonly string[] NoOrYesAnswer = { YesOrNoOption.NoAnswer, YesOrNoOption.YesAnswer };

            public NoOrYesQuestion(string content, Action<bool, DefaultConfigModel, QuestionContext> setter) : base(content, setter, Converter, NoOrYesAnswer)
            {
            }

            private static bool Converter(string input)
            {
                return input == YesOrNoOption.YesAnswer;
            }
        }

        private class SingleChoiceQuestion<T> : Question<T>
        {
            private Func<string, T> _converter;
            /// <summary>
            /// Options, the first one as the default one
            /// </summary>
            public string[] Options { get; set; }

            public SingleChoiceQuestion(string content, Action<T, DefaultConfigModel, QuestionContext> setter, Func<string, T> converter, params string[] options)
                : base(content, setter)
            {
                if (options == null || options.Length == 0) throw new ArgumentNullException(nameof(options));
                _converter = converter ?? throw new ArgumentNullException(nameof(converter));
                Options = options;
                DefaultAnswer = options[0];
                DefaultValue = converter(DefaultAnswer);
            }

            protected override T GetAnswer()
            {
                var options = Options;
                Console.Write("Choose Answer ({0}): ", string.Join("/", options));
                Console.Write(DefaultAnswer[0]);
                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                string matched = null;

                var line = Console.ReadLine();
                while (!string.IsNullOrEmpty(line))
                {
                    matched = GetMatchedOption(options, line);
                    if (matched == null)
                    {
                        Console.Write("Invalid Answer, please reenter: ");
                    }
                    else
                    {
                        return _converter(matched);
                    }

                    line = Console.ReadLine();
                }

                return DefaultValue;
            }

            private static string GetMatchedOption(string[] options, string input)
            {
                return options.FirstOrDefault(s => s.Equals(input, StringComparison.OrdinalIgnoreCase) || s.Substring(0, 1).Equals(input, StringComparison.OrdinalIgnoreCase));
            }
        }

        private sealed class MultiAnswerQuestion : Question<string[]>
        {
            public MultiAnswerQuestion(string content, Action<string[], DefaultConfigModel, QuestionContext> setter, string[] defaultValue = null)
                : base(content, setter)
            {
                DefaultValue = defaultValue;
                DefaultAnswer = ConvertToString(defaultValue);
            }

            protected override string[] GetAnswer()
            {
                var line = Console.ReadLine();
                List<string> answers = new List<string>();
                while (!string.IsNullOrEmpty(line))
                {
                    answers.Add(line);
                    line = Console.ReadLine();
                }

                if (answers.Count > 0)
                {
                    return answers.ToArray();
                }
                else
                {
                    return DefaultValue;
                }
            }

            private static string ConvertToString(string[] array)
            {
                if (array == null) return null;
                return string.Join(",", array);
            }
        }

        private sealed class SingleAnswerQuestion : Question<string>
        {
            public SingleAnswerQuestion(string content, Action<string, DefaultConfigModel, QuestionContext> setter, string defaultAnswer = null)
                : base(content, setter)
            {
                DefaultValue = defaultAnswer;
                DefaultAnswer = defaultAnswer;
            }

            protected override string GetAnswer()
            {
                var line = Console.ReadLine();
                if (!string.IsNullOrEmpty(line))
                {
                    return line;
                }
                else
                {
                    return DefaultValue;
                }
            }
        }

        private abstract class Question<T> : IQuestion
        {
            private Action<T, DefaultConfigModel, QuestionContext> _setter { get; }

            public string Content { get; }

            /// <summary>
            /// Each string stands for one line
            /// </summary>
            public string[] Descriptions { get; set; }

            public T DefaultValue { get; protected set; }
            public string DefaultAnswer { get; protected set; }

            public Question(string content, Action<T, DefaultConfigModel, QuestionContext> setter)
            {
                if (setter == null) throw new ArgumentNullException(nameof(setter));
                Content = content;
                _setter = setter;
            }

            public void Process(DefaultConfigModel model, QuestionContext context)
            {
                if (context.Quiet)
                {
                    _setter(DefaultValue, model, context);
                }
                else
                {
                    WriteQuestion(context);
                    var value = GetAnswer();
                    _setter(value, model, context);
                }
            }

            protected abstract T GetAnswer();

            private void WriteQuestion(QuestionContext context)
            {
                Content.WriteToConsole(context.NeedWarning ? ConsoleColor.Yellow : ConsoleColor.White);
                WriteDefaultAnswer();
                Descriptions.WriteLinesToConsole(ConsoleColor.Gray);
            }

            private void WriteDefaultAnswer()
            {
                if (DefaultAnswer == null)
                {
                    Console.WriteLine();
                    return;
                }

                " (Default: ".WriteToConsole(ConsoleColor.Gray);
                DefaultAnswer.WriteToConsole(ConsoleColor.Green);
                ")".WriteLineToConsole(ConsoleColor.Gray);
            }
        }

        private interface IQuestion
        {
            void Process(DefaultConfigModel model, QuestionContext context);
        }

        private sealed class QuestionContext
        {
            public bool Quiet { get; set; }
            public bool ContainsMetadata { get; set; }
            public bool NeedWarning { get; set; }
        }

        #endregion

        private static class Hints
        {
            public const string Tab = "Press TAB to list possible options.";
            public const string Enter = "Press ENTER to move to the next question.";
            public const string Glob = "You can use glob patterns, e.g. src/**";
        }

        private class DefaultConfigModel
        {
            [JsonProperty("metadata")]
            public MetadataJsonConfig Metadata { get; set; }

            [JsonProperty("build")]
            public BuildJsonConfig Build { get; set; }
        }
    }
}
