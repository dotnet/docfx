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
        internal static void Normalize(string outputPath, bool basicNormalize = false)
        {
            var sw = Stopwatch.StartNew();

            // remove docfx.yml to ignore the diff caused by xref url for now
            // the logic can be removed while docfx.yml not generated anymore
            foreach (var configPath in Directory.GetFiles(outputPath, "docfx.yml", SearchOption.AllDirectories))
            {
                File.Delete(configPath);
            }

            Parallel.ForEach(Directory.GetFiles(outputPath, "*.*", SearchOption.AllDirectories), (path) => PrettifyFile(path, basicNormalize));
            Console.WriteLine($"Normalizing done in {sw.Elapsed.TotalSeconds}s");
        }

        private static void PrettifyFile(string path, bool basicNormalize)
        {
            switch (Path.GetExtension(path).ToLowerInvariant())
            {
                case ".json":
                    File.WriteAllText(path, NormalizeJsonFile(path));
                    break;

                case ".log":
                case ".txt":
                    if (basicNormalize)
                    {
                        File.WriteAllLines(path, File.ReadAllLines(path).OrderBy(line => line));
                    }
                    else
                    {
                        File.WriteAllLines(path, File.ReadAllLines(path).OrderBy(line => line).Select((line) => NormalizeJsonLog(line)));
                    }
                    break;
            }
        }

        private static string NormalizeJsonFile(string path) => NormalizeNewLine(NormalizeJson(File.ReadAllText(path)));

        private static string NormalizeJson(string json) => JToken.Parse(json).ToString();

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
