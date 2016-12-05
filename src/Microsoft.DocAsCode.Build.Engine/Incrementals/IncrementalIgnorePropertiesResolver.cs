// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System.Reflection;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    
    public sealed class IncrementalIgnorePropertiesResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (property.AttributeProvider.GetAttributes(typeof(IncrementalIgnoreAttribute), true).Count > 0)
            {
                property.Ignored = true;
            }

            return property;
        }
    }
}
