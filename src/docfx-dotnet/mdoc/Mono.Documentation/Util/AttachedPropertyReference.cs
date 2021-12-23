using Mono.Cecil;

namespace Mono.Documentation.Util
{
    public class AttachedPropertyReference : FieldReference
    {
        private readonly FieldDefinition fieldDefinition;
        private readonly PropertyDefinition propertyDefinition;
        private AttachedPropertyDefinition definition;

        public AttachedPropertyReference(FieldDefinition fieldDefinition) : base(AttachedEntitiesHelper.GetPropertyName(fieldDefinition.Name), fieldDefinition.FieldType, fieldDefinition.DeclaringType)
        {
            this.fieldDefinition = fieldDefinition;
        }

        public AttachedPropertyReference(PropertyDefinition propertyDefinition) : base(AttachedEntitiesHelper.GetPropertyName(propertyDefinition.Name), propertyDefinition.PropertyType, propertyDefinition.DeclaringType)
        {
            this.propertyDefinition = propertyDefinition;
        }

        protected override IMemberDefinition ResolveDefinition()
        {
            return definition ??
                  (definition = fieldDefinition != null ? new AttachedPropertyDefinition(fieldDefinition, MetadataToken) : new AttachedPropertyDefinition(propertyDefinition, MetadataToken));
        }
    }
}