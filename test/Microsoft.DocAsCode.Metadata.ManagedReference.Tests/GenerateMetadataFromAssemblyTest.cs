// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference.Tests
{
    using System.Collections.Generic;
    using System.Linq;

    using Xunit;

    using static Microsoft.DocAsCode.Metadata.ManagedReference.RoslynIntermediateMetadataExtractor;

    [Trait("Owner", "qinezh")]
    [Trait("Language", "CSharp")]
    [Trait("EntityType", "Model")]
    [Collection("docfx STA")]
    public class GenerateMetadataFromAssemblyTest
    {
        public readonly string[] AssemblyFiles =
            new[]
            {
                @"TestData\BaseClassForTestClass1.dll",
                @"TestData\CatLibrary.dll",
                @"TestData\CatLibrary2.dll",
            };

        [Fact]
        public void TestGenerateMetadataFromAssembly()
        {
            var compilation = CompilationUtility.CreateCompilationFromAssembly(AssemblyFiles);
            var referenceAssembly = CompilationUtility.GetAssemblyFromAssemblyComplation(compilation, AssemblyFiles).Select(s => s.assembly).ToList();

            {
                var output = GenerateYamlMetadata(compilation, referenceAssembly[1]);
                var @class = output.Items[0].Items[2];
                Assert.NotNull(@class);
                Assert.Equal("Cat<T, K>", @class.DisplayNames.First().Value);
                Assert.Equal("Cat<T, K>", @class.DisplayNamesWithType.First().Value);
                Assert.Equal("CatLibrary.Cat<T, K>", @class.DisplayQualifiedNames.First().Value);
            }

            {
                var output = GenerateYamlMetadata(compilation, referenceAssembly[2]);
                var @class = output.Items[0].Items[0];
                Assert.NotNull(@class);
                Assert.Equal("CarLibrary2.Cat2", @class.Name);
                Assert.Equal(new[] { "System.Object", "CatLibrary.Cat{CatLibrary.Dog{System.String},System.Int32}" }, @class.Inheritance);
            }
        }
    }
}
