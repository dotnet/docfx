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
    public static class BuildTest
    {
        public static readonly TheoryData<string> Specs = new TheoryData<string>();

        static BuildTest()
        {
            foreach (var spec in Directory.EnumerateFiles("specs", "*.yml", SearchOption.AllDirectories))
            {
                Specs.Add(spec);
            }
        }

        [Theory]
        [MemberData(nameof(Specs))]
        public static async Task BuildDocset(string specPath)
        {
            var i = 1;
            var specName = specPath.Replace("\\", "/").Replace("specs/", "").Replace(".yml", "");

            foreach (var spec in YamlUtility.DeserializeMany<BuildTestSpec>(File.ReadAllText(specPath)))
            {
                var docsetPath = Path.Combine("specs.drop", specName, $"{i++}");

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

                await Program.Main(new[] { "build", docsetPath });

                foreach (var (file, content) in spec.Outputs)
                {
                    VerifyFile(Path.GetFullPath(Path.Combine(docsetPath, "_site", file)), content);
                }
            }
        }

        private static void VerifyFile(string file, string content)
        {
            Assert.True(File.Exists(file), $"File should exist: '{file}'");

            switch (Path.GetExtension(file.ToLower()))
            {
                case ".json":
                    VerifyJsonContainEquals(
                        JToken.Parse(content),
                        JToken.Parse(File.ReadAllText(file)));
                    break;

                default:
                    Assert.Equal(
                        content.Trim(),
                        File.ReadAllText(file).Trim(),
                        ignoreCase: false,
                        ignoreLineEndingDifferences: true,
                        ignoreWhiteSpaceDifferences: true);
                    break;
            }
        }

        private static void VerifyJsonContainEquals(JToken expected, JToken actual)
        {
            if (expected is JArray expectedArray)
            {
                if (actual is JArray actualArray)
                {
                    Assert.Equal(expectedArray.Count, actualArray.Count);
                    for (var i = 0; i < expectedArray.Count; i++)
                    {
                        VerifyJsonContainEquals(expectedArray[i], actualArray[i]);
                    }
                }
                else
                {
                    Assert.True(actual is JArray, $"Array expected: {actual}");
                }
            }
            else if (expected is JObject expectedObject)
            {
                if (actual is JObject actualObject)
                {
                    foreach (var (key, value) in expectedObject)
                    {
                        Assert.True(actualObject.ContainsKey(key), $"Key '{key}' expected: {actual}");
                        VerifyJsonContainEquals(value, actualObject[key]);
                    }
                }
                else
                {
                    Assert.True(actual is JArray, $"Object expected: {actual}");
                }
            }
            else
            {
                Assert.Equal(((JValue)expected).Value, ((JValue)actual).Value);
            }
        }

        private class BuildTestSpec
        {
            public readonly Dictionary<string, string> Inputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public readonly Dictionary<string, string> Outputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
