// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.EntityModel.ViewModels;

    public static class LinkParser
    {
        const string idSelector = @"(?![0-9])[\w_])+[\w\(\)\.\{\}\[\]\|\*\^~#@!`,_<>:]*";
        public static Regex CommentIdRegex = new Regex(@"^(?<type>N|T|M|P|F|E):(?<id>(" + idSelector + ")$", RegexOptions.Compiled);

        // self written link should be ended with a whitespace
        public static Regex LinkFromSelfWrittenRegex = new Regex(@"@(?<quote>[""']?)(?<content>(" + idSelector + @")\k<quote>", RegexOptions.Compiled);

        public static string ResolveToMarkdownLink(Func<string, ApiIndexItemModel> finder, string input, Func<ApiIndexItemModel, string> hrefGetter)
        {
            return ResolveText(finder, input, s =>
                 {
                     string href;
                     if (hrefGetter == null) return s.Name;
                     else href = hrefGetter(s);
                     return string.Format("[{0}]({1})", s.Name, href);
                 }, s => string.Format("[{0}]()", s)
                );
        }

        public static string ResolveText<T>(Func<string, T> finder, string input, Func<T, string> linkGenerator, Func<string, string> failureHandler = null)
        {
            if (finder == null) return input;
            return Resolve(input, s =>
            {
                T item = finder(s);
                if (item != null)
                {
                    if (linkGenerator != null)
                    {
                        return linkGenerator(item);
                    }
                    else
                    {
                        Debug.Assert(linkGenerator == null);
                        return item.ToString();
                    }
                }
                else
                {
                    if (failureHandler != null)
                    {
                        return failureHandler(s);
                    }

                    return null;
                }
            });
        }

        public static string Resolve(string input, Func<string, string> replaceHandler)
        {
            if (string.IsNullOrEmpty(input)) return null;
        
            input = LinkFromSelfWrittenRegex.Replace(input, new MatchEvaluator(s => LinkResolver(s, replaceHandler)));
            return input;
        }

        public static IList<MatchDetail> Select(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;
            var linkFromCref = LinkFromSelfWrittenRegex.Matches(input);
            var details = Merge(linkFromCref, input, null);
            return details.Values.ToList();
        }

        private static MatchDetailCollection Merge(MatchCollection collection, string input, MatchDetailCollection details)
        {
            if (details == null)
            {
                details = new MatchDetailCollection();
            }

            var singles = (from Match item in collection select SelectSingle(item, input));

            details.Merge(singles);

            return details;
        }

        private static MatchSingleDetail SelectSingle(Match match, string input)
        {
            var wholeMatch = match.Groups[0];
            string id = match.Groups["content"].Value;

            // For a valid commentid, remove the first 2 characters
            if (CommentIdRegex.IsMatch(id))
            {
                id = id.Substring(2);
            }

            var location = Location.GetLocation(input, wholeMatch.Index, wholeMatch.Length);
            return new MatchSingleDetail
                       {
                           Id = id,
                           MatchedSection =
                               new Section { Key = wholeMatch.Value, Locations = new List<Location> { location } }
                       };

        }

        /// <summary>
        /// TODO: Change according to the spec
        /// 0. \@ to escape @
        /// 1. support simple @abc
        /// 2. support @'abc' where abc should not be ''
        /// 3. support @"abc" where abc can be any chars
        /// </summary>
        /// <param name="match"></param>
        /// <param name="replaceHandler"></param>
        /// <returns></returns>
        private static string LinkResolver(Match match, Func<string, string> replaceHandler)
        {
            string id = match.Groups["content"].Value;
            // For a valid commentid, remove the first 2 characters
            if (CommentIdRegex.IsMatch(id))
            {
                id = id.Substring(2);

                string replacement = replaceHandler(id);
                if (!string.IsNullOrEmpty(replacement))
                {
                    return replacement;
                }
            }
            else
            {
                string replacement = replaceHandler(id);
                if (!string.IsNullOrEmpty(replacement))
                {
                    return replacement;
                }
            }

            return match.Value;
        }
    }
}
