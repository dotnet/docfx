// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class Config
    {
        private static readonly string[] s_defaultContentInclude = new[] { "docs/**/*.{md,yml,json}" };
        private static readonly string[] s_defaultContentExclude = Array.Empty<string>();

        /// <summary>
        /// Gets the default locale of this docset.
        /// </summary>
        public readonly string Locale = "en-us";

        /// <summary>
        /// Gets the contents that are managed by this docset.
        /// </summary>
        public readonly FileConfig Content = new FileConfig(s_defaultContentInclude, s_defaultContentExclude);

        /// <summary>
        /// Gets the output config.
        /// </summary>
        public readonly OutputConfig Output = new OutputConfig();

        /// <summary>
        /// Gets the global metadata added to each document.
        /// </summary>
        public readonly JObject Metadata = new JObject();

        /// <summary>
        /// Gets the map from dependency name to git url
        /// All dependencies need to be restored locally before build
        /// The default value is empty mappings
        /// </summary>
        public readonly Dictionary<string, string> Dependencies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Load the config under <paramref name="docsetPath"/>
        /// </summary>
        public static Config Load(string docsetPath, CommandLineOptions options)
        {
            ValidateDocsetPath(docsetPath);
            var configPath = Path.Combine(docsetPath, "docfx.yml");
            if (!File.Exists(configPath))
            {
                // TODO: error handling
                throw new Exception($"No config file found under '{docsetPath}'");
            }
            return LoadCore(configPath, options);
        }

        /// <summary>
        /// Load the config if it exists under <paramref name="docsetPath"/>
        /// </summary>
        /// <returns>Whether config exists under <paramref name="docsetPath"/></returns>
        public static bool LoadIfExists(string docsetPath, CommandLineOptions options, out Config config)
        {
            ValidateDocsetPath(docsetPath);
            var configPath = Path.Combine(docsetPath, "docfx.yml");
            var exists = File.Exists(configPath);
            config = exists ? LoadCore(configPath, options) : new Config();
            return exists;
        }

        private static void ValidateDocsetPath(string docsetPath)
        {
            if (PathUtility.FolderPathHasInvalidChars(docsetPath))
            {
                // TODO: error handling
                throw new Exception("docset path contains invalid characters.");
            }
        }

        private static Config LoadCore(string configPath, CommandLineOptions options)
        {
            // Options should be converted to config and overwrite the config parsed from docfx.yml
            try
            {
                var configObject = Expand(YamlUtility.Deserialize<JObject>(File.ReadAllText(configPath)) ?? new JObject());
                return configObject.ToObject<Config>(JsonUtility.DefaultDeserializer);
            }
            catch (Exception e)
            {
                // TODO: error handling
                throw new Exception($"Error parsing docset config {configPath}: {e.Message}", e);
            }
        }

        private static JObject Expand(JObject config)
        {
            config[ConfigConstants.Content] = ExpandFiles(config[ConfigConstants.Content]);
            return config;
        }

        private static JObject ExpandFiles(JToken file)
        {
            if (file == null)
                return null;
            if (file is JValue str)
                file = new JArray(str);
            if (file is JArray arr)
                file = new JObject { [ConfigConstants.Include] = arr };
            return ExpandIncludeExclude((JObject)file);
        }

        private static JObject ExpandIncludeExclude(JObject item)
        {
            Debug.Assert(item != null);
            item[ConfigConstants.Include] = ExpandStringArray(item[ConfigConstants.Include]);
            item[ConfigConstants.Exclude] = ExpandStringArray(item[ConfigConstants.Exclude]);
            return item;
        }

        private static JArray ExpandStringArray(JToken e)
        {
            if (e == null)
                return null;
            if (e is JValue str)
                return new JArray(e);
            if (e is JArray arr)
                return arr;

            // TODO: error handle
            return null;
        }
    }
}
