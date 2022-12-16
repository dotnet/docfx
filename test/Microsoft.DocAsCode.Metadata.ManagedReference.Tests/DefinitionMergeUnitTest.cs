// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference.Tests
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Xunit;

    [Trait("Language", "CSharp")]
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
            MetadataItem output = RoslynIntermediateMetadataExtractor.GenerateYamlMetadata(CompilationUtility.CreateCompilationFromCsharpCode(code, "test.dll"));

            // assert
            Assert.NotNull(output);
        }
    }
}