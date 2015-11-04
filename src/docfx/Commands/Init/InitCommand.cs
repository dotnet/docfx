// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using Microsoft.DocAsCode.EntityModel;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    public class DefaultConfigModel
    {
        [JsonProperty("metadata")]
        public MetadataJsonConfig Metadata { get; set; }

        [JsonProperty("build")]
        public BuildJsonConfig Build { get; set; }
    }

    class InitCommand : ICommand
    {
        private const string ConfigName = Constants.ConfigFileName;
        private static List<IQuestion> _questions = new List<IQuestion> {
            new SingleAnswerQuestion(
                "Is the generation contains conceptual files only?", (s, m) => {
                    m.Build = new BuildJsonConfig();
                    if (s == "No" || s == "N") 
                       m.Metadata = new MetadataJsonConfig();
                    },
                "Yes")
            {
                Descriptions = new string[]
                {
                    "Yes", 
                    "No"
                }
            },
            new SingleAnswerQuestion(
                "What is the title of your documentation?", (s, m) => m.Build.Title = s,
                "Doc-as-code documentation") {
                 Descriptions = new string[]
                 {
                     Hints.Enter,
                 }
            },
            new SingleAnswerQuestion(
                "Where to save the generated documenation?", (s, m) => m.Build.Destination = s,
                "_site") {
                 Descriptions = new string[]
                 {
                     Hints.Enter,
                 }
            },
            // TODO: enable metadata from the first option
            //new MultiAnswerQuestion(
            //    "What are the locations of your files?", (s, m) => { m.Metadata.Content= new FileMapping(); if (s != null) m.Projects.Add(new FileMappingItem { Files = new FileItems(s), Name = "api" }); },
            //    new string[] { "**/*.csproj", "**/*.vbproj" }) {
            //     Descriptions = new string[]
            //     {
            //         "Supported project files could be .sln, .csproj, .vbproj project files or .cs, .vb source files",
            //         Hints.Glob,
            //         Hints.Empty,
            //     }
            //},
            // TODO: Check if the input glob pattern matches any files
            // IF no matching: WARN [init]: There is no file matching this pattern.
            new MultiAnswerQuestion(
                "What are the locations of your conceputal files?", (s, m) => { m.Build.Content = new FileMapping(); if (s != null) m.Build.Content.Add(new FileMappingItem { Files = new FileItems(s) }); },
                new string[] { "articles/**/*.md" }) {
                 Descriptions = new string[]
                 {
                     "Supported conceptual files could be any text files, markdown format is also supported.",
                     Hints.Glob,
                     Hints.Empty,
                 }
            },
            new MultiAnswerQuestion(
                "What are the locations of your resource files?", (s, m) => { m.Build.Resource = new FileMapping(); if (s != null) m.Build.Resource.Add(new FileMappingItem { Files = new FileItems(s) }); },
                new string[] { "images/**" }) {
                 Descriptions = new string[]
                 {
                     "The resource files which conceptual files are referencing, e.g. images.",
                     Hints.Glob,
                     Hints.Empty,
                 }
            },
            new MultiAnswerQuestion(
                "Do you want to specify external API references?", (s, m) => { m.Build.ExternalReference = new FileMapping(); if (s != null) m.Build.ExternalReference.Add(new FileMappingItem { Files = new FileItems(s) }); },
                null) {
                 Descriptions = new string[]
                 {
                     "Supported external API references can be in either JSON or YAML format.",
                     Hints.Glob,
                     Hints.Empty,
                 }
            },
            new MultiAnswerQuestion(
                "What documentation templates to use?", (s, m) => { if (s != null) m.Build.Templates.AddRange(s); },
                new string[] { "default" }) {
                 Descriptions = new string[]
                 {
                     "You can define multiple templates in order, latter one will override former one if name collides",
                     "There are several predefined templates in docfx: default, op.html, msdn.html, angular",
                     Hints.Tab,
                     Hints.Empty,
                 }
            }
        };

        private CommandContext _context;
        public InitCommandOptions _options { get; }
        public Options _rootOptions { get; }
        public InitCommand(Options options, CommandContext context)
        {
            _options = options.InitCommand;
            _context = context;
            _rootOptions = options;
        }

        public ParseResult Exec(RunningContext context)
        {
            string name = null;
            string path = null;
            try
            {
                DefaultConfigModel config = new DefaultConfigModel();
                if (_options.Quiet)
                {
                    // Generate a default Config
                    foreach (var question in _questions)
                    {
                        question.SetDefault(config);
                    }
                }
                else
                {
                    foreach (var question in _questions)
                    {
                        if (question is SingleQuestion)
                        {
                            ProcessSingleQuestion((SingleQuestion)question, config);
                        }
                        else
                        {
                            ProcessMultiQuestion((MultiQuestion)question, config);
                        }

                    }
                }

                name = string.IsNullOrEmpty(_options.Name) ? ConfigName : _options.Name;
                path = string.IsNullOrEmpty(_options.OutputFolder) ? name : Path.Combine(_options.OutputFolder, name);

                JsonUtility.Serialize(path, config, Formatting.Indented);
                return new ParseResult(ResultLevel.Success, "Generated {0} to {1}", name, path);
            }
            catch (Exception e)
            {
                return new ParseResult(ResultLevel.Error, "Error init {0}: {1}", name ?? ConfigName, e.Message);
            }
        }

        private void ProcessSingleQuestion(SingleQuestion question, DefaultConfigModel model)
        {
            WriteContent(question.Content);
            WriteDefaultAnswer(question.DefaultAnswer);
            WriteDescription(question.Descriptions);
            var singleChoice = question as SingleChoiceQuestion;
            if (singleChoice != null)
            {
                var options = singleChoice.Options;
                Console.Write("Choose Answer ({0}): ", string.Join("/", options));
                Console.Write(singleChoice.DefaultAnswer[0]);
                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                string matched;
                do
                {
                    var input = Console.ReadLine();
                    if (string.IsNullOrEmpty(input))
                    {
                        question.SetDefault(model);
                        break;
                    }
                    else
                    {
                        matched = GetMatchedOption(options, input);
                        if (matched == null)
                            Console.Write("Invalid Answer, please reenter: ");
                        else
                        {
                            question.Setter(matched, model);
                        }
                    }
                }
                while (matched == null);
            }
            else
            {
                var line = Console.ReadLine();
                if (!string.IsNullOrEmpty(line))
                {
                    question.Setter(line, model);
                }
                else
                {
                    question.SetDefault(model);
                }
            }
        }

        private void ProcessMultiQuestion(MultiQuestion question, DefaultConfigModel model)
        {
            WriteContent(question.Content);
            WriteDefaultAnswer(question.DefaultAnswer);
            WriteDescription(question.Descriptions);
            var multiAnswer = question as MultiAnswerQuestion;
            if (multiAnswer != null)
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
                    question.Setter(answers.ToArray(), model);
                }
                else
                {
                    question.SetDefault(model);
                }
            }
        }

        private void WriteDefaultAnswer(params string[] defaultAnswers)
        {
            if (defaultAnswers == null || defaultAnswers.Length == 0) return;
            string defaultAnswerString = string.Join(",", defaultAnswers);
            " (Default: ".WriteToConsole(ConsoleColor.Gray);
            defaultAnswerString.WriteToConsole(ConsoleColor.Green);
            ")".WriteLineToConsole(ConsoleColor.Gray);
        }

        private void WriteDescription(params string[] description)
        {
            description.WriteLinesToConsole(ConsoleColor.Gray);
        }

        private void WriteContent(string content)
        {
            content.WriteToConsole(ConsoleColor.White);
        }

        private string GetMatchedOption(string[] options, string input)
        {
            var matched = options.FirstOrDefault(s => s.Equals(input, StringComparison.OrdinalIgnoreCase));
            if (matched == null)
            {
                matched = options.FirstOrDefault(s => s.Substring(0, 1).Equals(input, StringComparison.OrdinalIgnoreCase));
            }

            return matched;
        }

        static class Hints
        {
            public const string Tab = "Press tab to list possible options.";
            public const string Enter = "Enter to move to the next question.";
            public const string Empty = "Enter empty string to move to the next question.";
            public const string Glob = "You can use glob patterns, eg. src/**/*.cs";
        }

        enum QuestionType
        {
            Choice,
            SingleLineAnswer,
            MultiLineAnswer,
            Container
        }

        class SingleChoiceQuestion : SingleQuestion
        {
            public override QuestionType QuestionType
            {
                get
                {
                    return QuestionType.Choice;
                }
            }

            /// <summary>
            /// Options, the first one as the default one
            /// </summary>
            public string[] Options { get; set; }

            public SingleChoiceQuestion(string content, Action<string, DefaultConfigModel> setter, params string[] options) : base(content, setter, options == null || options.Length == 0 ? null : options[0])
            {
                Options = options;
            }
        }

        class MultiAnswerQuestion : MultiQuestion
        {
            public override QuestionType QuestionType
            {
                get
                {
                    return QuestionType.MultiLineAnswer;
                }
            }

            public MultiAnswerQuestion(string content, Action<string[], DefaultConfigModel> setter, string[] defaultAnswer = null) : base(content, setter, defaultAnswer)
            {
            }
        }

        class SingleAnswerQuestion : SingleQuestion
        {
            public override QuestionType QuestionType
            {
                get
                {
                    return QuestionType.SingleLineAnswer;
                }
            }


            public SingleAnswerQuestion(string content, Action<string, DefaultConfigModel> setter, string defaultAnswer = null) : base(content, setter, defaultAnswer)
            {
            }
        }

        abstract class MultiQuestion : Question<string[]>
        {
            public MultiQuestion(string content, Action<string[], DefaultConfigModel> setter, string[] defaultValue) : base(content, setter, defaultValue)
            {
            }
        }

        abstract class SingleQuestion : Question<string>
        {

            public SingleQuestion(string content, Action<string, DefaultConfigModel> setter, string defaultValue) : base(content, setter, defaultValue)
            {
            }

        }

        abstract class Question<T> : Question, IQuestion
        {
            public Action<T, DefaultConfigModel> Setter { get; private set; }

            public T DefaultAnswer { get; private set; }

            public Question(string content, Action<T, DefaultConfigModel> setter, T defaultValue) : base(content)
            {
                this.DefaultAnswer = defaultValue;
                this.Setter = setter;
            }

            public void SetDefault(DefaultConfigModel model)
            {
                Setter(DefaultAnswer, model);
            }
        }

        abstract class Question
        {
            public abstract QuestionType QuestionType { get; }

            public string Content { get; private set; }

            /// <summary>
            /// Each string stands for one line
            /// </summary>
            public string[] Descriptions { get; set; }

            public Question(string content)
            {
                this.Content = content;
            }
        }

        interface IQuestion
        {
            void SetDefault(DefaultConfigModel model);
        }
    }
}
