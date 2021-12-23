using System.Text;
using Mono.Cecil;

namespace Mono.Documentation.Updater
{
    public class FSharpFullMemberFormatter : FSharpFormatter
    {
        public override string Language => Consts.FSharp;

        private readonly MemberFormatter usageFormatter;
        public override MemberFormatter UsageFormatter => usageFormatter;

        public FSharpFullMemberFormatter() : this(null) {}
        public FSharpFullMemberFormatter(TypeMap map) : base(map) {
             usageFormatter = new FSharpUsageFormatter(map);
        }

        protected override StringBuilder AppendNamespace(StringBuilder buf, TypeReference type)
        {
            string ns = DocUtils.GetNamespace(type);
            if (GetFSharpType(type) == null && !string.IsNullOrEmpty(ns) && ns != "System")
                buf.Append(ns).Append('.');
            return buf;
        }
    }
}
