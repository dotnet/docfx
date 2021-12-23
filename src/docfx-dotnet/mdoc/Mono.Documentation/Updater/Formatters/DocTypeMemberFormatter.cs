using System.Text;

using Mono.Cecil;

namespace Mono.Documentation.Updater
{
    class DocTypeMemberFormatter : DocTypeFullMemberFormatter
    {
        public DocTypeMemberFormatter(TypeMap map) : base(map) { }

        protected override StringBuilder AppendNamespace (StringBuilder buf, TypeReference type)
        {
            return buf;
        }
    }
}