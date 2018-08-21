// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;
using System.IO;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class ConfigTest
    {
        [Fact]
        public static void TestConigWithGlobalMetadata()
        {
            var docsetPath = "config/ConfigWithGlobalMetadata";
            var configYaml = @"globalMetadata:
  key1: value
  key2: 2";
            Prepare(docsetPath, configYaml);

            var (errors, config) = Config.Load(docsetPath, new CommandLineOptions());

            var expected = new JObject() { ["key1"] = "value", ["key2"] = 2 };
            Assert.Empty(errors);
            AssertObjectEqual(expected, config.GlobalMetadata);
        }

        [Fact]
        public static void TestConfigWithObjectFileMetadata()
        {
            var docsetPath = "config/ConfigWithObjectFileMetadata";
            var configYaml = @"fileMetadata:
  folder/:
    key1: value1
  file:
    key2: value2
  special/chars/allowed{,}:
    key3: value3";
            Prepare(docsetPath, configYaml);

            var (errors, config) = Config.Load(docsetPath, new CommandLineOptions());

            var expected = new GlobConfig<JObject>[]
            {
                new GlobConfig<JObject>(new []{ "folder/"}, null, new JObject(){["key1"] = "value1"}, false),
                new GlobConfig<JObject>(new []{ "file"}, null, new JObject(){["key2"] = "value2"}, false),
                new GlobConfig<JObject>(new []{ "special/chars/allowed{,}"}, null, new JObject(){["key3"] = "value3"}, false),
            };
            Assert.Empty(errors);
            AssertObjectEqual(expected, config.FileMetadata);
        }

        [Fact]
        public static void TestConfigWithArrayFileMetadata()
        {
            var docsetPath = "config/ConfigWithArrayFileMetadata";
            var configYaml = @"fileMetadata:
- include: folder1/**
  exclude: folder1/exclude/**
  value:
    key1: value1
- include:
  - folder21/**
  - folder22/**
  value:
    key2: value2
";
            Prepare(docsetPath, configYaml);

            var (errors, config) = Config.Load(docsetPath, new CommandLineOptions());

            var expected = new GlobConfig<JObject>[]
            {
                new GlobConfig<JObject>(new []{ "folder1/**"}, new []{ "folder1/exclude/**" }, new JObject(){["key1"] = "value1"}),
                new GlobConfig<JObject>(new []{ "folder21/**", "folder22/**" }, null, new JObject(){["key2"] = "value2"}),
            };
            Assert.Empty(errors);
            AssertObjectEqual(expected, config.FileMetadata);
        }

        private static void Prepare(string docsetPath, string config)
        {
            if (Directory.Exists(docsetPath))
                Directory.Delete(docsetPath, true);
            Directory.CreateDirectory(docsetPath);
            File.WriteAllText(Path.Combine(docsetPath, "docfx.yml"), config);
        }

        private static void AssertObjectEqual(object expected, object actual)
        {
            Assert.Equal(JsonUtility.Serialize(expected), JsonUtility.Serialize(actual));
        }
    }
}
