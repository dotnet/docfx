// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DocAsTest
{
    public static class JsonDiffExtensions
    {
        public static JsonDiffBuilder UseIgnoreNull(this JsonDiffBuilder builder, JsonDiffPredicate predicate)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder.Use(predicate, (expected, actual, name, diff) =>
                expected.Type == JTokenType.Null ? (expected, expected) : (expected, actual));
        }

        public static JsonDiffBuilder UseNegate(this JsonDiffBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder.Use((expected, actual, name, diff) =>
            {
                if (expected.Type == JTokenType.String && actual.Type == JTokenType.String &&
                    expected.Value<string>() is string str && str.StartsWith("!"))
                {
                    if (str.Substring(1) != actual.Value<string>())
                    {
                        return (actual, actual);
                    }
                }
                return (expected, actual);
            });
        }

        public static JsonDiffBuilder UseRegex(this JsonDiffBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder.Use((expected, actual, name, diff) =>
            {
                if (expected.Type == JTokenType.String && actual.Type == JTokenType.String &&
                    expected.Value<string>() is string str &&
                    str.Length > 2 && str.StartsWith("/") && str.EndsWith("/"))
                {
                    var regex = str.Substring(1, str.Length - 2);
                    if (Regex.IsMatch(actual.Value<string>(), regex))
                    {
                        return (actual, actual);
                    }
                }
                return (expected, actual);
            });
        }

        public static JsonDiffBuilder UseWildcard(this JsonDiffBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder.Use((expected, actual, name, diff) =>
            {
                if (expected.Type == JTokenType.String && actual.Type == JTokenType.String &&
                    expected.Value<string>() is string str && str.Contains('*'))
                {
                    if (Regex.IsMatch(actual.ToString(), $"^{Regex.Escape(str).Replace("\\*", ".*")}$"))
                    {
                        return (actual, actual);
                    }
                }
                return (expected, actual);
            });
        }

        public static JsonDiffBuilder UseJson(this JsonDiffBuilder builder, JsonDiffOptions options = default)
            => UseJson(builder, IsFile(".json"), options);

        public static JsonDiffBuilder UseJson(this JsonDiffBuilder builder, JsonDiffPredicate predicate, JsonDiffOptions options = default)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder.Use(predicate, (expected, actual, name, diff) =>
            {
                if (expected is JValue ev && ev.Value is string expectedText &&
                    actual is JValue av && av.Value is string actualText)
                {
                    var (expectedNorm, actualNorm) = diff.Normalize(
                        JToken.Parse(expectedText),
                        JToken.Parse(actualText),
                        options);

                    return (expectedNorm.ToString(Formatting.Indented), actualNorm.ToString(Formatting.Indented));
                }

                return (expected, actual);
            });
        }

        public static JsonDiffBuilder UseHtml(this JsonDiffBuilder builder)
            => UseHtml(builder, IsFile(".html", ".htm"));

        public static JsonDiffBuilder UseHtml(this JsonDiffBuilder builder, JsonDiffPredicate predicate)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder.Use(predicate, (expected, actual, name, diff) =>
            {
                if (expected is JValue ev && ev.Value is string expectedText &&
                    actual is JValue av && av.Value is string actualText)
                {
                    return (JsonDiff.NormalizeHtml(expectedText), JsonDiff.NormalizeHtml(actualText));
                }

                return (expected, actual);
            });
        }

        private static JsonDiffPredicate IsFile(params string[] fileExtensions)
        {
            return (expected, actual, name) =>
                fileExtensions.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase);
        }
    }
}
