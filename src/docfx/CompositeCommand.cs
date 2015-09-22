// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.Utility;
    using System.IO;
    using Newtonsoft.Json.Linq;

    class CompositeCommand : ICommand
    {
        public IList<ICommand> Commands { get; }

        public CompositeCommand(params ICommand[] commands)
        {
            Commands = commands;
        }

        public CompositeCommand(IList<ICommand> commands)
        {
            Commands = commands;
        }

        public CompositeCommand(JToken value) : this(CommandFactory.ConvertJTokenTo<Dictionary<string, JToken>>(value))
        {

        }

        public CompositeCommand(Dictionary<string, JToken> commands)
        {
            var dictionary = new SortedDictionary<SubCommandType, JToken>();
            foreach (var pair in commands)
            {
                SubCommandType subCommandType;
                if (Enum.TryParse(pair.Key, true, out subCommandType))
                {
                    if (dictionary.ContainsKey(subCommandType))
                    {
                        ParseResult.WriteToConsole(ResultLevel.Warning, $"{subCommandType} is defined in config file for several times, the first config is used. NOTE that key is case insensitive.");
                    }
                    else
                    {
                        dictionary.Add(subCommandType, pair.Value);
                    }
                }
                else
                {
                    ParseResult.WriteToConsole(ResultLevel.Info, $"{pair.Value} is not a valid command currently supported, ignored.");
                }
            }

            // Order is now defined in SubCommandType
            Commands = dictionary.Select(s => CommandFactory.GetCommand<JToken>(s.Key, s.Value)).ToList();
        }

        public ParseResult Exec(RunningContext context)
        {
            return AggregateParseResult(YieldRun(context));
        }

        private IEnumerable<ParseResult> YieldRun(RunningContext context)
        {
            foreach (var command in Commands)
            {
                yield return command.Exec(context);
            }
        }

        public static ParseResult AggregateParseResult(IEnumerable<ParseResult> results)
        {
            List<ParseResult> warningResults = new List<ParseResult>();
            foreach(var result in results)
            {
                if (result.ResultLevel == ResultLevel.Error)
                {
                    return result;
                }
                else if (result.ResultLevel == ResultLevel.Warning)
                {
                    warningResults.Add(result);
                }
            }

            if (warningResults.Count > 0)
            {
                return new ParseResult(ResultLevel.Warning, warningResults.Select(s => $"Warning in build phase {s.Phase}: {s.Message}").ToDelimitedString("\n"));
            }

            return ParseResult.SuccessResult;
        }
    }

    public class RunningContext
    {
    }
}
