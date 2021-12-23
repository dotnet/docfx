using System.Collections.Generic;
using Mono.Cecil;
namespace Mono.Documentation.Util
{
    public class Eiimembers
    {
        public string Fingerprint { get; set; }
        public List<MemberReference> Interfaces { get; set; }
    }
}
