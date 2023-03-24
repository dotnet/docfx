﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.DocAsCode.Dotnet.Tests;

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
        var compilation = CompilationHelper.CreateCompilationFromCSharpCode(code, "test.dll");
        var output = compilation.Assembly.GenerateMetadataItem(compilation);

        // assert
        Assert.NotNull(output);
    }
}
