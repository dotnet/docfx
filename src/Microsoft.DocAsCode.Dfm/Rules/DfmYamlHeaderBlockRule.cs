// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.MarkdownLite.Matchers;
    using YamlDotNet.Core;

    public class DfmYamlHeaderBlockRule : IMarkdownRule
    {
        private static readonly Matcher _EndSymbol =
            Matcher.Char('-').Repeat(3, 3) + Matcher.WhiteSpacesOrEmpty +
            (Matcher.NewLine.RepeatAtLeast(1) | Matcher.EndOfString);
        private static readonly Matcher _YamlHeaderMatcher =
            Matcher.Char('-').Repeat(3, 3) + Matcher.WhiteSpacesOrEmpty + Matcher.NewLine +
            (
                Matcher.AnyStringInSingleLine |
                (Matcher.NewLine.RepeatAtLeast(1) + _EndSymbol.ToNegativeTest())
            ).RepeatAtLeast(1).ToGroup("yaml") +
            Matcher.NewLine.RepeatAtLeast(1) + _EndSymbol;

        public static readonly Regex YamlHeaderRegex = new Regex(@"^\-{3}(?:\s*?)\n([\s\S]+?)(?:\s*?)\n\-{3}(?:\s*?)(?:\n|$)", RegexOptions.Compiled | RegexOptions.Singleline, TimeSpan.FromSeconds(10));

        public string Name => "DfmYamlHeader";

        [Obsolete("Please use YamlHeaderMatcher.")]
        public virtual Regex YamlHeader => YamlHeaderRegex;

        public virtual Matcher YamlHeaderMatcher => _YamlHeaderMatcher;

        public IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (YamlHeader != YamlHeaderRegex || parser.Options.LegacyMode)
            {
                return TryMatchOld(parser, context);
            }
            var match = context.Match(YamlHeaderMatcher);
            if (match?.Length > 0)
            {
                // ---
                // a: b
                // ---
                var value = match["yaml"].GetValue();
                try
                {
                    using (StringReader reader = new StringReader(value))
                    {
                        var result = YamlUtility.Deserialize<Dictionary<string, object>>(reader);
                        if (result == null)
                        {
                            return null;
                        }
                    }
                }
                catch (YamlException invalidYamlHeaderException)
                {
                    LogMessage(invalidYamlHeaderException, context);
                    return null;
                }
                catch (Exception)
                {
                    LogMessage(context);
                    return null;
                }
                var sourceInfo = context.Consume(match.Length);
                return new DfmYamlHeaderBlockToken(this, parser.Context, value, sourceInfo);
            }
            return null;
        }

        private IMarkdownToken TryMatchOld(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = YamlHeader.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }

            // ---
            // a: b
            // ---
            var value = match.Groups[1].Value;
            try
            {
                using (StringReader reader = new StringReader(value))
                {
                    var result = YamlUtility.Deserialize<Dictionary<string, object>>(reader);
                    if (result == null)
                    {
                        return null;
                    }
                }
            }
            catch (YamlException invalidYamlHeaderException)
            {
                LogMessage(invalidYamlHeaderException, context);
                return null;
            }
            catch (Exception)
            {
                LogMessage(context);
                return null;
            }
            var sourceInfo = context.Consume(match.Length);
            return new DfmYamlHeaderBlockToken(this, parser.Context, value, sourceInfo);
        }

        private static void LogMessage(YamlException exception, IMarkdownParsingContext context)
        {
            Logger.Log(
                (context.LineNumber == 1 ? LogLevel.Warning : LogLevel.Info),
                $"Invalid yaml header: {exception.Message}",
                file: context.File,
                line: context.LineNumber.ToString(),
                code: WarningCodes.Markdown.InvalidYamlHeader);
        }

        private static void LogMessage(IMarkdownParsingContext context)
        {
            Logger.Log(
                (context.LineNumber == 1 ? LogLevel.Warning : LogLevel.Info),
                "Invalid yaml header.",
                file: context.File,
                line: context.LineNumber.ToString(),
                code: WarningCodes.Markdown.InvalidYamlHeader);
        }
    }
}
