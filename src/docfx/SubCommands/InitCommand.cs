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
    using Microsoft.DocAsCode.Utility;

    using Newtonsoft.Json;

    internal sealed class InitCommand : ISubCommand
    {
        #region private members

        private const string ConfigName = Constants.ConfigFileName;
        private const string DefaultOutputFolder = "docfx_project";
        private const string DefaultMetadataOutputFolder = "api";
        private static readonly string[] DefaultExcludeFiles = new string[] { "**/bin/**", "**/obj/**", "_site/**" };
        private readonly InitCommandOptions _options;

        private static readonly IEnumerable<IQuestion> _metadataQuestions = new IQuestion[]
        {
            new MultiAnswerQuestion(
                "What are the locations of your source code files?", (s, m, c) =>
                {
                    if (s != null)
                    {
                        var item = new FileMapping(new FileMappingItem(s) { Exclude = new FileItems(DefaultExcludeFiles) });
                        m.Metadata.Add(new MetadataJsonItemConfig
                        {
                             Source = item,
                             Destination = DefaultMetadataOutputFolder,
                        });
                        m.Build.Content = new FileMapping(new FileMappingItem("api/**.yml", "api/index.md"));
                    }
                },
                new string[] { "src/**.csproj" }) {
                Descriptions = new string[]
                {
                    "Supported project files could be .sln, .csproj, .vbproj project files or .cs, .vb source files",
                    Hints.Glob,
                    Hints.Enter,
                }
            },
            new MultiAnswerQuestion(
                "What are the locations of your the markdown files overwriting triple slash comments?", (s, m, c) =>
                {
                    if (s != null)
                    {
                        m.Build.Overwrite = new FileMapping(new FileMappingItem(s) { Exclude = new FileItems(DefaultExcludeFiles) });
                    }
                },
                new string[] { "apidoc/**.md" }) {
                Descriptions = new string[]
                {
                    "You can specify markdown files with YAML header to override summary, remarks and description for parameters",
                    Hints.Glob,
                    Hints.Enter,
                }
            },
        };

        private static readonly IEnumerable<IQuestion> _buildQuestions = new IQuestion[]
        {
            new SingleAnswerQuestion(
                "Where to save the generated documenation?", (s, m, c) => m.Build.Destination = s,
                "_site") {
                Descriptions = new string[]
                {
                    Hints.Enter,
                }
            },
            // TODO: Check if the input glob pattern matches any files
            // IF no matching: WARN [init]: There is no file matching this pattern.
            new MultiAnswerQuestion(
                "What are the locations of your conceputal files?", (s, m, c) =>
                {
                    if (s != null)
                    {
                        if (m.Build.Content == null)
                        {
                            m.Build.Content = new FileMapping();
                        }

                        m.Build.Content.Add(new FileMappingItem(s) { Exclude = new FileItems(DefaultExcludeFiles) });
                    }
                },
                new string[] { "articles/**.md", "articles/**/toc.yml", "toc.yml", "*.md" }) {
                Descriptions = new string[]
                {
                    "Supported conceptual files could be any text files, markdown format is also supported.",
                    Hints.Glob,
                    Hints.Enter,
                }
            },
            new MultiAnswerQuestion(
                "What are the locations of your resource files?", (s, m, c) =>
                {
                    if (s != null)
                    {
                        m.Build.Resource = new FileMapping(new FileMappingItem(s) { Exclude = new FileItems(DefaultExcludeFiles) });
                    }
                },
                new string[] { "images/**" }) {
                Descriptions = new string[]
                {
                    "The resource files which conceptual files are referencing, e.g. images.",
                    Hints.Glob,
                    Hints.Enter,
                }
            },
            new MultiAnswerQuestion(
                "Do you want to specify external API references?", (s, m, c) =>
                {
                    if (s != null)
                    {
                        m.Build.ExternalReference = new FileMapping(new FileMappingItem(s));
                    }
                },
                null) {
                Descriptions = new string[]
                {
                    "Supported external API references can be in either JSON or YAML format.",
                    Hints.Glob,
                    Hints.Enter,
                }
            },
            new MultiAnswerQuestion(
                "What documentation templates to use?", (s, m, c) => { if (s != null) m.Build.Templates.AddRange(s); },
                new string[] { "default" }) {
                Descriptions = new string[]
                {
                    "You can define multiple templates in order, latter one will override former one if name collides",
                    "Predefined templates in docfx are now: default",
                    Hints.Tab,
                    Hints.Enter,
                }
            }
        };

        private static readonly IEnumerable<IQuestion> _selectorQuestions = new IQuestion[]
        {
            new YesOrNoQuestion(
                "Is the generation contains source code files?", (s, m, c) =>
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
        #endregion

        public bool AllowReplay => false;

        public InitCommand(InitCommandOptions options)
        {
            _options = options;
        }

        public void Exec(SubCommandRunningContext context)
        {
            string outputFolder = null;
            try
            {
                var config = new DefaultConfigModel();
                var questionContext = new QuestionContext
                {
                    Quite = _options.Quiet
                };
                foreach (var question in _selectorQuestions)
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

                outputFolder = Path.GetFullPath(string.IsNullOrEmpty(_options.OutputFolder) ? DefaultOutputFolder : _options.OutputFolder).ToDisplayPath();
                bool generate = true;
                if (Directory.Exists(outputFolder))
                {
                    var overrideQuestion = new YesOrNoQuestion(
                        $"Output folder \"{outputFolder}\" already exists, do you still want to generate files into this folder?",
                        (s, m, c) =>
                            {
                                if (!s)
                                {
                                    generate = false;
                                }
                            });
                    overrideQuestion.Process(null, new QuestionContext());
                }
                else
                {
                    Directory.CreateDirectory(outputFolder);
                }

                if (generate)
                {
                    GenerateSeedProject(outputFolder, config);
                }
            }
            catch (Exception e)
            {
                throw new DocfxInitException($"Error init docfx project under \"{outputFolder}\" : {e.Message}", e);
            }
        }

        private static void GenerateSeedProject(string outputFolder, object config)
        {
            // 1. Create default files
            var srcFolder = Path.Combine(outputFolder, "src");
            var apiFolder = Path.Combine(outputFolder, "api");
            var apidocFolder = Path.Combine(outputFolder, "apidoc");
            var articleFolder = Path.Combine(outputFolder, "articles");
            var imageFolder = Path.Combine(outputFolder, "images");
            var folders = new string[] { srcFolder, apiFolder, apidocFolder, articleFolder, imageFolder };
            foreach(var folder in folders)
            {
                Directory.CreateDirectory(folder);
                $"Created folder {folder.ToDisplayPath()}".WriteLineToConsole(ConsoleColor.Gray);
            }

            // 3. Create default files
            // a. toc.yml
            // b. index.md
            // c. articles/toc.yml
            // d. articles/index.md
            var tocYaml = Tuple.Create("toc.yml", @"
- name: Articles
  href: articles/
- name: Api Documentation
  href: api/
  homepage: api/index.md
");
            var indexMarkdownFile = Tuple.Create("index.md", @"
# This is the **HOMEPAGE**.
Refer to [Markdown](http://daringfireball.net/projects/markdown/) for how to write markdown files.
## Quick Start Notes:
1. Add images to *images* folder if the file is referencing an image.
");
            var apiTocFile = Tuple.Create("api/toc.yml", @"
- name: TO BE REPLACED
- href: index.md
");
            var apiIndexFile = Tuple.Create("api/index.md", @"
# PLACEHOLDER
TODO: Add .NET projects to *src* folder and run `docfx` to generate a **REAL** *API Documentation*!
");

            var articleTocFile = Tuple.Create("articles/toc.yml", @"
- name: Introduction
  href: intro.md
");
            var articleMarkdownFile = Tuple.Create("articles/intro.md", @"
# Add your introductions here!
");

            var files = new Tuple<string, string>[] { tocYaml, indexMarkdownFile, apiTocFile, apiIndexFile, articleTocFile, articleMarkdownFile };
            foreach(var file in files)
            {
                var filePath = Path.Combine(outputFolder, file.Item1);
                var content = file.Item2;
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(filePath, content);
                $"Created File {filePath.ToDisplayPath()}".WriteLineToConsole(ConsoleColor.Gray);
            }

            // 2. Create docfx.json
            var path = Path.Combine(outputFolder, ConfigName);
            JsonUtility.Serialize(path, config, Formatting.Indented);
            $"Created config file {path.ToDisplayPath()}".WriteLineToConsole(ConsoleColor.Gray);

            $"Successfully generated default docfx project to {outputFolder.ToDisplayPath()}".WriteLineToConsole(ConsoleColor.Green);
            "Please run:".WriteLineToConsole(ConsoleColor.Gray);
            $"\tdocfx \"{path.ToDisplayPath()}\" --serve".WriteLineToConsole(ConsoleColor.White);
            "To generate a default docfx website.".WriteLineToConsole(ConsoleColor.Gray);
        }

        #region Question classes

        private sealed class YesOrNoQuestion : SingleChoiceQuestion<bool>
        {
            private const string YesAnswer = "Yes";
            private const string NoAnswer = "No";
            private static readonly string[] YesOrNoAnswer = new string[] { YesAnswer, NoAnswer };
            public YesOrNoQuestion(string content, Action<bool, DefaultConfigModel, QuestionContext> setter) : base(content, setter, Converter, YesOrNoAnswer)
            {
            }

            private static bool Converter(string input)
            {
                return input == YesAnswer;
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
                if (converter == null) throw new ArgumentNullException(nameof(converter));
                _converter = converter;
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
                if (context.Quite)
                {
                    _setter(DefaultValue, model, context);
                }
                else
                {
                    WriteQuestion();
                    var value = GetAnswer();
                    _setter(value, model, context);
                }
            }

            protected abstract T GetAnswer();

            protected void WriteQuestion()
            {
                Content.WriteToConsole(ConsoleColor.White);
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
            public bool Quite { get; set; }
            public bool ContainsMetadata { get; set; }
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
