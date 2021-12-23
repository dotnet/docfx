using System.Text;
using Mono.Cecil;

namespace Mono.Documentation.Updater.Formatters.CppFormatters
{
    public class CppCxMemberFormatter : CppCxFullMemberFormatter
    {
        public CppCxMemberFormatter() : this(null) {}
        public CppCxMemberFormatter(TypeMap map) : base(map) { }

        protected override StringBuilder AppendNamespace (StringBuilder buf, TypeReference type)
        {
            return buf;
        }
    }
}