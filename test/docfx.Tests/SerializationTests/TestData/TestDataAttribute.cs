// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit.Sdk;

namespace docfx.Tests;

public class TestDataAttribute<T> : DataAttribute
{
    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        var key = GetTestDataKey();
        var paths = TestData.GetTestDataFilePaths(key);

        var className = testMethod.DeclaringType.Name;

        // Filter test data.
        switch (className)
        {
            case nameof(JsonSerializationTest):
                paths = paths.Where(x => x.EndsWith(".json")).ToArray();
                break;
            case nameof(YamlSerializationTest):
                paths = paths.Where(x => x.EndsWith(".yml")).ToArray();
                break;
            default:
                throw new NotSupportedException($"{className} is not supported.");
        }

        return new TheoryData<string>(paths);
    }

    private static string GetTestDataKey()
    {
        var type = typeof(T);
        var fullname = type.FullName;

        switch (fullname)
        {
            case "Docfx.DataContracts.ManagedReference.PageViewModel":
                return "ManagedReference";
            case "Docfx.DataContracts.UniversalReference.PageViewModel":
                return "UniversalReference";
            default:
                return type.Name;
        }
    }
}
