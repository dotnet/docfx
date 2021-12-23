using System.Text;
using Mono.Cecil;

namespace Mono.Documentation.Updater.Formatters.CppFormatters
{
    public class CppMemberFormatter : CppFullMemberFormatter
    {
        public CppMemberFormatter() : this(null) {}
        public CppMemberFormatter(TypeMap map) : base(map) { }

        protected override StringBuilder AppendNamespace (StringBuilder buf, TypeReference type)
        {
            return buf;
        }
    }
}