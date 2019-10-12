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
        /// <summary>
        /// Ignore the actual result of a property if the expected value is null
        /// </summary>
        /// <example>
        /// Given the expectation { "a": null }, { "a": "anything" } pass.
        /// </example>
        public static JsonDiffBuilder UseIgnoreNull(this JsonDiffBuilder builder, JsonDiffPredicate predicate = null)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            return builder.Use(predicate, (expected, actual, name, diff) =>
                expected.Type == JTokenType.Null && actual.Type != JTokenType.Undefined ? (expected, expected) : (expected, actual));
        }

        /// <summary>
        /// Assert the actual result must not be the expected result if the expected value starts with !
        /// </summary>
        /// <example>
        /// Given the expectation "!value", "a value" pass but "value" fail
        /// </example>
        public static JsonDiffBuilder UseNegate(this JsonDiffBuilder builder, JsonDiffPredicate predicate = null)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            return builder.Use(predicate, (expected, actual, name, diff) =>
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

        /// <summary>
        /// Assert the actual value must match a regex if the expectation looks like /{regex}/
        /// </summary>
        /// <example>
        /// Given the expectation "/^a*$/", "a" pass but "b" fail.
        /// </example>
        public static JsonDiffBuilder UseRegex(this JsonDiffBuilder builder, JsonDiffPredicate predicate = null)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            return builder.Use(predicate, (expected, actual, name, diff) =>
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

        /// <summary>
        /// Assert the actual value must match a wildcard if the expectation contains *
        /// </summary>
        /// <example>
        /// Given the expectation "a*", "aa" pass but "bb" fail.
        /// </example>
        public static JsonDiffBuilder UseWildcard(this JsonDiffBuilder builder, JsonDiffPredicate predicate = null)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            return builder.Use(predicate, (expected, actual, name, diff) =>
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

        /// <summary>
        /// Ignore additonal properties in actual value that is missing in expected value.
        /// </summary>
        /// <example>
        /// Given the expectation { "a": 1 }, { "b": 1 } pass.
        /// </example>
        public static JsonDiffBuilder UseAdditionalProperties(
            this JsonDiffBuilder builder, JsonDiffPredicate predicate = null, Func<string, bool> isRequiredProperty = null)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            return builder.Use(predicate, (expected, actual, name, diff) =>
            {
                if (expected is JObject expectedObj && actual is JObject actualObj)
                {
                    var newActual = new JObject(actualObj.Properties().Where(IsRequiredProperty));

                    return (expected, newActual);
                }

                return (expected, actual);

                bool IsRequiredProperty(JProperty property)
                {
                    if (expectedObj.ContainsKey(property.Name))
                    {
                        return true;
                    }
                    if (isRequiredProperty != null && isRequiredProperty(property.Name))
                    {
                        return true;
                    }
                    return false;
                }
            });
        }

        /// <summary>
        /// Assert the actual value must be a JSON string that matches the expected JSON string.
        /// </summary>
        /// <example>
        /// Given the expectation "{ \"a\": 1 }", ""{ \"a\": 1 }"" pass but "{ \"a\": 2 }" fail.
        /// </example>
        public static JsonDiffBuilder UseJson(this JsonDiffBuilder builder, JsonDiffPredicate predicate = null, JsonDiff jsonDiff = null)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            return builder.Use(predicate ?? IsFile(".json"), (expected, actual, name, diff) =>
            {
                if (expected is JValue ev && ev.Value is string expectedText &&
                    actual is JValue av && av.Value is string actualText)
                {
                    var (expectedNorm, actualNorm) = (jsonDiff ?? diff).Normalize(
                        JToken.Parse(expectedText),
                        JToken.Parse(actualText));

                    return (expectedNorm.ToString(Formatting.Indented), actualNorm.ToString(Formatting.Indented));
                }

                return (expected, actual);
            });
        }

        /// <summary>
        /// Assert the actual value must be an HTML string that matches the expected HTML string.
        /// </summary>
        /// <example>
        /// Given the expectation "<div>text</div>", "<div> text </div>" pass but "<div>text 2</div>" fail.
        /// </example>
        public static JsonDiffBuilder UseHtml(this JsonDiffBuilder builder, JsonDiffPredicate predicate = null)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            return builder.Use(predicate ?? IsFile(".html", ".htm"), (expected, actual, name, diff) =>
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
