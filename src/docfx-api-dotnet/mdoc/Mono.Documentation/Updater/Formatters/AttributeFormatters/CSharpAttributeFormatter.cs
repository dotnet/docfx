using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mono.Documentation.Updater.Formatters
{
    class CSharpAttributeFormatter : AttributeFormatter
    {
        public override string PrefixBrackets { get; } = "[";
        public override string SurfixBrackets { get; } = "]";
        public override string Language => Consts.CSharp;
    }
}
