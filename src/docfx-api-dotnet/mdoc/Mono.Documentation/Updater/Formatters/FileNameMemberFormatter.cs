using System.Text;

using Mono.Cecil;

namespace Mono.Documentation.Updater
{
    class FileNameMemberFormatter : SlashDocMemberFormatter
    {
        public FileNameMemberFormatter(TypeMap map) : base(map) { }

        protected override StringBuilder AppendNamespace (StringBuilder buf, TypeReference type)
        {
            return buf;
        }

        protected override string NestedTypeSeparator
        {
            get { return "+"; }
        }
    }
}