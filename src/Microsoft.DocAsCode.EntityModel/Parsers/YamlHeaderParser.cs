namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Match yaml header from markdown files.
    /// YAML HEADER Syntax
    /// 1. Started with a new line
    /// 2. Followed by three dashes `---` as a line, spaces are allowed before 
    /// 3. Followed by and must followed by `uid: `
    /// 4. Followed by other properties in YAML format
    /// 5. Ended with three dashes `---` as a line, spaces are allowed before 
    /// </summary>
    public static class YamlHeaderParser
    {
        private static readonly List<string> RequiredProperties = new List<string> { "uid" };

        // If is not the end of the file, then \n should be appended to ---
        public static readonly Regex YamlHeaderRegex = new Regex(@"((?!\n)\s)*\n((?!\n)\s)*\-\-\-((?!\n)\s)*\n((?!\n)\s)*(?<content>uid:.*?)\s*\-\-\-((?!\n)\s)*\n", RegexOptions.Compiled | RegexOptions.Singleline);

        public static IList<MatchDetail> Select(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;
            var yamlHeader = YamlHeaderRegex.Matches(input);
            if (yamlHeader.Count == 0) return null;

            var details = new MatchDetailCollection();
            var singles = (from Match item in yamlHeader select SelectSingle(item, input));
            details.Merge(singles);
            return details.Values.ToList();
        }

        private static MatchSingleDetail SelectSingle(Match match, string input)
        {
            var wholeMatch = match.Groups[0];

            string content = match.Groups["content"].Value;
            Dictionary<string, object> properties;
            string message;
            if (!TryExtractProperties(content, RequiredProperties, out properties, out message))
            {
                ParseResult.WriteToConsole(ResultLevel.Warning, message);
                return null;
            }

            var overridenProperties = RemoveRequiredProperties(properties, RequiredProperties);

            // Get one character larger then the actual match
            var location = Location.GetLocation(input, wholeMatch.Index - 1, wholeMatch.Length + 2);

            return new MatchSingleDetail
                       {
                           Id = properties["uid"].ToString(),
                           MatchedSection =
                               new Section { Key = wholeMatch.Value, Locations = new List<Location> { location } },
                           Properties = overridenProperties,
                       };
        }

        private static Dictionary<string, object> RemoveRequiredProperties(Dictionary<string, object> properties, IEnumerable<string> requiredProperties)
        {
            if (properties == null) return null;

            var overridenProperties = new Dictionary<string, object>(properties);
            foreach (var requiredProperty in RequiredProperties)
            {
                if (requiredProperty != null) overridenProperties.Remove(requiredProperty);
            }

            return overridenProperties;
        }

        /// <summary>
        /// Extract YAML format content from yaml header
        /// </summary>
        /// <param name="content">the whole matched yaml header</param>
        /// <param name="requiredProperties">The properties that should be specified</param>
        /// <param name="properties"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private static bool TryExtractProperties(string content, IEnumerable<string> requiredProperties, out Dictionary<string, object> properties, out string message)
        {
            properties = new Dictionary<string, object>();
            message = string.Empty;
            if (string.IsNullOrEmpty(content)) return false;
            try
            {
                using (StringReader reader = new StringReader(content))
                {
                    properties = YamlUtility.Deserialize<Dictionary<string, object>>(reader);
                    string checkPropertyMessage;
                    bool checkPropertyStatus = CheckRequiredProperties(properties, requiredProperties, out checkPropertyMessage);
                    if (!checkPropertyStatus)
                    {
                        throw new InvalidDataException(checkPropertyMessage);
                    }

                    if (!string.IsNullOrEmpty(checkPropertyMessage))
                    {
                        message += checkPropertyMessage;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                // Yaml header could be very long.. substring it
                content = content?.Split('\n').FirstOrDefault()?.Trim();
                message += string.Format(@"yaml header '{0}' is not in a valid YAML format: {1}.", content, e.Message);
                return false;
            }
        }

        private static bool CheckRequiredProperties(Dictionary<string, object> properties, IEnumerable<string> requiredKeys, out string message)
        {
            Dictionary<string, bool> requiredKeyExistence = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var requiredKey in requiredKeys)
            {
                bool current;
                if (!requiredKeyExistence.TryGetValue(requiredKey, out current))
                {
                    requiredKeyExistence.Add(requiredKey, false);
                }
            }

            foreach (var property in properties)
            {
                if (requiredKeyExistence.ContainsKey(property.Key))
                {
                    requiredKeyExistence[property.Key] = true;
                }
            }

            var notExistsKeys = requiredKeyExistence.Where(s => !s.Value);
            if (notExistsKeys.Any())
            {
                message = string.Format("Required properties {{{{{0}}}}} are not set. Note that keys are case insensitive.", string.Join(",", notExistsKeys.Select(s => s.Key)));
                return false;
            }

            message = string.Empty;
            return true;
        }
    }
}
