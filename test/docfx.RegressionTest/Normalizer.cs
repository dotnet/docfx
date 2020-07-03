// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class Normalizer
    {
        /// <summary>
        /// Normalize output directory.
        /// To eliminate new diff during new feature integration:
        /// - Add your temporary normalization logic inside <see cref="NormalizeJsonFile(string)"/>,
        /// - Set NormalizeJsonFiles flat on call-site when normalizing baseline folder
        /// also remember to rever them if they will disappear after baseline refreshment.
        /// </summary>
        internal static void Normalize(string outputPath, NormalizeStage normalizeStage)
        {
            var sw = Stopwatch.StartNew();

            // remove docfx.yml to ignore the diff caused by xref url for now
            // the logic can be removed while docfx.yml not generated anymore
            foreach (var configPath in Directory.GetFiles(outputPath, "docfx.yml", SearchOption.AllDirectories))
            {
                File.Delete(configPath);
            }

            Parallel.ForEach(Directory.GetFiles(outputPath, "*.*", SearchOption.AllDirectories), (path) => NormalizeFile(path, normalizeStage));
            Console.WriteLine($"Normalizing done in {sw.Elapsed.TotalSeconds}s");
        }

        private static void NormalizeFile(string path, NormalizeStage normalizeStage)
        {
            switch (Path.GetExtension(path).ToLowerInvariant())
            {
                case ".json":
                    if (normalizeStage.HasFlag(NormalizeStage.NormalizeJsonFiles))
                    {
                        File.WriteAllText(path, NormalizeJsonFile(path));
                    }
                    break;

                case ".log":
                case ".txt":
                    if (normalizeStage.HasFlag(NormalizeStage.PrettifyLogFiles))
                    {
                        File.WriteAllLines(path, File.ReadAllLines(path).Select(line => Regex.Replace(line, ",\"date_time\":.*?Z\"", "")).OrderBy(line => line));
                    }
                    else if (normalizeStage.HasFlag(NormalizeStage.NormalizeLogFiles))
                    {
                        File.WriteAllLines(path, File.ReadAllLines(path).OrderBy(line => line).Select((line) => NormalizeJsonLog(line)));
                    }
                    break;

                case ".html":
                case ".yml":
                    break;

                default:
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    break;
            }
        }

        private static string NormalizeJsonFile(string path)
        {
            // Apply your normalize logic to eliminate new diff
            var obj = JToken.Parse(File.ReadAllText(path));
            return NormalizeNewLine(obj.ToString());
        }

        private static string NormalizeNewLine(string text) => text.Replace("\r", "").Replace("\\n\\n", "⬇\n").Replace("\\n", "⬇\n");

        private static string NormalizeJsonLog(string json)
        {
            var obj = JObject.Parse(json);
            obj.Remove("date_time");

            if (obj.ContainsKey("code")
            && obj["code"]!.Value<string>() == "yaml-syntax-error"
            && obj.ContainsKey("message"))
            {
                obj["message"] = JValue.CreateString(Regex.Replace(obj["message"]!.Value<string>(), @"Idx: \d+", ""));
            }

            return NormalizeNewLine(obj.ToString());
        }
    }
}
