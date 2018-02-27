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
        public List<string> AssemblyFiles { get; set; } = new List<string>{
            @"TestData\BaseClassForTestClass1.dll",
            @"TestData\CatLibrary.dll" };

        [Fact]
        public void TestGenerateMetadataFromAssembly()
        {
            var compilation = CompilationUtility.CreateCompilationFromAssembly(AssemblyFiles);
            var referenceAssembly = CompilationUtility.GetAssemblyFromAssemblyComplation(compilation).Select(s => s.Item2).ToList();

            var output = GenerateYamlMetadata(compilation, referenceAssembly[1]);
            var @class = output.Items[0].Items[2];
            Assert.NotNull(@class);
            Assert.Equal("Cat<T, K>", @class.DisplayNames.First().Value);
            Assert.Equal("Cat<T, K>", @class.DisplayNamesWithType.First().Value);
            Assert.Equal("CatLibrary.Cat<T, K>", @class.DisplayQualifiedNames.First().Value);
        }
    }
}
