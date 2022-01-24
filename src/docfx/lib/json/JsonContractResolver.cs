// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build;

internal class JsonContractResolver : DefaultContractResolver
{
    protected override JsonObjectContract CreateObjectContract(Type objectType)
    {
        var contract = base.CreateObjectContract(objectType);
        PropagateSourceInfoToExtensionData(contract);
        return contract;
    }

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);
        HandleSourceInfo(property);
        DoNotSerializeEmptyArray(property);
        SetMemberWritable();
        return property;

        void SetMemberWritable()
        {
            if (!property.Writable)
            {
                if (member is PropertyInfo p && p.CanWrite)
                {
                    property.Writable = true;
                }
            }
        }
    }

    private static void DoNotSerializeEmptyArray(JsonProperty property)
    {
        if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType) && !(property.PropertyType == typeof(string)))
        {
            var originalShouldSerialize = property.ShouldSerialize;

            property.ShouldSerialize = target =>
            {
                var value = property.ValueProvider?.GetValue(target);
                if (value != null && IsEmptyArray(value))
                {
                    return false;
                }

                return originalShouldSerialize?.Invoke(target) ?? true;
            };
        }
    }

    private static bool IsEmptyArray(object value)
    {
        return value is not string && value is IEnumerable enumerable && !enumerable.GetEnumerator().MoveNext();
    }

    private static void HandleSourceInfo(JsonProperty property)
    {
        if (property.PropertyType != null && property.PropertyType.IsGenericType &&
            property.PropertyType.GetGenericTypeDefinition() == typeof(SourceInfo<>))
        {
            // Allow source info propagation for null values
            property.NullValueHandling = NullValueHandling.Include;

            // Do not serialize empty array or null
            var originalShouldSerialize = property.ShouldSerialize;

            property.ShouldSerialize = target =>
            {
                if (property.ValueProvider?.GetValue(target) is ISourceInfo sourceInfoValue)
                {
                    var value = sourceInfoValue.GetValue();
                    if (value is null || IsEmptyArray(value))
                    {
                        return false;
                    }
                }

                return originalShouldSerialize?.Invoke(target) ?? true;
            };
        }
    }

    private static void PropagateSourceInfoToExtensionData(JsonObjectContract contract)
    {
        var extensionDataSetter = contract.ExtensionDataSetter;
        if (extensionDataSetter != null)
        {
            contract.ExtensionDataSetter = (o, key, value) =>
            {
                if (contract.ExtensionDataValueType == typeof(JToken))
                {
                    var currentToken = JsonUtility.State?.Reader?.CurrentToken;
                    if (currentToken != null)
                    {
                        extensionDataSetter(o, key, JsonUtility.DeepClone(currentToken));
                        return;
                    }
                }
                extensionDataSetter(o, key, value);
            };
        }
    }
}
