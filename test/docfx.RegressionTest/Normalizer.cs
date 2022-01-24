// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal static class Normalizer
{
    /// <summary>
    /// Normalize output directory.
    /// To eliminate new diff during new feature integration:
    /// - Add your temporary normalization logic inside <see cref="NormalizeJsonFile(string)"/>,
    /// - Set NormalizeJsonFiles flag on call-site when normalizing baseline folder
    /// also remember to rever them if they will disappear after baseline refreshment.
    /// </summary>
    internal static void Normalize(string outputPath, NormalizeStage normalizeStage, ErrorLevel errorLevel = ErrorLevel.Info)
    {
        var sw = Stopwatch.StartNew();

        // remove docfx.yml to ignore the diff caused by xref url for now
        // the logic can be removed while docfx.yml not generated anymore
        foreach (var configPath in Directory.GetFiles(outputPath, "docfx.yml", SearchOption.AllDirectories))
        {
            File.Delete(configPath);
        }

        Parallel.ForEach(Directory.GetFiles(outputPath, "*.*", SearchOption.AllDirectories), (path) => NormalizeFile(path, normalizeStage, errorLevel));
        Console.WriteLine($"Normalizing done in {sw.Elapsed.TotalSeconds}s");
    }

    private static void NormalizeFile(string path, NormalizeStage normalizeStage, ErrorLevel errorLevel)
    {
        switch (Path.GetExtension(path).ToLowerInvariant())
        {
            case ".json":
                if (normalizeStage.HasFlag(NormalizeStage.PrettifyJsonFiles))
                {
                    File.WriteAllText(path, NormalizeNewLine(JToken.Parse(File.ReadAllText(path)).ToString()));
                }
                else if (normalizeStage.HasFlag(NormalizeStage.NormalizeJsonFiles))
                {
                    NormalizeJsonFile(path);
                }
                break;

            case ".log":
            case ".txt":
                if (normalizeStage.HasFlag(NormalizeStage.PrettifyLogFiles))
                {
                    PrettifyJsonLog(path, errorLevel);
                }
                break;

            case ".html":
            case ".yml":
                break;

            default:
                File.Delete(path);
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

    private static void PrettifyJsonLog(string logPath, ErrorLevel errorLevel)
    {
        var logs = File.ReadAllLines(logPath).OrderBy(line => line, StringComparer.Ordinal);
        using var sw = new StreamWriter(File.Create(logPath));

        foreach (var log in logs)
        {
            var obj = JObject.Parse(log);
            obj.Remove("date_time");
            if (Enum.TryParse((string)obj["message_severity"]!, true, out ErrorLevel level)
                && level < errorLevel)
            {
                continue;
            }

            var code = obj.Value<string>("code");

            switch (code)
            {
                case "exceed-max-file-errors":
                    throw new Exception("Exceed max file errors, adjust regression test config to allow more error items.");

                case "yaml-syntax-error" when obj.ContainsKey("message"):
                    obj["message"] = JValue.CreateString(Regex.Replace(obj["message"]?.Value<string>() ?? "", @"Idx: \d+", ""));
                    break;
            }

            sw.WriteLine(obj.ToString());
        }
    }
}
