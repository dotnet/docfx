// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using YamlDotNet.RepresentationModel;

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

            foreach (var spec in LoadSpecs(specPath))
            {
                var docsetPath = Path.Combine("specs.drop", specName, $"{i++}");

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
                default:
                    Assert.Equal(File.ReadAllText(file), content);
                    break;
            }
        }

        private static IEnumerable<BuildTestSpec> LoadSpecs(string specPath)
        {
            var yaml = new YamlStream();
            yaml.Load(new StringReader(File.ReadAllText(specPath)));

            foreach (var doc in yaml.Documents)
            {
                if (doc.RootNode is YamlMappingNode root)
                {
                    var spec = new BuildTestSpec();

                    foreach (var (file, content) in (YamlMappingNode)root["inputs"])
                    {
                        spec.Inputs.Add(((YamlScalarNode)file).Value, ((YamlScalarNode)content).Value);
                    }

                    foreach (var (file, content) in (YamlMappingNode)root["outputs"])
                    {
                        spec.Outputs.Add(((YamlScalarNode)file).Value, ((YamlScalarNode)content).Value);
                    }

                    yield return spec;
                }
            }
        }

        private class BuildTestSpec
        {
            public Dictionary<string, string> Inputs { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public Dictionary<string, string> Outputs { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
