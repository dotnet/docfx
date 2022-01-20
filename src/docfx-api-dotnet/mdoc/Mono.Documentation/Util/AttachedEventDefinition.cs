using Mono.Cecil;
using Mono.Collections.Generic;

namespace Mono.Documentation.Util
{
    public class AttachedEventDefinition : AttachedEventReference, IMemberDefinition
    {
        private readonly FieldDefinition fieldDefinition;

        public AttachedEventDefinition(FieldDefinition fieldDefinition, MetadataToken metadataToken)
            : base(fieldDefinition)
        {
            this.fieldDefinition = fieldDefinition;
            MetadataToken = metadataToken;
        }

        public Collection<CustomAttribute> CustomAttributes => fieldDefinition.CustomAttributes;
        public bool HasCustomAttributes => fieldDefinition.HasCustomAttributes;

        public bool IsSpecialName
        {
            get { return fieldDefinition.IsSpecialName; }
            set { fieldDefinition.IsSpecialName = value; }
        }

        public bool IsRuntimeSpecialName
        {
            get { return fieldDefinition.IsRuntimeSpecialName; }
            set { fieldDefinition.IsRuntimeSpecialName = value; }
        }

        public new TypeDefinition DeclaringType
        {
            get { return fieldDefinition.DeclaringType; }
            set { fieldDefinition.DeclaringType = value; }
        }
    }
}