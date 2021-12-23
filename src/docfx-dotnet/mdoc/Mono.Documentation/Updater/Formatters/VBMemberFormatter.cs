using System.Text;
using Mono.Cecil;

namespace Mono.Documentation.Updater
{
    public interface IFormatterNamespaceControl
    {
        bool ShouldAppendNamespace { get; set; }
    }
    public class VBMemberFormatter : VBFullMemberFormatter, IFormatterNamespaceControl
    {
        public bool ShouldAppendNamespace { get; set; } = false;

        public VBMemberFormatter() : this(null) {}
        public VBMemberFormatter(TypeMap map) : base(map) { }

        protected override StringBuilder AppendNamespace(StringBuilder buf, TypeReference type)
        {
            return ShouldAppendNamespace ? base.AppendNamespace(buf, type) : buf;
        }

    }
}