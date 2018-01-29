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
            MetadataItem output = RoslynIntermediateMetadataExtractor.GenerateYamlMetadata(CreateCompilationFromCSharpCode(code, "test.dll"));

            // assert
            Assert.NotNull(output);
        }

        private static Compilation CreateCompilationFromCSharpCode(string code, string assemblyName, params MetadataReference[] references)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(code);
            var defaultReferences = new List<MetadataReference> { MetadataReference.CreateFromFile(typeof(object).Assembly.Location), MetadataReference.CreateFromFile(typeof(EditorBrowsableAttribute).Assembly.Location) };
            if (references != null)
            {
                defaultReferences.AddRange(references);
            }

            var compilation = CSharpCompilation.Create(
                assemblyName,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                syntaxTrees: new[] { tree },
                references: defaultReferences);
            return compilation;
        }
    }
}