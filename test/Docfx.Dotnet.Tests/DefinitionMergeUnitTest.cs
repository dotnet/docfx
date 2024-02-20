// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Docfx.Dotnet.Tests;

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
        var compilation = CompilationHelper.CreateCompilationFromCSharpCode(code, msbuildProperties: new Dictionary<string, string>(), "test.dll");
        var output = compilation.Assembly.GenerateMetadataItem(compilation);

        // assert
        Assert.NotNull(output);
    }
}
