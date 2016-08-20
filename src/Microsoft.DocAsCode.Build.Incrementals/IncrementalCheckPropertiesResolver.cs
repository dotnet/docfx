namespace Microsoft.DocAsCode.Build.Incrementals
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    
    public sealed class IncrementalCheckPropertiesResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (property.AttributeProvider.GetAttributes(typeof(IncrementalCheckAttribute), true).Count == 0)
            {
                property.Ignored = true;
            }

            return property;
        }
    }
}
