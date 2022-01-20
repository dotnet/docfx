using System.Text;

using Mono.Cecil;

namespace Mono.Documentation.Updater.Formatters
{
    public class CSharpMemberFormatter : CSharpFullMemberFormatter
    {
        public CSharpMemberFormatter() : this(null) {}
        public CSharpMemberFormatter(TypeMap map) : base(map) { }

        protected override StringBuilder AppendNamespace (StringBuilder buf, TypeReference type)
        {
            return buf;
        }
    }
}