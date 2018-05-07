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
                    foreach (var spec in FindTestSpecHeadersInFile(file))
                    {
                        result.Add(file, spec);
                    }
                });

            return result;
        }

        public static TestSpec FindTestSpecInFile(string ymlPath, string header)
        {
            var sections = File.ReadAllText(ymlPath).Split("\n---", StringSplitOptions.RemoveEmptyEntries);

            foreach (var section in sections)
            {
                var yaml = section.Trim('\r', '\n', '-');
                if (string.Equals(header, YamlUtility.ReadHeader(yaml) ?? "", StringComparison.OrdinalIgnoreCase))
                {
                    var spec = YamlUtility.Deserialize<TestSpec>(yaml);

                    spec.Path = Path.Combine(ymlPath.Replace("\\", "/").Replace("specs/", "").Replace(".yml", ""), header);

                    return spec;
                }
            }

            return null;
        }

        public static string CreateDocset(this TestSpec spec)
        {
            var docsetPath = Path.Combine("specs.drop", spec.Path);

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

            return docsetPath;
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

        private static IEnumerable<string> FindTestSpecHeadersInFile(string path)
        {
            var sections = File.ReadAllText(path).Split("\n---", StringSplitOptions.RemoveEmptyEntries);

            foreach (var section in sections)
            {
                var yaml = section.Trim('\r', '\n', '-');
                var header = YamlUtility.ReadHeader(yaml) ?? "";

                yield return header;
            }
        }
    }
}
