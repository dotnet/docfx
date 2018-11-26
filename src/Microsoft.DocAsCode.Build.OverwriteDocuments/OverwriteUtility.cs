// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.OverwriteDocuments
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Common;

    public static class OverwriteUtility
    {
        private static readonly string[] UidWrappers = { "`", "``", "```", "````", "`````", "``````" };

        private static readonly Regex OPathRegex =
            new Regex(
                @"^(?<propertyName>[:A-Za-z_](?>[\w\.\-:]*))(?:\[\s*(?<key>[:A-Za-z_](?>[\w\.\-:]*))\s*=\s*(?:""(?<value>(?:(?>[^""\\]*)|\\.)*)"")\s*\])?(?:\/|$)",
                RegexOptions.Compiled);

        public static List<OPathSegment> ParseOPath(string OPathString)
        {
            if (string.IsNullOrEmpty(OPathString))
            {
                throw new ArgumentException("OPathString cannot be null or empty.", nameof(OPathString));
            }

            if (OPathString.EndsWith("/"))
            {
                throw new ArgumentException($"{OPathString} is not a valid OPath");
            }

            var OPathSegments = new List<OPathSegment>();

            var leftString = OPathString;
            while (leftString.Length > 0)
            {
                var match = OPathRegex.Match(leftString);
                if (match.Length == 0)
                {
                    throw new ArgumentException($"{OPathString} is not a valid OPath");
                }

                if (!match.Value.EndsWith("/") && match.Groups["key"].Success)
                {
                    throw new ArgumentException($"{OPathString} is not a valid OPath");
                }

                OPathSegments.Add(new OPathSegment
                {
                    SegmentName = match.Groups["propertyName"].Value,
                    Key = match.Groups["key"].Value,
                    Value = match.Groups["value"].Value,
                    OriginalSegmentString = match.Value.TrimEnd('/')
                });
                leftString = leftString.Substring(match.Length);
            }
            return OPathSegments;
        }

        public static string GetUidWrapper(string uid)
        {
            int wrapperCount = 0;
            int lastPos = 0;
            for (int i = 0; i < uid.Length; i++)
            {
                if (uid[i] == '`')
                {
                    wrapperCount = System.Math.Max(wrapperCount, i - lastPos);
                }
                else
                {
                    lastPos = i;
                }
            }
            return UidWrappers[wrapperCount];
        }

        public static void AddOrUpdateFragmentEntity(this Dictionary<string, MarkdownFragment> fragments, string uid, Dictionary<string, object> metadata = null)
        {
            if (!fragments.ContainsKey(uid))
            {
                fragments.Add(uid, new MarkdownFragment()
                {
                    Uid = uid,
                    Properties = new Dictionary<string, MarkdownProperty>(),
                    Metadata = metadata
                });
            }
            fragments[uid].Metadata = MergeMetadata(fragments[uid].Metadata, metadata);
            fragments[uid].Touched = true;
        }

        public static void AddOrUpdateFragmentProperty(this MarkdownFragment fragment, string oPath, string content = null, Dictionary<string, object> metadata = null)
        {
            if (!fragment.Properties.ContainsKey(oPath))
            {
                fragment.Properties[oPath] = new MarkdownProperty()
                {
                    OPath = oPath
                };
            }
            if (string.IsNullOrEmpty(fragment.Properties[oPath].Content))
            {
                fragment.Properties[oPath].Content = string.IsNullOrWhiteSpace(content) ? string.Empty : content.Trim('\n', '\r');
            }
            fragment.Properties[oPath].Touched = true;
            fragment.Metadata = MergeMetadata(fragment.Metadata, metadata);
        }

        public static MarkdownFragment ToMarkdownFragment(this MarkdownFragmentModel model, string originalContent = null)
        {
            Dictionary<string, object> metadata = null;
            if (!string.IsNullOrEmpty(model.YamlCodeBlock))
            {
                using (TextReader sr = new StringReader(model.YamlCodeBlock))
                {
                    metadata = YamlUtility.Deserialize<Dictionary<string, object>>(sr);
                }
            }

            return new MarkdownFragment()
            {
                Uid = model.Uid,
                Metadata = metadata,
                Properties = model.Contents
                    ?.Select(prop => prop.ToMarkdownProperty(originalContent))
                    .GroupBy(m => m.OPath)
                    .ToDictionary(g => g.Key, g => g.First())
            };
        }

        public static MarkdownProperty ToMarkdownProperty(this MarkdownPropertyModel model, string originalContent = null)
        {
            var content = string.Empty;
            if (model.PropertyValue?.Count > 0 && originalContent != null)
            {
                var start = model.PropertyValue.First().Span.Start;
                var length = model.PropertyValue.Last().Span.End - start + 1;
                var piece = originalContent.Substring(start, length);
                if (!string.IsNullOrWhiteSpace(piece))
                {
                    content = piece;
                }
            }
            return new MarkdownProperty()
            {
                OPath = model.PropertyName,
                Content = content
            };
        }

        private static Dictionary<string, object> MergeMetadata(Dictionary<string, object> left, Dictionary<string, object> right)
        {
            if (left == null)
            {
                return right;
            }
            if (right?.Count > 0)
            {
                foreach (var pair in right)
                {
                    if (!left.ContainsKey(pair.Key))
                    {
                        left[pair.Key] = right[pair.Key];
                    }
                }
            }
            return left;
        }
    }
}
