using Mono.Cecil;
using Mono.Documentation.Updater;
using Mono.Documentation.Util;
using System.Collections.Generic;
using System.Linq;

namespace mdoc.Mono.Documentation.Updater
{
    public class DynamicTypeProvider
    {
        private const string DynamicAttributeFulleName = "System.Runtime.CompilerServices.DynamicAttribute";

        private ICustomAttributeProvider provider;

        public DynamicTypeProvider(ICustomAttributeProvider provider)
        {
            this.provider = provider;
        }

        public IList<bool> GetDynamicTypeFlags()
        {
            CustomAttribute dynamicAttribute = FindDynamicAttribute();
            if (dynamicAttribute != null)
            {
                CustomAttributeArgument[] attributeValues = new CustomAttributeArgument[0];
                if (dynamicAttribute.ConstructorArguments.Count > 0)
                {
                    attributeValues = (CustomAttributeArgument[])dynamicAttribute.ConstructorArguments[0].Value;
                }

                return attributeValues.Select(t => (bool)t.Value).ToList();
            }

            return null;
        }

        private CustomAttribute FindDynamicAttribute()
        {
            if (provider.HasCustomAttributes)
            {
                return provider.CustomAttributes.SafeCast<CustomAttribute>().SingleOrDefault(ca => ca.GetDeclaringType() == DynamicAttributeFulleName);
            }

            return null;
        }
    }
}
