using mdoc.Mono.Documentation.Updater;
using Mono.Cecil;
using Mono.Documentation.Util;
using System;
using System.Collections.ObjectModel;

namespace Mono.Documentation.Updater
{
    public class AttributeParserContext : IAttributeParserContext
    {
        private int nullableAttributeIndex;
        private int dynamicAttributeIndex;
        private ICustomAttributeProvider provider;
        private ReadOnlyCollection<bool?> nullableAttributeFlags;
        private ReadOnlyCollection<bool> dynamicAttributeFlags;

        private AttributeParserContext(ICustomAttributeProvider provider)
        {
            this.provider = provider;

            ReadDynamicAttribute();
            ReadNullableAttribute();
        }

        private bool ExistsNullableAttribute
        {
            get
            {
                return nullableAttributeFlags.Count > 0;
            }
        }

        private bool HasSameNullableValue
        {
            get
            {
                return nullableAttributeFlags.Count == 1;
            }
        }

        public static IAttributeParserContext Create(ICustomAttributeProvider provider)
        {
            return new AttributeParserContext(provider);
        }

        public void NextDynamicFlag()
        {
            dynamicAttributeIndex++;
        }

        public bool IsDynamic()
        {
            return dynamicAttributeFlags != null && (dynamicAttributeFlags.Count == 0 || dynamicAttributeFlags[dynamicAttributeIndex]);
        }

        public bool IsNullable()
        {
            if (ExistsNullableAttribute)
            {
                if (HasSameNullableValue)
                {
                    return nullableAttributeFlags[0].IsTrue();
                }

                if (nullableAttributeIndex < nullableAttributeFlags.Count)
                {
                    return nullableAttributeFlags[nullableAttributeIndex++].IsTrue();
                }

                throw new IndexOutOfRangeException("You are out of range in the nullable attribute values, please call the method for each nullable checking only once.");
            }

            return false;
        }

        private void ReadDynamicAttribute()
        {
            DynamicTypeProvider dynamicTypeProvider = new DynamicTypeProvider(provider);
            var dynamicTypeFlags = dynamicTypeProvider.GetDynamicTypeFlags();
            if (dynamicTypeFlags != null)
            {
                dynamicAttributeFlags = new ReadOnlyCollection<bool>(dynamicTypeFlags);
            }
        }

        private void ReadNullableAttribute()
        {
            NullableReferenceTypeProvider nullableReferenceTypeProvider = new NullableReferenceTypeProvider(provider);
            nullableAttributeFlags = new ReadOnlyCollection<bool?>(nullableReferenceTypeProvider.GetNullableReferenceTypeFlags());
        }
    }
}