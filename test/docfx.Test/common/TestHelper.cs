using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Docs.Build
{
    internal static class TestHelper
    {
        public static IEnumerable<(string docsetPath, BuildTestSpec testSpec)> PrepareDocsetsFromSpec(string specPath)
        {
            Assert.NotNull(specPath);

            var i = 1;
            var docsets = new List<(string docsetPath, BuildTestSpec testSpec)>();
            var specName = specPath.Replace("\\", "/").Replace("specs/", "").Replace(".yml", "");
            foreach (var testSpec in YamlUtility.DeserializeMany<BuildTestSpec>(File.ReadAllText(specPath)))
            {
                var docsetPath = Path.Combine("specs.drop", specName, $"{i++}");

                if (Directory.Exists(docsetPath))
                {
                    Directory.Delete(docsetPath, recursive: true);
                }

                foreach (var (file, content) in testSpec.Inputs)
                {
                    var filePath = Path.Combine(docsetPath, file);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    File.WriteAllText(filePath, content);
                }

                docsets.Add((docsetPath, testSpec));
            }

            return docsets;
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
    }

    class BuildTestSpec
    {
        public readonly Dictionary<string, string> Inputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public readonly Dictionary<string, string> Outputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public readonly Dictionary<string, string> Restorations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
