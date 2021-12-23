using System.Text;
using Mono.Cecil;

namespace Mono.Documentation.Updater
{
    class MsxdocSlashDocMemberFormatter : SlashDocMemberFormatter
    {
        public MsxdocSlashDocMemberFormatter(TypeMap map) : base(map) { }

        protected override StringBuilder AppendRefTypeName(StringBuilder buf, TypeReference type, IAttributeParserContext context)
        {
            TypeSpecification spec = type as TypeSpecification;
            return _AppendTypeName(buf, spec != null ? spec.ElementType : type.GetElementType(), context).Append(RefTypeModifier);
        }
    }
}
