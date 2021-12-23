using System.Text;
using Mono.Cecil;

namespace Mono.Documentation.Updater
{
    public class FSharpMemberFormatter : FSharpFullMemberFormatter
    {
        public FSharpMemberFormatter() : this(null) {}
        public FSharpMemberFormatter(TypeMap map) : base(map) { }

        protected override StringBuilder AppendNamespace(StringBuilder buf, TypeReference type)
        {
            return buf;
        }
    }
}