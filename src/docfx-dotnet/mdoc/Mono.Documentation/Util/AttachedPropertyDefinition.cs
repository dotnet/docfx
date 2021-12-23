using Mono.Cecil;
using Mono.Collections.Generic;

namespace Mono.Documentation.Util
{
    public class AttachedPropertyDefinition : AttachedPropertyReference, IMemberDefinition
    {
        private readonly FieldDefinition fieldDefinition;
        private readonly PropertyDefinition propertyDefinition;
        private bool isAttachedField;

        public AttachedPropertyDefinition(FieldDefinition fieldDefinition, MetadataToken metadataToken) : base(fieldDefinition)
        {
            this.fieldDefinition = fieldDefinition;
            MetadataToken = metadataToken;
            isAttachedField = true;
        }

        public AttachedPropertyDefinition(PropertyDefinition propertyDefinition, MetadataToken metadataToken) : base(propertyDefinition)
        {
            this.propertyDefinition = propertyDefinition;
            MetadataToken = metadataToken;
        }

        public MemberReference GetMethod 
        {
            get => 
                    isAttachedField ?
                        this.DeclaringType.GetMember(
                        $"Get{AttachedEntitiesHelper.GetPropertyName(fieldDefinition.Name)}",
                        m => (m as MethodReference)?.Parameters.Count == 1)
                    :
                        this.DeclaringType.GetMember(
                        $"Get{AttachedEntitiesHelper.GetPropertyName(propertyDefinition.Name)}",
                        m => (m as MethodReference)?.Parameters.Count == 1);
        }
        public MemberReference SetMethod
        {
            get => 
                   isAttachedField ?
                       this.DeclaringType.GetMember(
                       $"Set{AttachedEntitiesHelper.GetPropertyName(fieldDefinition.Name)}",
                       m => (m as MethodReference)?.Parameters.Count == 2)
                     :
                       this.DeclaringType.GetMember(
                       $"Set{AttachedEntitiesHelper.GetPropertyName(propertyDefinition.Name)}",
                       m => (m as MethodReference)?.Parameters.Count == 2);
        }

        public Collection<CustomAttribute> CustomAttributes => isAttachedField ? fieldDefinition.CustomAttributes : propertyDefinition.CustomAttributes;
        public bool HasCustomAttributes => isAttachedField ? fieldDefinition.HasCustomAttributes : propertyDefinition.HasCustomAttributes;

        public bool IsSpecialName
        {
            get { return isAttachedField ? fieldDefinition.IsSpecialName : propertyDefinition.IsSpecialName; }
            set
            {
                if (isAttachedField)
                    fieldDefinition.IsSpecialName = value;
                else
                    propertyDefinition.IsSpecialName = value;
            }
        }

        public bool IsRuntimeSpecialName
        {
            get { return isAttachedField ? fieldDefinition.IsRuntimeSpecialName : propertyDefinition.IsRuntimeSpecialName; }
            set
            {
                if (isAttachedField)
                    fieldDefinition.IsRuntimeSpecialName = value;
                else
                    propertyDefinition.IsRuntimeSpecialName = value;
            }
        }

        public new TypeDefinition DeclaringType
        {
            get { return isAttachedField ? fieldDefinition.DeclaringType : propertyDefinition.DeclaringType; }
            set
            {
                if (isAttachedField)
                    fieldDefinition.DeclaringType = value;
                else
                    propertyDefinition.DeclaringType = value;
            }
        }
    }
}