// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions.Equivalency;
using Newtonsoft.Json.Linq;

#nullable enable

namespace docfx.Tests;

internal class CustomEqualityEquivalencyStep : IEquivalencyStep
{
    public EquivalencyResult Handle(
        Comparands comparands,
        IEquivalencyValidationContext context,
        IEquivalencyValidator nestedValidator)
    {
        if (comparands.Subject is null || comparands.Expectation is null)
            return EquivalencyResult.ContinueWithNext;

        var subject = comparands.Subject;
        var expected = comparands.Expectation;

        Type subjectType = subject.GetType();
        Type expectedType = expected.GetType();
        if (subjectType == expectedType || expectedType.IsAssignableFrom(subjectType))
        {
            return EquivalencyResult.ContinueWithNext;
        }

        // Try to convert `expection` to `subject` type.
        switch (expected)
        {
            // Required to comparing models that are loaded by SystemTextJsonUtility/NewtonsoftJsonUtility.
            case JToken jToken:
                comparands.Expectation = jToken.ToObject(subjectType);
                return EquivalencyResult.ContinueWithNext;

            // Required to comparing models that are loaded by YamlUtility/JsonUtility.
            case Dictionary<object, object> dict:
                {
                    comparands.Expectation = dict.ToDictionary(x => (string)x.Key, x => x.Value);
                    return EquivalencyResult.ContinueWithNext;
                }
        }

        // Try to convert `subject` to `expected` type.
        switch (subject)
        {
            case JToken jToken:
                comparands.Subject = jToken.ToObject(expectedType);
                return EquivalencyResult.ContinueWithNext;

            case Dictionary<object, object> dict:
                {
                    var convertedSubject = dict.ToDictionary(x => (string)x.Key, x => x.Value);
                    comparands.Subject = convertedSubject;
                    return EquivalencyResult.ContinueWithNext;
                }
        }

        // Use default validation if custom type conversion is not found.
        // (e.g. NewtonsoftJson deserialize integer as `Int64`. but SystemTextJson try to convert integer value to specific type by using `ObjectToInferredTypesConverter)
        return EquivalencyResult.ContinueWithNext;
    }
}
