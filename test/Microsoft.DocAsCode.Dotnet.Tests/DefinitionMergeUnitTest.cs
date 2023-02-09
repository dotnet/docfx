// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dotnet.Tests
{
    using Xunit;

    public class DefinitionMergeUnitTest
    {
        [Fact]
        public void InterfaceWithTemplateDoesNotCrash()
        {
            // arrange
            string code = @"
namespace A {
  public interface IInterface
  {
    /// <summary>
    /// Summary
    /// </summary>
    void Function<T>();
  }

  public class Implementation : IInterface
  {
    public void Function<T>()
    {
    }
  }
}

";
            // act
            MetadataItem output = RoslynIntermediateMetadataExtractor.GenerateYamlMetadata(
                CompilationHelper.CreateCompilationFromCSharpCode(code, "test.dll").Assembly);

            // assert
            Assert.NotNull(output);
        }
    }
}
