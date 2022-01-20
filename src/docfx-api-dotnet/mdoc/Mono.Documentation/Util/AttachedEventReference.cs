using Mono.Cecil;

namespace Mono.Documentation.Util
{
    public class AttachedEventReference : FieldReference
    {
        private readonly FieldDefinition fieldDefinition;
        private AttachedEventDefinition definition;

        public AttachedEventReference(FieldDefinition fieldDefinition) : base(AttachedEntitiesHelper.GetEventName(fieldDefinition.Name), fieldDefinition.FieldType, fieldDefinition.DeclaringType)
        {
            this.fieldDefinition = fieldDefinition;
        }

        protected override IMemberDefinition ResolveDefinition()
        {
            return definition ??
                   (definition = new AttachedEventDefinition(fieldDefinition, MetadataToken));
        }
    }
}