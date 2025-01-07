// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

using Docfx.Common;

namespace Docfx.Build.OverwriteDocuments;

public static partial class OverwriteUtility
{
    private static readonly string[] UidWrappers = ["`", "``", "```", "````", "`````", "``````"];

    [GeneratedRegex(@"^(?<propertyName>[:A-Za-z_](?>[\w\.\-:]*))(?:\[\s*(?<key>[:A-Za-z_](?>[\w\.\-:]*))\s*=\s*(?:""(?<value>(?:(?>[^""\\]*)|\\.)*)"")\s*\])?(?:\/|$)")]
    private static partial Regex OPathRegex();

    public static List<OPathSegment> ParseOPath(string OPathString)
    {
        if (string.IsNullOrEmpty(OPathString))
        {
            throw new ArgumentException("OPathString cannot be null or empty.", nameof(OPathString));
        }

        if (OPathString.EndsWith('/'))
        {
            throw new ArgumentException($"{OPathString} is not a valid OPath");
        }

        var OPathSegments = new List<OPathSegment>();

        var leftString = OPathString;
        while (leftString.Length > 0)
        {
            var match = OPathRegex().Match(leftString);
            if (match.Length == 0)
            {
                throw new ArgumentException($"{OPathString} is not a valid OPath");
            }

            if (!match.Value.EndsWith('/') && match.Groups["key"].Success)
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
        if (!fragments.TryGetValue(uid, out var value))
        {
            value = new MarkdownFragment
            {
                Uid = uid,
                Properties = [],
                Metadata = metadata
            };
            fragments.Add(uid, value);
        }

        value.Metadata = MergeMetadata(value.Metadata, metadata);
        value.Touched = true;
    }

    public static void AddOrUpdateFragmentProperty(this MarkdownFragment fragment, string oPath, string content = null, Dictionary<string, object> metadata = null)
    {
        if (!fragment.Properties.TryGetValue(oPath, out var property))
        {
            fragment.Properties[oPath] = property = new MarkdownProperty { OPath = oPath };
        }

        if (string.IsNullOrEmpty(property.Content))
        {
            property.Content = string.IsNullOrWhiteSpace(content) ? string.Empty : content.Trim('\n', '\r');
        }

        property.Touched = true;
        fragment.Metadata = MergeMetadata(fragment.Metadata, metadata);
    }

    public static MarkdownFragment ToMarkdownFragment(this MarkdownFragmentModel model, string originalContent = null)
    {
        Dictionary<string, object> metadata = null;
        if (!string.IsNullOrEmpty(model.YamlCodeBlock))
        {
            using TextReader sr = new StringReader(model.YamlCodeBlock);
            metadata = YamlUtility.Deserialize<Dictionary<string, object>>(sr);
        }

        return new MarkdownFragment
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
        return new MarkdownProperty
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
                left.TryAdd(pair.Key, pair.Value);
            }
        }
        return left;
    }
}
