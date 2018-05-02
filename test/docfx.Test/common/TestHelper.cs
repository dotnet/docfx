using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Docs.Build
{
    internal static class TestHelper
    {
        public static TheoryData<TestSpec> FindTestSpecs(string path)
        {
            var result = new TheoryData<TestSpec>();

            Parallel.ForEach(
                Directory.EnumerateFiles(Path.Combine("specs", path), "*.yml", SearchOption.AllDirectories),
                file =>
                {
                    foreach (var spec in FindTestSpecsInFile(file))
                    {
                        result.Add(spec);
                    }
                });

            return result;
        }

        public static IEnumerable<TestSpec> FindTestSpecsInFile(string path)
        {
            var sections = File.ReadAllText(path).Split("\n---", StringSplitOptions.RemoveEmptyEntries);

            foreach (var section in sections)
            {
                var yaml = section.Trim('\r', '\n', '-');
                var header = YamlUtility.ReadHeader(yaml) ?? "";

                yield return new TestSpec
                {
                    Yaml = yaml,
                    Path = Path.Combine(path.Replace("\\", "/").Replace("specs/", "").Replace(".yml", ""), header),
                };
            }
        }

        public static (string docsetPath, TestSpecDefinition spec) CreateDocset(this TestSpec spec)
        {
            var docsetPath = Path.Combine("specs.drop", spec.Path);

            if (Directory.Exists(docsetPath))
            {
                Directory.Delete(docsetPath, recursive: true);
            }

            var definition = YamlUtility.Deserialize<TestSpecDefinition>(spec.Yaml);

            foreach (var (file, content) in definition.Inputs)
            {
                var filePath = Path.Combine(docsetPath, file);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, content);
            }

            return (docsetPath, definition);
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
}
