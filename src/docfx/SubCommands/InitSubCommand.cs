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

    class InitSubCommand : ISubCommand
    {
        private const string ConfigName = "xdoc.json";
        private static List<IQuestion> _questions = new List<IQuestion> {
            new SingleAnswerQuestion(
                "What is the title of your documentation?", (s, m) => m.Title = s,
                "Doc-as-code documentation") {
                 Descriptions = new string[]
                 {
                     Hints.Enter,
                 }
            },
            new SingleAnswerQuestion(
                "Where to save the generated documenation?", (s, m) => m.OutputFolder = s,
                "xdoc") {
                 Descriptions = new string[]
                 {
                     Hints.Enter,
                 }
            },
            new MultiAnswerQuestion(
                "What are the locations of your source project files?", (s, m) => { m.Projects= new FileMapping(); if (s != null) m.Projects.Add(new FileMappingItem { Files = new FileItems(s), Name = "api" }); },
                new string[] { "**/*.csproj", "**/*.vbproj" }) {
                 Descriptions = new string[]
                 {
                     "Supported project files could be .sln, .csproj, .vbproj project files or .cs, .vb source files",
                     Hints.Glob,
                     Hints.Empty,
                 }
            },
            // TODO: Check if the input glob pattern matches any files
            // IF no matching: WARN [init]: There is no file matching this pattern.
            new MultiAnswerQuestion(
                "What are the locations of your conceputal files?", (s, m) => { m.Conceptuals = new FileMapping(); if (s != null) m.Conceptuals.Add(new FileMappingItem { Files = new FileItems(s) }); },
                new string[] { "**/*.md" }) {
                 Descriptions = new string[]
                 {
                     "Supported conceputal files could be any text files, markdown format is also supported.",
                     "Note that the referenced files, e.g. images or code snippet files should also be included.",
                     Hints.Glob,
                     Hints.Empty,
                 }
            },
            new MultiAnswerQuestion(
                "Do you want to specify external API references?", (s, m) => { m.ExternalReferences = new FileMapping(); if (s != null) m.ExternalReferences.Add(new FileMappingItem { Files = new FileItems(s) }); },
                null) {
                 Descriptions = new string[]
                 {
                     "Supported external API references can be in either JSON or YAML format.",
                     Hints.Glob,
                     Hints.Empty,
                 }
            },
            new SingleChoiceQuestion(
                "Where will you host your website?", (s, m) => { TemplateType type; if( Enum.TryParse(s, true, out type))  m.TemplateType = type; },
                TemplateType.Base.ToString(), TemplateType.Github.ToString(), TemplateType.IIS.ToString()) {
                 Descriptions = new string[]
                 {
                     "Xdoc provides additional files required for different host",
                     Hints.Tab,
                     Hints.Empty,
                 }
            }
        };

        public ParseResult Exec(Options options)
        {
            string name = null;
            string path = null;
            try
            {

                ConfigModel config = new ConfigModel();
                var initVerb = options.InitVerb;
                if (initVerb.Quiet)
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

                name = string.IsNullOrEmpty(initVerb.Name) ? ConfigName : initVerb.Name;
                path = string.IsNullOrEmpty(initVerb.OutputFolder) ? name : Path.Combine(initVerb.OutputFolder, name);

                JsonUtility.Serialize(path, config, Formatting.Indented);
                return new ParseResult(ResultLevel.Success, "Generated {0} to {1}", name, path);
            }
            catch (Exception e)
            {
                return new ParseResult(ResultLevel.Error, "Error init {0}: {1}", name ?? ConfigName, e.Message);
            }
        }

        private void ProcessSingleQuestion(SingleQuestion question, ConfigModel model)
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

        private void ProcessMultiQuestion(MultiQuestion question, ConfigModel model)
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

            public SingleChoiceQuestion(string content, Action<string, ConfigModel> setter, params string[] options) : base(content, setter, options == null || options.Length == 0 ? null : options[0])
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

            public MultiAnswerQuestion(string content, Action<string[], ConfigModel> setter, string[] defaultAnswer = null) : base(content, setter, defaultAnswer)
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
             

            public SingleAnswerQuestion(string content, Action<string, ConfigModel> setter, string defaultAnswer = null) : base(content, setter, defaultAnswer)
            {
            }
        }

        abstract class MultiQuestion : Question<string[]>
        {
            public MultiQuestion(string content, Action<string[], ConfigModel> setter, string[] defaultValue) : base(content, setter, defaultValue)
            {
            }
        }

        abstract class SingleQuestion : Question<string>
        {

            public SingleQuestion(string content, Action<string, ConfigModel> setter, string defaultValue) : base(content, setter, defaultValue)
            {
            }

        }

        abstract class Question<T> : Question, IQuestion
        {
            public Action<T, ConfigModel> Setter { get; private set; }

            public T DefaultAnswer { get; private set; }

            public Question(string content, Action<T, ConfigModel> setter, T defaultValue) : base(content)
            {
                this.DefaultAnswer = defaultValue;
                this.Setter = setter;
            }

            public void SetDefault(ConfigModel model)
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
            void SetDefault(ConfigModel model);
        }
    }
}
