// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Docs.Build
{
    internal static class TestHelper
    {
        public static TheoryData<string, string> FindTestSpecs(string path)
        {
            var result = new TheoryData<string, string>();

            Parallel.ForEach(
                Directory.EnumerateFiles(Path.Combine("specs", path), "*.yml", SearchOption.AllDirectories),
                file =>
                {
                    var i = 0;
                    foreach (var (header, yaml) in FindTestSpecHeadersInFile(file))
                    {
                        var name = $"{++i:D2}. {header}";
                        var folder = Path.Combine(
                            file.Replace("\\", "/").Replace($"specs/", "").Replace(".yml", ""),
                            name).Replace("\\", "/");

                        result.Add(folder, yaml);
                    }
                });

            return result;
        }

        public static (string docsetPath, TestSpec spec) CreateDocset(string specPath, string specYaml)
        {
            var spec = YamlUtility.Deserialize<TestSpec>(specYaml);

            foreach (var ch in Path.GetInvalidPathChars())
            {
                specPath = specPath.Replace(ch, ' ');
            }

            var docsetPath = Path.Combine("specs.drop", specPath);

            if (Directory.Exists(docsetPath))
            {
                Directory.Delete(docsetPath, recursive: true);
            }

            foreach (var (file, content) in spec.Inputs)
            {
                var filePath = Path.Combine(docsetPath, file);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, content);
            }

            return (docsetPath, spec);
        }

        public static void VerifyJsonContainEquals(JToken expected, JToken actual)
        {
            if (expected is JArray expectedArray)
            {
                Assert.IsType<JArray>(actual);
                var actualArray = (JArray)actual;
                Assert.Equal(expectedArray.Count, actualArray.Count);
                for (var i = 0; i < expectedArray.Count; i++)
                {
                    VerifyJsonContainEquals(expectedArray[i], actualArray[i]);
                }
            }
            else if (expected is JObject expectedObject)
            {
                Assert.IsType<JObject>(actual);
                var actualObject = (JObject)actual;
                foreach (var (key, value) in expectedObject)
                {
                    Assert.True(actualObject.ContainsKey(key), $"Key '{key}' expected: {actual}");
                    VerifyJsonContainEquals(value, actualObject[key]);
                }
            }
            else
            {
                Assert.Equal(expected, actual);
            }
        }

        private static IEnumerable<(string header, string yaml)> FindTestSpecHeadersInFile(string path)
        {
            var sections = File.ReadAllText(path).Split("\n---", StringSplitOptions.RemoveEmptyEntries);

            foreach (var section in sections)
            {
                var yaml = section.Trim('\r', '\n', '-');
                var header = YamlUtility.ReadHeader(yaml) ?? "";

                yield return (header, yaml);
            }
        }
    }
}
